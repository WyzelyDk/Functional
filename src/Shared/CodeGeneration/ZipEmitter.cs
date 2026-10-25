namespace Wyzely.Functional.CodeGeneration;

/// <summary>
/// Writes the <c>Zip</c> overloads of one library: for every count from 2 up to <see cref="GeneratorSettings.MaxZipArity"/>, one method that
/// combines the values into a tuple and one that combines them with a selector, plus the tests of both. A <c>Maybe</c> gets a third
/// overload whose selector returns a nullable value type.
/// </summary>
/// <param name="profile">What differs between the libraries that share the generator.</param>
/// <param name="kind">Whether the values are maybes or results.</param>
internal sealed class ZipEmitter(GeneratorProfile profile, ZipKind kind)
{
    private const string AssertIsNone = "Assert.True(outcome.IsNone);";

    private static readonly string[] Ordinals = ["first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth"];
    private static readonly string[] TupleNames = ["First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth"];

    private bool IsMaybe => kind == ZipKind.Maybe;

    private string Noun => IsMaybe ? "maybe" : "result";

    private string TypeName => IsMaybe ? "Maybe" : "Result";

    /// <summary>The relative path of the generated library file of one overload.</summary>
    /// <param name="arity">The number of values.</param>
    /// <param name="selector"><see langword="true"/> for the overload with a selector.</param>
    /// <param name="nullableStruct"><see langword="true"/> for the overload whose selector returns a nullable value type.</param>
    /// <returns>The path, with forward slashes.</returns>
    internal string LibraryPath(int arity, bool selector, bool nullableStruct = false)
    {
        var flavour = "Tuple";

        if (nullableStruct)
        {
            flavour = "SelectorStruct";
        }
        else if (selector)
        {
            flavour = "Selector";
        }

        return $"{profile.LibraryDirectory}/Zip/Zip.{flavour}.Arity{arity}.g.cs";
    }

    /// <summary>The relative path of the generated test file of one arity.</summary>
    /// <param name="arity">The number of values.</param>
    /// <returns>The path, with forward slashes.</returns>
    internal string TestPath(int arity) => $"{profile.TestDirectory}/ZipArity{arity}Tests.g.cs";

    /// <summary>Writes the library file of one overload.</summary>
    /// <param name="arity">The number of values.</param>
    /// <param name="selector"><see langword="true"/> for the overload with a selector.</param>
    /// <param name="stubs"><see langword="true"/> to emit a body that does nothing useful (the red phase).</param>
    /// <param name="nullableStruct"><see langword="true"/> for the overload whose selector returns a nullable value type (a <c>Maybe</c> only).</param>
    /// <returns>The text of the file.</returns>
    internal string EmitLibrary(int arity, bool selector, bool stubs, bool nullableStruct = false)
    {
        var writer = new CodeWriter();
        WriteHeader(writer);
        writer.Line("namespace Wyzely.Functional;");
        writer.Line();
        writer.Line("/// <content>");
        writer.Line($"/// The <c>Zip</c> operation for {arity} {Noun}s, {(selector ? "combined with a selector" : "combined into a tuple")}.");
        writer.Line("/// </content>");
        writer.Line($"public static partial class {profile.ExtensionsClass}");
        writer.Open();
        WriteDocumentation(writer, arity, selector, nullableStruct);

        var valueTypes = Enumerable.Range(1, arity).Select(index => $"T{index}").ToArray();
        var typeParameters = selector ? [.. valueTypes, "TResult"] : valueTypes;
        var resultType = selector ? "TResult" : TupleType(valueTypes);

        var parameters = string.Join(", ", Parameters(valueTypes, selector));

        writer.Line($"public static {TypeName}<{resultType}> Zip<{string.Join(", ", typeParameters)}>(this {parameters})");

        if (IsMaybe)
        {
            WriteConstraints(writer, typeParameters, nullableStruct);
        }

        writer.Open();
        writer.Lines(stubs ? ["return default!;"] : Body(arity, selector, resultType, nullableStruct));
        writer.Close();
        writer.Close();

        return writer.ToString();
    }

    private static string TupleType(string[] valueTypes) =>
        $"({string.Join(", ", valueTypes.Select((type, index) => $"{type} {TupleNames[index]}"))})";

    private List<string> Parameters(string[] valueTypes, bool selector)
    {
        var parameters = valueTypes.Select((type, index) => $"{TypeName}<{type}> {Ordinals[index]}").ToList();

        if (selector)
        {
            parameters.Add($"Func<{string.Join(", ", valueTypes)}, TResult{(IsMaybe ? "?" : string.Empty)}> selector");
        }

        return parameters;
    }

    private static void WriteConstraints(CodeWriter writer, string[] typeParameters, bool nullableStruct)
    {
        writer.Indent();

        foreach (var typeParameter in typeParameters)
        {
            writer.Line(nullableStruct && typeParameter == "TResult" ? "where TResult : struct" : $"where {typeParameter} : notnull");
        }

        writer.Unindent();
    }

    /// <summary>Writes the test file of one arity.</summary>
    /// <param name="arity">The number of values.</param>
    /// <returns>The text of the file.</returns>
    internal string EmitTests(int arity)
    {
        var writer = new CodeWriter();
        WriteHeader(writer);
        writer.Line($"namespace {profile.TestNamespace};");
        writer.Line();
        writer.Line($"public sealed class ZipArity{arity}Tests");
        writer.Open();

        var file = new TestFile(writer, arity, IsMaybe);

        AddValueTests(file);
        AddFailureTests(file);
        AddSelectorTests(file);

        if (IsMaybe)
        {
            AddNullableSelectorTests(file);
        }

        writer.Close();

        return writer.ToString();
    }

    private static void AddValueTests(TestFile file) => file.Add(
        "combines_the_values_into_a_tuple_when_all_have_a_value",
        $"var outcome = {Call(file.Present, null)};",
        string.Empty,
        $"Assert.Equal({file.ExpectedTuple}, outcome.GetValue());");

    // One test for every position that has no value or failed, and the tests that only a result has.
    private void AddFailureTests(TestFile file)
    {
        for (var index = 0; index < file.Arity; index++)
        {
            var arguments = (string[])file.Present.Clone();

            if (IsMaybe)
            {
                arguments[index] = "Maybe.None<int>()";
                file.Add(
                    $"returns_none_when_the_{Ordinals[index]}_maybe_has_no_value",
                    $"var outcome = {Call(arguments, null)};",
                    string.Empty,
                    AssertIsNone);
            }
            else
            {
                arguments[index] = "Result.Failure<int>(error)";
                file.Add(
                    $"returns_the_error_when_the_{Ordinals[index]}_result_failed",
                    "var error = Error.Create(\"Failed.\");",
                    $"var outcome = {Call(arguments, null)};",
                    string.Empty,
                    "Assert.Same(error, outcome.GetError());");
            }
        }

        if (!IsMaybe)
        {
            AddResultOnlyTests(file);
        }
    }

    private static void AddResultOnlyTests(TestFile file)
    {
        var several = (string[])file.Present.Clone();
        several[0] = "Result.Failure<int>(first)";
        several[file.Arity - 1] = "Result.Failure<int>(last)";
        var defaulted = (string[])file.Present.Clone();
        defaulted[file.Arity - 1] = "default(Result<int>)";

        file.Add(
            "returns_an_aggregate_of_all_errors_in_order_when_several_results_failed",
            "var first = Error.Create(\"First failed.\");",
            "var last = Error.Create(\"Last failed.\");",
            $"var outcome = {Call(several, null)};",
            string.Empty,
            "var error = outcome.GetError();",
            "Assert.True(error.IsAggregate);",
            "Assert.Equal([first, last], error.Children);");

        file.Add(
            "treats_a_default_result_as_a_success",
            $"var outcome = {Call(defaulted, null)};",
            string.Empty,
            "Assert.True(outcome.IsSuccess);");
    }

    private void AddSelectorTests(TestFile file)
    {
        file.Add(
            "calls_the_selector_with_the_values_when_all_have_a_value",
            "var calls = 0;",
            $"var outcome = {Call(file.Present, file.SelectorCalls)};",
            string.Empty,
            $"Assert.Equal({file.Total}, outcome.GetValue());",
            "Assert.Equal(1, calls);");

        file.Add(
            $"does_not_call_the_selector_when_one_{Noun}_{(IsMaybe ? "has_no_value" : "failed")}",
            "var calls = 0;",
            $"var outcome = {Call(file.Failing(), file.SelectorCalls)};",
            string.Empty,
            IsMaybe ? AssertIsNone : "Assert.True(outcome.IsFailure);",
            "Assert.Equal(0, calls);");

        var nullSelector = $"(Func<{string.Join(", ", Enumerable.Repeat("int", file.Arity))}, int>)null!";
        file.Add("throws_when_the_selector_is_null", $"Assert.Throws<ArgumentNullException>(() => {{ _ = {Call(file.Present, nullSelector)}; }});");
    }

    private static void AddNullableSelectorTests(TestFile file)
    {
        file.Add(
            "returns_none_when_the_selector_returns_null",
            $"var outcome = {Call(file.Present, $"({file.Names}) => (string)null!")};",
            string.Empty,
            AssertIsNone);
        file.Add(
            "returns_none_when_the_selector_returns_a_null_nullable_value",
            $"var outcome = {Call(file.Present, $"({file.Names}) => (int?)null")};",
            string.Empty,
            AssertIsNone);
        file.Add(
            "wraps_the_value_when_the_selector_returns_a_nullable_value",
            $"var outcome = {Call(file.Present, $"({file.Names}) => (int?)({file.Sum})")};",
            string.Empty,
            $"Assert.Equal({file.Total}, outcome.GetValue());");

        var nullableSelectorCalls = $"({file.Names}) => {{ calls++; return (int?)({file.Sum}); }}";

        file.Add(
            "does_not_call_a_nullable_value_selector_when_one_maybe_has_no_value",
            "var calls = 0;",
            $"var outcome = {Call(file.Failing(), nullableSelectorCalls)};",
            string.Empty,
            AssertIsNone,
            "Assert.Equal(0, calls);");
    }

    private static string SomeLine(bool selector, bool nullableStruct, string call, string resultType, string values)
    {
        if (nullableStruct)
        {
            return $"    var value = {call};\n    return value.HasValue ? new Maybe<TResult>(value.GetValueOrDefault()) : default;";
        }

        return selector ? $"    return Maybe<TResult>.Of({call});" : $"    return new Maybe<{resultType}>(({values}));";
    }

    private static string Call(string[] arguments, string? selector) =>
        $"{arguments[0]}.Zip({string.Join(", ", selector is null ? arguments.Skip(1) : [.. arguments.Skip(1), selector])})";

    private void WriteHeader(CodeWriter writer)
    {
        writer.Line("// <auto-generated>");
        writer.Line($"//     This file was generated by {profile.ToolNamespace}. Do not edit it by hand:");
        writer.Line($"//     change the generator and run `dotnet run --project {profile.ToolProject}`.");
        writer.Line("// </auto-generated>");
        writer.Line("#nullable enable");
        writer.Line();
    }

    private void WriteDocumentation(CodeWriter writer, int arity, bool selector, bool nullableStruct)
    {
        writer.Line($"/// <summary>{Summary(arity, selector, nullableStruct)}</summary>");

        for (var index = 0; index < arity; index++)
        {
            writer.Line($"/// <typeparam name=\"T{index + 1}\">The type of the value of the {Ordinals[index]} {Noun}.</typeparam>");
        }

        if (selector)
        {
            writer.Line("/// <typeparam name=\"TResult\">The type of the combined value.</typeparam>");
        }

        for (var index = 0; index < arity; index++)
        {
            writer.Line($"/// <param name=\"{Ordinals[index]}\">The {Ordinals[index]} {Noun}.</param>");
        }

        if (selector)
        {
            writer.Line("/// <param name=\"selector\">Combines the values.</param>");
        }

        writer.Line($"/// <returns>{ReturnsText(selector)}</returns>");

        if (selector)
        {
            writer.Line("/// <exception cref=\"ArgumentNullException\"><paramref name=\"selector\"/> is <see langword=\"null\"/>.</exception>");
        }
    }

    private string Summary(int arity, bool selector, bool nullableStruct)
    {
        var how = selector ? "with a function" : "into a tuple";
        var what = IsMaybe
            ? $"Combines the values of {arity} independent maybes {how}, when all of them have a value."
            : $"Combines the values of {arity} independent results {how}, reporting the errors of every one that failed.";

        if (nullableStruct)
        {
            what += " The selector returns a nullable value type; a <see langword=\"null\"/> result becomes none, so the result is a " +
                "<c>Maybe&lt;TResult&gt;</c> and never a <c>Maybe&lt;TResult?&gt;</c>.";
        }

        return what;
    }

    private string ReturnsText(bool selector)
    {
        if (IsMaybe && selector)
        {
            return "A maybe with the combined value, or none when any of them has no value or the selector returns <see langword=\"null\"/>.";
        }

        if (IsMaybe)
        {
            return "A maybe with all the values, or none when any of them has no value.";
        }

        var subject = selector ? "the combined value" : "all the values";

        return $"A successful result with {subject}, or a failed result with the error of the one that failed, " +
            "or an aggregate of the errors (in argument order) when several failed.";
    }

    private string[] Body(int arity, bool selector, string resultType, bool nullableStruct)
    {
        var lines = new List<string>();

        if (selector)
        {
            lines.Add("Guard.NotNull(selector, nameof(selector));");
            lines.Add(string.Empty);
        }

        var values = string.Join(", ", Enumerable.Range(1, arity).Select(index => $"value{index}"));

        lines.AddRange(IsMaybe ? MaybeBody(arity, selector, resultType, nullableStruct, values) : ResultBody(arity, selector, resultType, values));

        return [.. lines];
    }

    private static List<string> MaybeBody(int arity, bool selector, string resultType, bool nullableStruct, string values)
    {
        var condition = string.Join(" && ", Enumerable.Range(0, arity).Select(index => $"{Ordinals[index]}.Unpack(out var value{index + 1})"));
        var lines = new List<string> { $"if ({condition})", "{" };
        lines.AddRange(SomeLine(selector, nullableStruct, $"selector({values})", resultType, values).Split('\n'));
        lines.Add("}");
        lines.Add(string.Empty);
        lines.Add("return default;");

        return lines;
    }

    private static List<string> ResultBody(int arity, bool selector, string resultType, string values)
    {
        var lines = new List<string>();

        for (var index = 0; index < arity; index++)
        {
            lines.Add($"var error{index + 1} = {Ordinals[index]}.Unpack(out var value{index + 1});");
        }

        lines.Add(string.Empty);
        lines.Add($"if ({string.Join(" && ", Enumerable.Range(1, arity).Select(index => $"error{index} is null"))})");
        lines.Add("{");

        if (selector)
        {
            lines.Add($"    var value = selector({values});");
            lines.Add("    return Result<TResult>.Succeeded(value);");
        }
        else
        {
            lines.Add($"    return Result<{resultType}>.Succeeded(({values}));");
        }

        lines.Add("}");
        lines.Add(string.Empty);
        lines.Add($"return ZipFailure.Of<{resultType}>({string.Join(", ", Enumerable.Range(1, arity).Select(index => $"error{index}"))});");

        return lines;
    }

    /// <summary>The state of the test file of one arity: the writer, the arguments and the text that the tests share.</summary>
    private sealed class TestFile
    {
        private readonly CodeWriter _writer;
        private readonly bool _isMaybe;
        private bool _first = true;

        /// <summary>Initializes a new instance of the <see cref="TestFile"/> class.</summary>
        /// <param name="writer">The writer of the file.</param>
        /// <param name="arity">The number of values that are combined.</param>
        /// <param name="isMaybe"><see langword="true"/> for maybes, <see langword="false"/> for results.</param>
        internal TestFile(CodeWriter writer, int arity, bool isMaybe)
        {
            _writer = writer;
            _isMaybe = isMaybe;
            Arity = arity;
            Present = [.. Enumerable.Range(1, arity).Select(index => _isMaybe ? $"Maybe.Some({index})" : $"Result.Success({index})")];
            Names = string.Join(", ", Enumerable.Range(0, arity).Select(index => $"v{index}"));
            Sum = string.Join(" + ", Enumerable.Range(0, arity).Select(index => $"v{index}"));
            ExpectedTuple = $"({string.Join(", ", Enumerable.Range(1, arity))})";
            SelectorCalls = $"({Names}) => {{ calls++; return {Sum}; }}";
            Total = Enumerable.Range(1, arity).Sum();
        }

        /// <summary>Gets the number of values that are combined.</summary>
        internal int Arity { get; }

        /// <summary>Gets the arguments that all have a value.</summary>
        internal string[] Present { get; }

        /// <summary>Gets the parameter names of a selector.</summary>
        internal string Names { get; }

        /// <summary>Gets the sum of the parameters of a selector.</summary>
        internal string Sum { get; }

        /// <summary>Gets the tuple that the combined values are expected to form.</summary>
        internal string ExpectedTuple { get; }

        /// <summary>Gets a selector that counts its calls and returns the sum.</summary>
        internal string SelectorCalls { get; }

        /// <summary>Gets the sum of the values that the arguments hold.</summary>
        internal int Total { get; }

        /// <summary>Gets the arguments of which the last has no value or failed.</summary>
        /// <returns>The arguments.</returns>
        internal string[] Failing()
        {
            var failing = (string[])Present.Clone();
            failing[Arity - 1] = _isMaybe ? "Maybe.None<int>()" : "Result.Failure<int>(Error.Create(\"Failed.\"))";

            return failing;
        }

        /// <summary>Writes one test.</summary>
        /// <param name="name">The part of the name after <c>Zip_of_N_</c>.</param>
        /// <param name="body">The lines of the body.</param>
        internal void Add(string name, params string[] body)
        {
            if (!_first)
            {
                _writer.Line();
            }

            _first = false;
            _writer.Line("[Fact]");
            _writer.Line($"internal void Zip_of_{Arity}_{name}()");
            _writer.Open();
            _writer.Lines(body);
            _writer.Close();
        }
    }
}
