namespace Wyzely.Functional.CodeGeneration;

/// <summary>
/// Writes the <c>SuccessIfNotNull</c> factories of a library: a result that holds a value that is not <see langword="null"/>, and a failure
/// otherwise. The value is given as it is, in a <c>Task</c> or in a <c>ValueTask</c>, for a reference type and for a nullable value type, and the
/// error as an error, a message, or a function that creates the error only when it is needed. The tests are generated with them.
/// </summary>
/// <param name="profile">What differs between the libraries that share the generator.</param>
internal sealed class NotNullFactoryEmitter(GeneratorProfile profile)
{
    private const string MethodName = "SuccessIfNotNull";
    private const string ErrorType = "Error";
    private const string StringType = "string";
    private const string ErrorFactoryType = "Func<Error>";
    private const string ErrorParameter = "error";
    private const string MessageParameter = "message";
    private const string ErrorFactoryParameter = "errorFactory";

    private static readonly ValueForm[] Values =
    [
        new("value", "Value", Wrapper.None, "{0}", "{0}", "await value"),
        new("task", "Task", Wrapper.Task, "Task<{0}>", "Task.FromResult<{0}>({1})", "await value"),
        new("value_task", "ValueTask", Wrapper.ValueTask, "ValueTask<{0}>", "new ValueTask<{0}>({1})", "await value"),
    ];

    private static readonly ErrorForm[] Errors =
    [
        new("error", "Error", ErrorType, ErrorParameter, ErrorParameter, ErrorParameter, Lazy: false),
        new("message", "Message", StringType, MessageParameter, MessageParameter, "\"Nope.\"", Lazy: false),
        new("error_factory", "ErrorFactory", ErrorFactoryType, ErrorFactoryParameter, "errorFactory()", "() => { factoryCalls++; return error; }", Lazy: true),
    ];

    /// <summary>Gets the number of library files this emitter writes.</summary>
    internal static int FileCount => Values.Length * 2 * Errors.Length;

    /// <summary>Gets the name of the folder, inside the library directory, that holds the library files.</summary>
    internal static string Folder => MethodName;

    /// <summary>Generates every library file and the test file. The keys are paths relative to the repository root, with forward slashes.</summary>
    /// <param name="stubs"><see langword="true"/> to emit bodies that do nothing useful (the red phase).</param>
    /// <returns>The generated files by relative path.</returns>
    internal IEnumerable<KeyValuePair<string, string>> Generate(bool stubs)
    {
        foreach (var value in Values)
        {
            foreach (var isStruct in new[] { false, true })
            {
                foreach (var error in Errors)
                {
                    var kind = isStruct ? "Struct" : "Class";
                    var path = $"{profile.LibraryDirectory}/{MethodName}/{MethodName}.{kind}.{value.FileName}.{error.FileName}.g.cs";

                    yield return new(path, EmitLibrary(value, isStruct, error, stubs));
                }
            }
        }

        yield return new($"{profile.TestDirectory}/{MethodName}MatrixTests.g.cs", EmitTests());
    }

    private static string ParameterType(ValueForm value) =>
        value.Returns == Wrapper.None ? "T?" : string.Format(System.Globalization.CultureInfo.InvariantCulture, value.Type, "T?");

    private static string Escaped(string text) => text.Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    private static string[] Body(ValueForm value, bool isStruct, ErrorForm error)
    {
        var lines = new List<string>();

        if (value.Returns is Wrapper.Task)
        {
            lines.Add("Guard.NotNull(value, nameof(value));");
        }

        lines.Add($"Guard.NotNull({error.Parameter}, nameof({error.Parameter}));");
        lines.Add(string.Empty);

        var failure = $"Failure<T>({error.Evaluate})";
        var choose = isStruct
            ? $"resolved.HasValue ? Success(resolved.GetValueOrDefault()) : {failure}"
            : $"resolved is null ? {failure} : Success<T>(resolved)";

        if (value.Returns == Wrapper.None)
        {
            lines.Add($"return {choose.Replace("resolved", "value", StringComparison.Ordinal)};");

            return [.. lines];
        }

        var returnType = value.Returns.Wrap("Result<T>");
        var parameters = $"{string.Format(System.Globalization.CultureInfo.InvariantCulture, value.Type, "T?")} value, {error.Type} {error.Parameter}";

        lines.Add($"return Core(value, {error.Parameter});");
        lines.Add(string.Empty);
        lines.Add($"static async {returnType} Core({parameters})");
        lines.Add("{");
        lines.Add("    var resolved = await value.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);");
        lines.Add(string.Empty);
        lines.Add($"    return {choose};");
        lines.Add("}");

        return [.. lines];
    }

    private string EmitLibrary(ValueForm value, bool isStruct, ErrorForm error, bool stubs)
    {
        var writer = new CodeWriter();
        WriteHeader(writer);

        if (value.Returns != Wrapper.None)
        {
            writer.Line("using System.Runtime.CompilerServices;");
            writer.Line();
        }

        var parameterType = ParameterType(value);

        writer.Line("namespace Wyzely.Functional;");
        writer.Line();
        writer.Line("/// <content>");
        var typeKind = isStruct ? "a nullable value type" : "a reference type";

        writer.Line($"/// The <c>{MethodName}</c> factory for a value of type <c>{Escaped(parameterType)}</c> ({typeKind})");
        writer.Line($"/// and an error given as <c>{Escaped(error.Type)}</c>.");
        writer.Line("/// </content>");
        writer.Line("public readonly partial struct Result");
        writer.Open();
        writer.Line($"/// <summary>{Summary(value)}</summary>");
        writer.Line("/// <typeparam name=\"T\">The type of the value, without the nullable annotation.</typeparam>");
        writer.Line($"/// <param name=\"value\">{ValueDocumentation(value)}</param>");
        writer.Line($"/// <param name=\"{error.Parameter}\">{ErrorDocumentation(error)}</param>");
        writer.Line($"/// <returns>{ReturnDocumentation(value, error)}</returns>");
        WriteExceptions(writer, value, error);

        if (value.Returns != Wrapper.None)
        {
            writer.Line("[OverloadResolutionPriority(1)]");
        }

        var returnType = value.Returns == Wrapper.None ? "Result<T>" : value.Returns.Wrap("Result<T>");
        writer.Line($"public static {returnType} {MethodName}<T>({parameterType} value, {error.Type} {error.Parameter})");
        writer.Indent();
        writer.Line(isStruct ? "where T : struct" : "where T : class");
        writer.Unindent();
        writer.Open();
        writer.Lines(stubs ? ["return default;"] : Body(value, isStruct, error));
        writer.Close();
        writer.Close();

        return writer.ToString();
    }

    private string Summary(ValueForm value)
    {
        const string summary = "Creates a successful result that holds a value when it is not <see langword=\"null\"/>, and a failed result otherwise.";

        return value.Returns == Wrapper.None
            ? summary
            : summary + " The value is awaited without capturing the synchronization context unless " +
                $"<see cref=\"{profile.OptionsClass}.ContinueOnCapturedContext\"/> says otherwise, and the result is returned in a {value.Returns.Label()}.";
    }

    private static string ValueDocumentation(ValueForm value) => value.Returns == Wrapper.None
        ? "The value, which may be <see langword=\"null\"/>."
        : $"The operation that produces the value, which may be <see langword=\"null\"/>. It must not be <see langword=\"null\"/> itself.";

    private static string ErrorDocumentation(ErrorForm error) => error.Type switch
    {
        ErrorType => "The error of the failed result, used when the value is <see langword=\"null\"/>.",
        StringType => "The message of the error of the failed result, used when the value is <see langword=\"null\"/>.",
        _ => "Creates the error of the failed result. It is called only when the value is <see langword=\"null\"/>.",
    };

    private static string ReturnDocumentation(ValueForm value, ErrorForm error)
    {
        var failed = error.Type == StringType
            ? $"a failed result whose error was created from <paramref name=\"{error.Parameter}\"/>"
            : $"a failed result that holds the error of <paramref name=\"{error.Parameter}\"/>";
        var text = $"A successful result that holds the value, or {failed}";

        return value.Returns == Wrapper.None ? text + "." : text + $", in a {value.Returns.Label()}.";
    }

    private static void WriteExceptions(CodeWriter writer, ValueForm value, ErrorForm error)
    {
        var errorReference = $"<paramref name=\"{error.Parameter}\"/>";
        var guarded = value.Returns == Wrapper.Task ? $"<paramref name=\"value\"/>, {errorReference}" : errorReference;

        writer.Line($"/// <exception cref=\"ArgumentNullException\">{guarded} is <see langword=\"null\"/>.</exception>");

        if (error.Type == StringType || error.Parameter == ErrorFactoryParameter)
        {
            var parameter = $"<paramref name=\"{error.Parameter}\"/>";
            var which = error.Type == StringType ? parameter : $"The error that {parameter} returns";
            var what = error.Type == StringType ? "is empty or consists only of white space" : "is <see langword=\"null\"/>";
            var exception = error.Type == StringType ? "ArgumentException" : "ArgumentNullException";

            writer.Line($"/// <exception cref=\"{exception}\">");
            writer.Line($"/// {which} {what}, and the value is <see langword=\"null\"/>.");
            writer.Line("/// </exception>");
        }
    }

    private void WriteHeader(CodeWriter writer)
    {
        writer.Line("// <auto-generated>");
        writer.Line($"//     This file was generated by {profile.ToolNamespace}. Do not edit it by hand:");
        writer.Line($"//     change the generator and run `dotnet run --project {profile.ToolProject}`.");
        writer.Line("// </auto-generated>");
        writer.Line("#nullable enable");
        writer.Line();
    }

    private string EmitTests()
    {
        var writer = new CodeWriter();
        WriteHeader(writer);
        writer.Line($"namespace {profile.TestNamespace};");
        writer.Line();
        writer.Line($"public sealed class {MethodName}MatrixTests");
        writer.Open();

        var first = true;

        foreach (var value in Values)
        {
            foreach (var isStruct in new[] { false, true })
            {
                foreach (var error in Errors)
                {
                    foreach (var (name, lines) in Tests(value, isStruct, error))
                    {
                        if (!first)
                        {
                            writer.Line();
                        }

                        first = false;
                        var awaits = lines.Any(line => line.Contains("= await ", StringComparison.Ordinal));
                        writer.Line("[Fact]");
                        writer.Line(awaits ? $"internal async Task {name}()" : $"internal void {name}()");
                        writer.Open();
                        writer.Lines(profile.TestPreamble);
                        writer.Lines(lines);
                        writer.Close();
                    }
                }
            }
        }

        writer.Close();

        return writer.ToString();
    }

    private static List<(string Name, string[] Lines)> Tests(ValueForm value, bool isStruct, ErrorForm error)
    {
        var cases = new TestCases(value, isStruct, error);
        var tests = new List<(string Name, string[] Lines)> { cases.Success(), cases.Failure() };

        if (value.Returns == Wrapper.Task)
        {
            tests.Add(cases.PendingFailure());
        }

        tests.Add(cases.NullError());

        if (value.Returns == Wrapper.Task)
        {
            tests.Add(cases.NullTask());
        }

        return tests;
    }

    /// <summary>The test cases for one combination of value and error.</summary>
    private sealed class TestCases
    {
        private readonly ValueForm _value;
        private readonly bool _isStruct;
        private readonly ErrorForm _error;
        private readonly string[] _counter;
        private readonly string[] _noCalls;
        private readonly string[] _oneCall;
        private readonly string _failureAssert;

        /// <summary>Initializes a new instance of the <see cref="TestCases"/> class.</summary>
        /// <param name="value">How the value is given.</param>
        /// <param name="isStruct"><see langword="true"/> for a nullable value type, <see langword="false"/> for a reference type.</param>
        /// <param name="error">How the error is given.</param>
        internal TestCases(ValueForm value, bool isStruct, ErrorForm error)
        {
            _value = value;
            _isStruct = isStruct;
            _error = error;
            _counter = error.Lazy ? ["var factoryCalls = 0;"] : [];
            _noCalls = error.Lazy ? ["Assert.Equal(0, factoryCalls);"] : [];
            _oneCall = error.Lazy ? ["Assert.Equal(1, factoryCalls);"] : [];
            _failureAssert = error.Type == StringType ? "Assert.Equal(\"Nope.\", outcome.GetError().Message);" : "Assert.Same(error, outcome.GetError());";
        }

        /// <summary>Creates the test for a value that is not null.</summary>
        /// <returns>The name and the lines of the test.</returns>
        internal (string Name, string[] Lines) Success()
        {
            var present = _isStruct ? "5" : "\"a\"";

            return (
                Name("returns_a_success_with_the_value_when_it_is_not_null"),
                Run(Present(), ["Assert.True(outcome.IsSuccess);", $"Assert.Equal({present}, outcome.GetValue());", .. _noCalls]));
        }

        /// <summary>Creates the test for a value that is null.</summary>
        /// <returns>The name and the lines of the test.</returns>
        internal (string Name, string[] Lines) Failure() =>
            (Name("returns_a_failure_when_the_value_is_null"), Run(Wrap(_isStruct ? "(int?)null" : "(string?)null"), [_failureAssert, .. _oneCall]));

        /// <summary>Creates the test for a task that completes later with null.</summary>
        /// <returns>The name and the lines of the test.</returns>
        internal (string Name, string[] Lines) PendingFailure()
        {
            var pending = _isStruct ? "Pending.Of((int?)null)" : "Pending.Of((string?)null)";

            return (Name("returns_a_failure_when_the_value_is_null_and_completes_later"), Run(pending, [_failureAssert, .. _oneCall]));
        }

        /// <summary>Creates the test for an error argument that is null.</summary>
        /// <returns>The name and the lines of the test.</returns>
        internal (string Name, string[] Lines) NullError()
        {
            var nullError = $"var exception = Record.Exception(() => {{ _ = Result.{MethodName}({Present()}, {NullArgument()}); }});";

            return (
                Name($"throws_when_the_{_error.Parameter}_is_null"),
                [nullError, string.Empty, $"Assert.Equal(\"{_error.Parameter}\", Assert.IsType<ArgumentNullException>(exception).ParamName);"]);
        }

        /// <summary>Creates the test for a value task argument that is null.</summary>
        /// <returns>The name and the lines of the test.</returns>
        internal (string Name, string[] Lines) NullTask()
        {
            var type = _isStruct ? "int?" : "string?";

            return (
                Name("throws_when_the_value_task_is_null"),
                [
                    .. _counter,
                    $"var exception = Record.Exception(() => {{ _ = Result.{MethodName}((Task<{type}>)null!, {_error.Test}); }});",
                    string.Empty,
                    "Assert.Equal(\"value\", Assert.IsType<ArgumentNullException>(exception).ParamName);",
                ]);
        }

        private string Name(string behavior)
        {
            var article = _error.Slug.StartsWith('e') ? "an" : "a";

            return $"{MethodName}_with_a_{(_isStruct ? "nullable_value_type" : "reference_type")}_{_value.Slug}_and_{article}_{_error.Slug}_{behavior}";
        }

        private string Present() => Wrap(_isStruct ? "5" : "\"a\"");

        // A value as the test passes it: itself, or in a completed task.
        private string Wrap(string expression)
        {
            var type = _isStruct ? "int?" : "string?";

            if (_value.Returns != Wrapper.None)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, _value.Test, type, expression);
            }

            return expression.StartsWith('(') ? expression : $"({type}){expression}";
        }

        private string NullArgument() => _error.Type switch
        {
            ErrorType => "(Error)null!",
            StringType => "(string)null!",
            _ => "(Func<Error>)null!",
        };

        private string[] Run(string valueExpression, string[] asserts)
        {
            var awaited = _value.Returns == Wrapper.None ? string.Empty : "await ";

            return
            [
                .. _counter,
                $"var outcome = {awaited}Result.{MethodName}({valueExpression}, {_error.Test});",
                string.Empty,
                .. asserts,
            ];
        }
    }

    // How a value is given: itself, in a Task or in a ValueTask. Type and Test are formats of the nullable type (and of the expression, for Test).
    private sealed record ValueForm(string Slug, string FileName, Wrapper Returns, string Type, string Test, string Evaluate);

    // How an error is given: an error, a message, or a function that creates the error.
    private sealed record ErrorForm(string Slug, string FileName, string Type, string Parameter, string Evaluate, string Test, bool Lazy);
}
