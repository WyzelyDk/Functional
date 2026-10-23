namespace Wyzely.Functional.CodeGeneration;

/// <summary>
/// Writes the <c>SuccessIf</c> and <c>FailureIf</c> factories of a library: every kind of condition (a value, a function, a <c>Task</c> or a
/// <c>ValueTask</c>, and the functions of those) combined with every way to give the error (an error, a message, or a function that creates
/// either, called only when the result fails), plus the tests of all of them. They are static methods of the result type, not extension
/// methods, so they do not go through the operation catalog.
/// </summary>
/// <param name="profile">What differs between the libraries that share the generator.</param>
internal sealed class ConditionalFactoryEmitter(GeneratorProfile profile)
{
    private const string ErrorParameter = "error";
    private const string MessageParameter = "message";
    private const string SuccessIf = "SuccessIf";
    private const string ErrorType = "Error";
    private const string StringType = "string";
    private const string ErrorFactoryType = "Func<Error>";

    private static readonly string[] Methods = [SuccessIf, "FailureIf"];

    private static readonly ConditionForm[] Conditions =
    [
        new("bool", "Bool", "bool", Wrapper.None, Guarded: false, "condition", "{0}"),
        new("func", "Func", "Func<bool>", Wrapper.None, Guarded: true, "condition()", "() => {0}"),
        new("task_func", "TaskFunc", "Func<Task<bool>>", Wrapper.Task, Guarded: true, "await condition()", "async () => {{ await Task.Yield(); return {0}; }}"),
        new(
            "value_task_func",
            "ValueTaskFunc",
            "Func<ValueTask<bool>>",
            Wrapper.ValueTask,
            Guarded: true,
            "await condition()",
            "() => new ValueTask<bool>({0})"),
        new("task", "Task", "Task<bool>", Wrapper.Task, Guarded: true, "await condition", "Task.FromResult({0})"),
        new("value_task", "ValueTask", "ValueTask<bool>", Wrapper.ValueTask, Guarded: false, "await condition", "new ValueTask<bool>({0})"),
    ];

    private static readonly ErrorForm[] Errors =
    [
        new("error", "Error", ErrorType, ErrorParameter, Lazy: false, ErrorParameter, "error", "(Error)null!"),
        new("message", "Message", StringType, MessageParameter, Lazy: false, MessageParameter, "\"Nope.\"", "(string)null!"),
        new(
            "error_factory",
            "ErrorFactory",
            ErrorFactoryType,
            "errorFactory",
            Lazy: true,
            "errorFactory()",
            "() => { factoryCalls++; return error; }",
            "(Func<Error>)null!"),
        new(
            "message_factory",
            "MessageFactory",
            "Func<string>",
            "messageFactory",
            Lazy: true,
            "messageFactory()",
            "() => { factoryCalls++; return \"Nope.\"; }",
            "(Func<string>)null!"),
    ];

    /// <summary>Gets the number of library files this emitter writes.</summary>
    internal static int FileCount => Methods.Length * Conditions.Length * Errors.Length;

    /// <summary>Gets the names of the folders, inside the library directory, that hold the library files.</summary>
    internal static IReadOnlyList<string> Folders => Methods;

    /// <summary>Generates every library file and test file. The keys are paths relative to the repository root, with forward slashes.</summary>
    /// <param name="stubs"><see langword="true"/> to emit bodies that do nothing useful (the red phase).</param>
    /// <returns>The generated files by relative path.</returns>
    internal IEnumerable<KeyValuePair<string, string>> Generate(bool stubs)
    {
        foreach (var method in Methods)
        {
            foreach (var condition in Conditions)
            {
                foreach (var error in Errors)
                {
                    var path = $"{profile.LibraryDirectory}/{method}/{method}.{condition.FileName}.{error.FileName}.g.cs";

                    yield return new(path, EmitLibrary(method, condition, error, stubs));
                }
            }

            yield return new($"{profile.TestDirectory}/{method}MatrixTests.g.cs", EmitTests(method));
        }
    }

    private string Summary(string method, ConditionForm condition)
    {
        var summary = method == SuccessIf
            ? "Creates a successful result when a condition holds and a failed one otherwise."
            : "Creates a failed result when a condition holds and a successful one otherwise.";

        return condition.Returns == Wrapper.None
            ? summary
            : summary + " The condition is awaited without capturing the synchronization context unless " +
                $"<see cref=\"{profile.OptionsClass}.ContinueOnCapturedContext\"/> says otherwise, " +
                $"and the result is returned in a {condition.Returns.Label()}.";
    }

    private static string ConditionDocumentation(string method, ConditionForm condition)
    {
        var meaning = method == SuccessIf ? "The condition that must hold for the result to succeed" : "The condition that makes the result fail";

        return condition.Type switch
        {
            "bool" => meaning + ".",
            "Func<bool>" => meaning + ", as a function that is called once.",
            "Task<bool>" or "ValueTask<bool>" => meaning + ", as an operation that produces it.",
            _ => meaning + ", as an asynchronous function that is called once.",
        };
    }

    private static string ErrorDocumentation(string method, ErrorForm error)
    {
        var when = method == SuccessIf ? "<see langword=\"false\"/>" : "<see langword=\"true\"/>";
        var used = $"used when <paramref name=\"condition\"/> is {when}.";

        return error.Type switch
        {
            ErrorType => $"The error of the failed result, {used}",
            StringType => $"The message of the error of the failed result, {used}",
            ErrorFactoryType => $"Creates the error of the failed result. It is called only when <paramref name=\"condition\"/> is {when}.",
            _ => $"Creates the message of the error of the failed result. It is called only when <paramref name=\"condition\"/> is {when}.",
        };
    }

    private static string ReturnDocumentation(string method, ErrorForm error, ConditionForm condition)
    {
        var failed = error.Type is ErrorType or ErrorFactoryType
            ? $"a failed result that holds <paramref name=\"{error.Parameter}\"/>"
            : $"a failed result whose error was created from <paramref name=\"{error.Parameter}\"/>";
        var text = method == SuccessIf ? $"A successful result, or {failed}." : $"{char.ToUpperInvariant(failed[0])}{failed[1..]}, or a successful result.";

        return condition.Returns == Wrapper.None ? text : text[..^1] + $", in a {condition.Returns.Label()}.";
    }

    private static string Choose(string method, string holds, ErrorForm error) => method == SuccessIf
        ? $"{holds} ? Success() : Failure({error.Evaluate})"
        : $"{holds} ? Failure({error.Evaluate}) : Success()";

    private static string[] Body(string method, ConditionForm condition, ErrorForm error)
    {
        var lines = new List<string>();

        if (condition.Guarded)
        {
            lines.Add("Guard.NotNull(condition, nameof(condition));");
        }

        lines.Add($"Guard.NotNull({error.Parameter}, nameof({error.Parameter}));");
        lines.Add(string.Empty);

        if (condition.Returns == Wrapper.None)
        {
            lines.Add($"return {Choose(method, condition.Evaluate, error)};");

            return [.. lines];
        }

        var result = condition.Returns.Wrap("Result");
        var arguments = new[] { "condition", error.Parameter };
        var parameters = $"{condition.Type} condition, {error.Type} {error.Parameter}";

        lines.Add($"return Core({string.Join(", ", arguments)});");
        lines.Add(string.Empty);
        lines.Add($"static async {result} Core({parameters})");
        lines.Add("{");
        lines.Add($"    var holds = {condition.Evaluate}.ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext);");
        lines.Add(string.Empty);
        lines.Add($"    return {Choose(method, "holds", error)};");
        lines.Add("}");

        return [.. lines];
    }

    private static string TestName(string method, ConditionForm condition, ErrorForm error, string behavior)
    {
        var article = error.Slug.StartsWith('e') ? "an" : "a";

        return $"{method}_with_a_{condition.Slug}_condition_and_{article}_{error.Slug}_{behavior}";
    }

    private static string Escaped(string type) => type.Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    private static string Argument(string template, string value) => string.Format(System.Globalization.CultureInfo.InvariantCulture, template, value);

    private string EmitLibrary(string method, ConditionForm condition, ErrorForm error, bool stubs)
    {
        var writer = new CodeWriter();
        WriteHeader(writer);

        if (condition.Type == "Func<Task<bool>>")
        {
            writer.Line("using System.Runtime.CompilerServices;");
            writer.Line();
        }

        writer.Line("namespace Wyzely.Functional;");
        writer.Line();
        writer.Line("/// <content>");
        writer.Line($"/// The <c>{method}</c> factory for a condition of type <c>{Escaped(condition.Type)}</c>");
        writer.Line($"/// and an error given as <c>{Escaped(error.Type)}</c>.");
        writer.Line("/// </content>");
        writer.Line("public readonly partial struct Result");
        writer.Open();
        writer.Line($"/// <summary>{Summary(method, condition)}</summary>");
        writer.Line($"/// <param name=\"condition\">{ConditionDocumentation(method, condition)}</param>");
        writer.Line($"/// <param name=\"{error.Parameter}\">{ErrorDocumentation(method, error)}</param>");
        writer.Line($"/// <returns>{ReturnDocumentation(method, error, condition)}</returns>");
        WriteExceptions(writer, condition, error);

        if (condition.Type == "Func<Task<bool>>")
        {
            writer.Line("[OverloadResolutionPriority(1)]");
        }

        var returnType = condition.Returns == Wrapper.None ? "Result" : condition.Returns.Wrap("Result");
        writer.Line($"public static {returnType} {method}({condition.Type} condition, {error.Type} {error.Parameter})");
        writer.Open();
        writer.Lines(stubs ? ["return default!;"] : Body(method, condition, error));
        writer.Close();
        writer.Close();

        return writer.ToString();
    }

    private static void WriteExceptions(CodeWriter writer, ConditionForm condition, ErrorForm error)
    {
        var guarded = condition.Guarded ? $"<paramref name=\"condition\"/>, <paramref name=\"{error.Parameter}\"/>" : $"<paramref name=\"{error.Parameter}\"/>";

        writer.Line($"/// <exception cref=\"ArgumentNullException\">{guarded} is <see langword=\"null\"/>.</exception>");

        if (error.Type is StringType or "Func<string>")
        {
            var parameter = $"<paramref name=\"{error.Parameter}\"/>";
            var which = error.Type == StringType ? parameter : $"The message that {parameter} returns";
            writer.Line($"/// <exception cref=\"ArgumentException\">");
            writer.Line($"/// {which} is empty or consists only of white space, and the result is a failure.");
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

    private string EmitTests(string method)
    {
        var writer = new CodeWriter();
        WriteHeader(writer);
        writer.Line($"namespace {profile.TestNamespace};");
        writer.Line();
        writer.Line($"public sealed class {method}MatrixTests");
        writer.Open();

        var first = true;

        foreach (var condition in Conditions)
        {
            foreach (var error in Errors)
            {
                foreach (var (name, lines) in Tests(method, condition, error))
                {
                    if (!first)
                    {
                        writer.Line();
                    }

                    first = false;
                    writer.Line("[Fact]");
                    var awaits = lines.Any(line => line.Contains("= await ", StringComparison.Ordinal));
                    writer.Line(awaits ? $"internal async Task {name}()" : $"internal void {name}()");
                    writer.Open();
                    writer.Lines(profile.TestPreamble);
                    writer.Lines(lines);
                    writer.Close();
                }
            }
        }

        writer.Close();

        return writer.ToString();
    }

    private static IEnumerable<(string Name, string[] Lines)> Tests(string method, ConditionForm condition, ErrorForm error)
    {
        var succeedsWhen = method == SuccessIf ? "true" : "false";
        var failsWhen = method == SuccessIf ? "false" : "true";
        var holds = method == SuccessIf ? "holds" : "does_not_hold";
        var fails = method == SuccessIf ? "does_not_hold" : "holds";
        var awaited = condition.Returns == Wrapper.None ? string.Empty : "await ";
        var counter = error.Lazy ? new[] { "var factoryCalls = 0;" } : [];
        var failure = error.Lazy ? new[] { "Assert.Equal(1, factoryCalls);" } : [];
        var noCalls = error.Lazy ? new[] { "Assert.Equal(0, factoryCalls);" } : [];
        var failureAssert = error.Type is ErrorType or ErrorFactoryType
            ? "Assert.Same(error, outcome.GetError());"
            : "Assert.Equal(\"Nope.\", outcome.GetError().Message);";

        string[] Run(string conditionExpression, params string[] asserts) =>
        [
            .. counter,
            $"var outcome = {awaited}Result.{method}({conditionExpression}, {error.Test});",
            string.Empty,
            .. asserts,
        ];

        yield return (
            TestName(method, condition, error, $"returns_a_success_when_the_condition_{holds}"),
            Run(Argument(condition.Test, succeedsWhen), ["Assert.True(outcome.IsSuccess);", .. noCalls]));

        yield return (
            TestName(method, condition, error, $"returns_a_failure_when_the_condition_{fails}"),
            Run(Argument(condition.Test, failsWhen), [failureAssert, .. failure]));

        if (condition.Type == "Task<bool>")
        {
            yield return (
                TestName(method, condition, error, $"returns_a_failure_when_the_condition_{fails}_and_completes_later"),
                Run($"Pending.Of({failsWhen})", [failureAssert, .. failure]));
        }

        var nullError = $"var exception = Record.Exception(() => {{ _ = Result.{method}({Argument(condition.Test, succeedsWhen)}, {error.NullArgument}); }});";

        yield return (
            TestName(method, condition, error, $"throws_when_the_{error.Parameter}_is_null"),
            [nullError, string.Empty, $"Assert.Equal(\"{error.Parameter}\", Assert.IsType<ArgumentNullException>(exception).ParamName);"]);

        if (condition.Guarded)
        {
            var nullCondition = $"var exception = Record.Exception(() => {{ _ = Result.{method}(({condition.Type})null!, {error.Test}); }});";

            yield return (
                TestName(method, condition, error, "throws_when_the_condition_is_null"),
                [.. counter, nullCondition, string.Empty, "Assert.Equal(\"condition\", Assert.IsType<ArgumentNullException>(exception).ParamName);"]);
        }
    }

    // How a kind of condition is spelled: in the file names and tests, in the signature, and in the body.
    private sealed record ConditionForm(string Slug, string FileName, string Type, Wrapper Returns, bool Guarded, string Evaluate, string Test);

    // How an error is given: an error, a message, or a function that creates either.
    private sealed record ErrorForm(string Slug, string FileName, string Type, string Parameter, bool Lazy, string Evaluate, string Test, string NullArgument);
}
