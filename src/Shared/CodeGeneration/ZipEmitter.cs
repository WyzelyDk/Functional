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
        var tuple = $"({string.Join(", ", valueTypes.Select((type, index) => $"{type} {TupleNames[index]}"))})";
        var resultType = selector ? "TResult" : tuple;
        var parameters = valueTypes.Select((type, index) => $"{TypeName}<{type}> {Ordinals[index]}").ToList();

        if (selector)
        {
            parameters.Add($"Func<{string.Join(", ", valueTypes)}, TResult{(IsMaybe ? "?" : string.Empty)}> selector");
        }

        writer.Line($"public static {TypeName}<{resultType}> Zip<{string.Join(", ", typeParameters)}>(this {string.Join(", ", parameters)})");

        if (IsMaybe)
        {
            writer.Indent();

            foreach (var typeParameter in typeParameters)
            {
                writer.Line(nullableStruct && typeParameter == "TResult" ? "where TResult : struct" : $"where {typeParameter} : notnull");
            }

            writer.Unindent();
        }

        writer.Open();
        writer.Lines(stubs ? ["return default!;"] : Body(arity, selector, resultType, nullableStruct));
        writer.Close();
        writer.Close();

        return writer.ToString();
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

        var present = Enumerable.Range(1, arity).Select(index => IsMaybe ? $"Maybe.Some({index})" : $"Result.Success({index})").ToArray();
        var names = string.Join(", ", Enumerable.Range(0, arity).Select(index => $"v{index}"));
        var sum = string.Join(" + ", Enumerable.Range(0, arity).Select(index => $"v{index}"));
        var expectedTuple = $"({string.Join(", ", Enumerable.Range(1, arity))})";
        var selectorCalls = $"({names}) => {{ calls++; return {sum}; }}";
        var first = true;

        void Test(string name, params string[] body)
        {
            if (!first)
            {
                writer.Line();
            }

            first = false;
            writer.Line("[Fact]");
            writer.Line($"internal void Zip_of_{arity}_{name}()");
            writer.Open();
            writer.Lines(body);
            writer.Close();
        }

        Test(
            "combines_the_values_into_a_tuple_when_all_have_a_value",
            $"var outcome = {Call(present, null)};",
            string.Empty,
            $"Assert.Equal({expectedTuple}, outcome.GetValue());");

        for (var index = 0; index < arity; index++)
        {
            var arguments = (string[])present.Clone();

            if (IsMaybe)
            {
                arguments[index] = "Maybe.None<int>()";
                Test(
                    $"returns_none_when_the_{Ordinals[index]}_maybe_has_no_value",
                    $"var outcome = {Call(arguments, null)};",
                    string.Empty,
                    AssertIsNone);
            }
            else
            {
                arguments[index] = "Result.Failure<int>(error)";
                Test(
                    $"returns_the_error_when_the_{Ordinals[index]}_result_failed",
                    "var error = Error.Create(\"Failed.\");",
                    $"var outcome = {Call(arguments, null)};",
                    string.Empty,
                    "Assert.Same(error, outcome.GetError());");
            }
        }

        var failing = (string[])present.Clone();
        failing[arity - 1] = IsMaybe ? "Maybe.None<int>()" : "Result.Failure<int>(Error.Create(\"Failed.\"))";

        if (!IsMaybe)
        {
            var several = (string[])present.Clone();
            several[0] = "Result.Failure<int>(first)";
            several[arity - 1] = "Result.Failure<int>(last)";
            var defaulted = (string[])present.Clone();
            defaulted[arity - 1] = "default(Result<int>)";

            Test(
                "returns_an_aggregate_of_all_errors_in_order_when_several_results_failed",
                "var first = Error.Create(\"First failed.\");",
                "var last = Error.Create(\"Last failed.\");",
                $"var outcome = {Call(several, null)};",
                string.Empty,
                "var error = outcome.GetError();",
                "Assert.True(error.IsAggregate);",
                "Assert.Equal([first, last], error.Children);");

            Test(
                "treats_a_default_result_as_a_success",
                $"var outcome = {Call(defaulted, null)};",
                string.Empty,
                "Assert.True(outcome.IsSuccess);");
        }

        Test(
            "calls_the_selector_with_the_values_when_all_have_a_value",
            "var calls = 0;",
            $"var outcome = {Call(present, selectorCalls)};",
            string.Empty,
            $"Assert.Equal({Enumerable.Range(1, arity).Sum()}, outcome.GetValue());",
            "Assert.Equal(1, calls);");

        Test(
            $"does_not_call_the_selector_when_one_{Noun}_{(IsMaybe ? "has_no_value" : "failed")}",
            "var calls = 0;",
            $"var outcome = {Call(failing, selectorCalls)};",
            string.Empty,
            IsMaybe ? AssertIsNone : "Assert.True(outcome.IsFailure);",
            "Assert.Equal(0, calls);");

        var nullSelector = $"(Func<{string.Join(", ", Enumerable.Repeat("int", arity))}, int>)null!";
        Test("throws_when_the_selector_is_null", $"Assert.Throws<ArgumentNullException>(() => {{ _ = {Call(present, nullSelector)}; }});");

        if (IsMaybe)
        {
            Test(
                "returns_none_when_the_selector_returns_null",
                $"var outcome = {Call(present, $"({names}) => (string)null!")};",
                string.Empty,
                AssertIsNone);
            Test(
                "returns_none_when_the_selector_returns_a_null_nullable_value",
                $"var outcome = {Call(present, $"({names}) => (int?)null")};",
                string.Empty,
                AssertIsNone);
            Test(
                "wraps_the_value_when_the_selector_returns_a_nullable_value",
                $"var outcome = {Call(present, $"({names}) => (int?)({sum})")};",
                string.Empty,
                $"Assert.Equal({Enumerable.Range(1, arity).Sum()}, outcome.GetValue());");

            var nullableSelectorCalls = $"({names}) => {{ calls++; return (int?)({sum}); }}";

            Test(
                "does_not_call_a_nullable_value_selector_when_one_maybe_has_no_value",
                "var calls = 0;",
                $"var outcome = {Call(failing, nullableSelectorCalls)};",
                string.Empty,
                AssertIsNone,
                "Assert.Equal(0, calls);");
        }

        writer.Close();

        return writer.ToString();
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
        var how = selector ? "with a function" : "into a tuple";
        var what = IsMaybe
            ? $"Combines the values of {arity} independent maybes {how}, when all of them have a value."
            : $"Combines the values of {arity} independent results {how}, reporting the errors of every one that failed.";

        if (nullableStruct)
        {
            what += " The selector returns a nullable value type; a <see langword=\"null\"/> result becomes none, so the result is a " +
                "<c>Maybe&lt;TResult&gt;</c> and never a <c>Maybe&lt;TResult?&gt;</c>.";
        }

        writer.Line($"/// <summary>{what}</summary>");

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

        string returns;

        if (IsMaybe && selector)
        {
            returns = "A maybe with the combined value, or none when any of them has no value or the selector returns <see langword=\"null\"/>.";
        }
        else if (IsMaybe)
        {
            returns = "A maybe with all the values, or none when any of them has no value.";
        }
        else
        {
            var subject = selector ? "the combined value" : "all the values";
            returns = $"A successful result with {subject}, or a failed result with the error of the one that failed, " +
                "or an aggregate of the errors (in argument order) when several failed.";
        }

        writer.Line($"/// <returns>{returns}</returns>");

        if (selector)
        {
            writer.Line("/// <exception cref=\"ArgumentNullException\"><paramref name=\"selector\"/> is <see langword=\"null\"/>.</exception>");
        }
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
        var call = $"selector({values})";

        if (IsMaybe)
        {
            var condition = string.Join(" && ", Enumerable.Range(0, arity).Select(index => $"{Ordinals[index]}.Unpack(out var value{index + 1})"));
            lines.Add($"if ({condition})");
            lines.Add("{");
            lines.AddRange(SomeLine(selector, nullableStruct, call, resultType, values).Split('\n'));
            lines.Add("}");
            lines.Add(string.Empty);
            lines.Add("return default;");

            return [.. lines];
        }

        for (var index = 0; index < arity; index++)
        {
            lines.Add($"var error{index + 1} = {Ordinals[index]}.Unpack(out var value{index + 1});");
        }

        lines.Add(string.Empty);
        lines.Add($"if ({string.Join(" && ", Enumerable.Range(1, arity).Select(index => $"error{index} is null"))})");
        lines.Add("{");

        if (selector)
        {
            lines.Add($"    var value = {call};");
            lines.Add("    return Result<TResult>.Succeeded(value);");
        }
        else
        {
            lines.Add($"    return Result<{resultType}>.Succeeded(({values}));");
        }

        lines.Add("}");
        lines.Add(string.Empty);
        lines.Add($"return ZipFailure.Of<{resultType}>({string.Join(", ", Enumerable.Range(1, arity).Select(index => $"error{index}"))});");

        return [.. lines];
    }
}
