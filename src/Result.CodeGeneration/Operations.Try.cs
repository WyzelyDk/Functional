using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// The <c>*Try</c> family: <c>MapTry</c>, <c>BindTry</c>, <c>TapTry</c> and <c>TapIfTry</c>, which run a delegate that may throw and turn the
/// exception into a failed result, like <c>Result.Try</c>. Each exists with a handler for any exception (which follows <c>ShouldCatch</c>) and with a
/// handler for the one kind of exception that its parameter names.
/// </content>
internal static partial class Operations
{
    private const string OnExceptionName = "onException";
    private const string WhenName = "when";
    private const string TExceptionName = "TException";
    private const string MapTryName = "MapTry";
    private const string BindTryName = "BindTry";
    private const string TapTryName = "TapTry";
    private const string TapIfTryName = "TapIfTry";
    private const string ReceiverFailedFailure = "Result<TResult>.Failed(failure)";
    private const string HandledSetup = """var handled = Error.Create("Handled.");""";
    private const string AssertSameHandledOutcomeGetError = "Assert.Same(handled, outcome.GetError());";
    private const string NamedHandler = "(InvalidOperationException _) => handled";
    private const string ReceiverPassesThrough = " A failed receiver passes through and the delegate is not called.";
    private const string ReturnReceiverOrFailure = "The receiver, or a failed result when the delegate throws.";
    private const string ReturnDelegateOrFailure = "The result of the delegate, or a failed result.";
    private const string BoomThrowStatement = "Boom.Throw();";
    private const string AssertTapCalledOnce = "Assert.Equal(1, tapCalls);";
    private const string FailureOfResultOfTResult = "Result.Failure<TResult>({0})";
    private const string FailureOfResult = "Result.Failure({0})";

    private const string ExceptionTypeDoc =
        "The type of the exception to catch, which is the type of the parameter of <paramref name=\"onException\"/>.";

    private const string BoolConditionDoc = "Whether the delegate runs. When it is <see langword=\"false\"/> the receiver is returned as it was.";

    private const string DefaultExceptionSentence =
        " A thrown exception becomes a failed result unless <see cref=\"ResultOptions.ShouldCatch\"/> excludes it, as a cancellation is by default.";

    private const string NamedExceptionSentence =
        " Only an exception of type <typeparamref name=\"TException\"/>, or one derived from it, becomes a failed result; " +
        "any other exception keeps travelling.";

    // A method, not a field: the order in which static fields of partial files are initialized is not defined, and All reads this.
    private static TryShape[] CreateTryShapes() =>
    [
        new()
        {
            Name = MapTryName,
            Typed = true,
            Summary = "Transforms the value of a successful result with a function that may throw.",
            TypeParameters = ["T", TResultName],
            TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value that the delegate produces.")],
            Delegate = new("map", ["T"], TResultName, "Computes the new value from the value of the successful result."),
            ReturnType = ResultOfTResult,
            ReturnDescription = "A successful result that holds the value computed by the delegate, or a failed result.",
            OnReceiverFailed = _ => ReceiverFailedFailure,
            FailureFormat = FailureOfResultOfTResult,
            Act = ctx => [$"var value = {ctx.Call("map", InputName)};", "return Result<TResult>.Succeeded(value);"],
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns("map", ["x"], "x * 10", "int"),
            ThrowingTest = Returns("map", ["x"], "Boom.Throw<int>()", "int"),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(20, outcome.GetValue());", "Assert.Equal(1, mapCalls);"],
            OtherAsserts = [AssertSameErrorOutcomeGetError, "Assert.Equal(0, mapCalls);"],
        },
        new()
        {
            Name = BindTryName,
            StemPart = "BindTryToResultOfT",
            Typed = true,
            Summary = "Chains an operation that can fail and returns a value, and may also throw.",
            TypeParameters = ["T", TResultName],
            TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value of the result that the delegate returns.")],
            Delegate = new("bind", ["T"], ResultOfTResult, "The operation to run with the value of the successful result."),
            ReturnType = ResultOfTResult,
            ReturnDescription = ReturnDelegateOrFailure,
            OnReceiverFailed = _ => ReceiverFailedFailure,
            FailureFormat = FailureOfResultOfTResult,
            Act = ctx => [$"return {ctx.Call("bind", InputName)};"],
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns("bind", ["x"], "Result.Success(x * 10)", ResultOfInt),
            ThrowingTest = Returns("bind", ["x"], "Boom.Throw<Result<int>>()", ResultOfInt),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(20, outcome.GetValue());", AssertEqual1BindCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls],
        },
        new()
        {
            Name = BindTryName,
            StemPart = "BindTryToResult",
            Typed = true,
            Summary = "Chains an operation that can fail and returns no value, and may also throw.",
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", TDoc)],
            Delegate = new("bind", ["T"], ResultType, "The operation to run with the value of the successful result."),
            ReturnType = ResultType,
            ReturnDescription = ReturnDelegateOrFailure,
            OnReceiverFailed = _ => "new Result(failure)",
            FailureFormat = FailureOfResult,
            Act = ctx => [$"return {ctx.Call("bind", InputName)};"],
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns("bind", ["x"], "Result.Failure(other)", ResultType),
            ThrowingTest = Returns("bind", ["x"], "Boom.Throw<Result>()", ResultType),
            Setup = [OtherErrorSetup],
            RunAsserts = [AssertSameOtherOutcomeGetError, AssertEqual1BindCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls],
        },
        new()
        {
            Name = TapTryName,
            Typed = true,
            KeepsValue = true,
            Summary = "Runs a side effect that may throw with the value of a successful result, and returns the result unchanged when it does not.",
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", TDoc)],
            Delegate = new("tap", ["T"], "void", "The side effect to run with the value of the successful result."),
            ReturnType = ResultOfT,
            ReturnDescription = ReturnReceiverOrFailure,
            OnReceiverFailed = ctx => ctx.R,
            FailureFormat = "Result.Failure<T>({0})",
            Act = ctx => [ctx.CallStatement("tap", InputName), $"return {ctx.R};"],
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Does("tap", ["x"], "seen = x;"),
            ThrowingTest = Does("tap", ["x"], BoomThrowStatement),
            Setup = ["var seen = 0;"],
            RunAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(2, seen);", AssertTapCalledOnce],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoTapCalls],
        },
        new()
        {
            Name = TapIfTryName,
            Typed = true,
            Conditional = true,
            KeepsValue = true,
            Summary = "Runs a side effect that may throw with the value of a successful result when a condition holds, and returns the result " +
                "unchanged when it does not throw.",
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", TDoc)],
            Delegate = new("tap", ["T"], "void", "The side effect to run with the value of the successful result."),
            ReturnType = ResultOfT,
            ReturnDescription = ReturnReceiverOrFailure,
            OnReceiverFailed = ctx => ctx.R,
            FailureFormat = "Result.Failure<T>({0})",
            Act = ctx => [ctx.CallStatement("tap", InputName), $"return {ctx.R};"],
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Does("tap", ["x"], "seen = x;"),
            ThrowingTest = Does("tap", ["x"], BoomThrowStatement),
            Setup = ["var seen = 0;"],
            RunAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(2, seen);", AssertTapCalledOnce],
            SkipAsserts = [AssertEqual2OutcomeGetValue, "Assert.Equal(0, seen);", AssertNoTapCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoTapCalls],
        },
        new()
        {
            Name = MapTryName,
            Typed = false,
            Summary = "Turns a successful result without a value into a result with a value, computed by a function that may throw.",
            TypeParameters = [TResultName],
            TypeParameterDocs = [new(TResultName, "The type of the value that the delegate produces.")],
            Delegate = new("map", [], TResultName, "Produces the value of the new result."),
            ReturnType = ResultOfTResult,
            ReturnDescription = "A successful result that holds the value produced by the delegate, or a failed result.",
            OnReceiverFailed = _ => ReceiverFailedFailure,
            FailureFormat = FailureOfResultOfTResult,
            Act = ctx => [$"var value = {ctx.Call("map", string.Empty)};", "return Result<TResult>.Succeeded(value);"],
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Returns("map", [], "7", "int"),
            ThrowingTest = Returns("map", [], "Boom.Throw<int>()", "int"),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(7, outcome.GetValue());", "Assert.Equal(1, mapCalls);"],
            OtherAsserts = [AssertSameErrorOutcomeGetError, "Assert.Equal(0, mapCalls);"],
        },
        new()
        {
            Name = BindTryName,
            StemPart = "BindTryToResultOfT",
            Typed = false,
            Summary = "Chains an operation that returns a value after an operation that returned none, and may throw.",
            TypeParameters = [TResultName],
            TypeParameterDocs = [new(TResultName, "The type of the value of the result that the delegate returns.")],
            Delegate = new("bind", [], ResultOfTResult, "The operation to run."),
            ReturnType = ResultOfTResult,
            ReturnDescription = ReturnDelegateOrFailure,
            OnReceiverFailed = _ => ReceiverFailedFailure,
            FailureFormat = FailureOfResultOfTResult,
            Act = ctx => [$"return {ctx.Call("bind", string.Empty)};"],
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Returns("bind", [], "Result.Success(7)", ResultOfInt),
            ThrowingTest = Returns("bind", [], "Boom.Throw<Result<int>>()", ResultOfInt),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(7, outcome.GetValue());", AssertEqual1BindCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls],
        },
        new()
        {
            Name = BindTryName,
            StemPart = "BindTryToResult",
            Typed = false,
            Summary = "Chains an operation that returns no value after another one, and may throw.",
            TypeParameters = [],
            TypeParameterDocs = [],
            Delegate = new("bind", [], ResultType, "The operation to run."),
            ReturnType = ResultType,
            ReturnDescription = ReturnDelegateOrFailure,
            OnReceiverFailed = ctx => ctx.R,
            FailureFormat = FailureOfResult,
            Act = ctx => [$"return {ctx.Call("bind", string.Empty)};"],
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Returns("bind", [], "Result.Failure(other)", ResultType),
            ThrowingTest = Returns("bind", [], "Boom.Throw<Result>()", ResultType),
            Setup = [OtherErrorSetup],
            RunAsserts = [AssertSameOtherOutcomeGetError, AssertEqual1BindCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls],
        },
        new()
        {
            Name = TapTryName,
            Typed = false,
            KeepsValue = true,
            Summary = "Runs a side effect that may throw for a successful result, and returns the result unchanged when it does not.",
            TypeParameters = [],
            TypeParameterDocs = [],
            Delegate = new("tap", [], "void", "The side effect to run."),
            ReturnType = ResultType,
            ReturnDescription = ReturnReceiverOrFailure,
            OnReceiverFailed = ctx => ctx.R,
            FailureFormat = FailureOfResult,
            Act = ctx => [ctx.CallStatement("tap", string.Empty), $"return {ctx.R};"],
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Does("tap", []),
            ThrowingTest = Does("tap", [], BoomThrowStatement),
            RunAsserts = [AssertTrueOutcomeIsSuccess, AssertTapCalledOnce],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoTapCalls],
        },
        new()
        {
            Name = TapIfTryName,
            Typed = false,
            Conditional = true,
            KeepsValue = true,
            Summary = "Runs a side effect that may throw for a successful result when a condition holds, and returns the result unchanged when it " +
                "does not throw.",
            TypeParameters = [],
            TypeParameterDocs = [],
            Delegate = new("tap", [], "void", "The side effect to run."),
            ReturnType = ResultType,
            ReturnDescription = ReturnReceiverOrFailure,
            OnReceiverFailed = ctx => ctx.R,
            FailureFormat = FailureOfResult,
            Act = ctx => [ctx.CallStatement("tap", string.Empty), $"return {ctx.R};"],
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Does("tap", []),
            ThrowingTest = Does("tap", [], BoomThrowStatement),
            RunAsserts = [AssertTrueOutcomeIsSuccess, AssertTapCalledOnce],
            SkipAsserts = [AssertNoTapCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoTapCalls],
        },
    ];

    /// <summary>Gets the operations of the <c>*Try</c> family: every shape with a handler for any exception and with one for a named exception.</summary>
    private static IEnumerable<OperationSpec> TryFamily() => CreateTryShapes().SelectMany(TrySpecs);

    private static IEnumerable<OperationSpec> TrySpecs(TryShape shape)
    {
        foreach (var named in new[] { false, true })
        {
            if (!shape.Conditional)
            {
                yield return TrySpec(shape, named, null);

                continue;
            }

            yield return TrySpec(shape, named, IfCondition.Bool);
            yield return TrySpec(shape, named, IfCondition.Function);

            if (shape.Typed)
            {
                yield return TrySpec(shape, named, IfCondition.OfValue);
            }
        }
    }

    private static OperationSpec TrySpec(TryShape shape, bool named, IfCondition? condition)
    {
        var stemKind = condition switch
        {
            null => string.Empty,
            IfCondition.Bool => "Bool",
            IfCondition.Function => "Func",
            _ => "ValueFunc",
        };
        var stem = $"{shape.StemPart ?? shape.Name}{(named ? "OfException" : string.Empty)}{stemKind}From{(shape.Typed ? "ResultOfT" : ResultType)}";
        var typeParameters = named ? [.. shape.TypeParameters, TExceptionName] : shape.TypeParameters;
        var typeParameterDocs = named
            ? [.. shape.TypeParameterDocs, new TypeParameterDoc(TExceptionName, ExceptionTypeDoc)]
            : shape.TypeParameterDocs;

        return new OperationSpec
        {
            Stem = stem,
            Name = shape.Name,
            KeepsValue = shape.KeepsValue,
            ReceiverType = shape.Typed ? ResultOfT : ResultType,
            ReceiverTestType = shape.Typed ? ResultOfInt : ResultType,
            ReceiverDescription = shape.Typed ? ResultOfTReceiverDescription : ResultReceiverDescription,
            TypeParameters = typeParameters,
            TypeParameterDocs = typeParameterDocs,
            Constraints = named ? [$"where {TExceptionName} : Exception"] : [],
            Summary = shape.Summary + ReceiverPassesThrough + (named ? NamedExceptionSentence : DefaultExceptionSentence),
            LeadingParameters = condition is { } kind ? [TryCondition(shape, kind)] : [],
            Delegates = [shape.Delegate],
            ExtraParameters = named ? NamedHandlerParameters() : DefaultHandlerParameters(),
            ReturnType = shape.ReturnType,
            ReturnDescription = shape.ReturnDescription,
            ForwardsTo = condition == IfCondition.Bool ? TapTryName : null,
            Body = ctx => TryBody(shape, named, condition, ctx),
            Tests = TryTests(shape, named, condition),
        };
    }

    private static ExtraParameter TryCondition(TryShape shape, IfCondition condition) => condition switch
    {
        IfCondition.Bool => new("bool", ConditionName, BoolConditionDoc, Validated: false),
        IfCondition.Function => new("Func<bool>", ConditionName, "Decides whether the delegate runs. It is called only when the receiver succeeded."),
        _ => new(
            shape.Typed ? "Func<T, bool>" : "Func<bool>",
            ConditionName,
            "Decides whether the delegate runs. It is called only when the receiver succeeded, with its value."),
    };

    private static ExtraParameter[] DefaultHandlerParameters() =>
    [
        new(
            "Func<Exception, Error>?",
            OnExceptionName,
            "Creates the error from the exception. When omitted, <see cref=\"Error.FromException(Exception, string?)\"/> is used.",
            Validated: false,
            DefaultValue: "null"),
    ];

    private static ExtraParameter[] NamedHandlerParameters() =>
    [
        new("Func<TException, Error>", OnExceptionName, "Creates the error from the exception."),
        new(
            "Func<TException, bool>?",
            WhenName,
            "A condition that the exception must also meet to be caught; an exception that does not meet it keeps travelling.",
            Validated: false,
            DefaultValue: "null"),
    ];

    // Reads the state of the receiver once, returns it as it was when the delegate must not run, and otherwise runs the delegate inside a try.
    private static string[] TryBody(TryShape shape, bool named, IfCondition? condition, BodyContext ctx)
    {
        List<string> lines =
        [
            shape.Typed ? Unpack(ctx) : Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {shape.OnReceiverFailed(ctx)};",
            "}",
            string.Empty,
        ];

        if (condition is { } kind)
        {
            var holds = kind == IfCondition.Function ? $"{ConditionName}()" : $"{ConditionName}({InputName})";
            lines.AddRange([$"if (!{holds})", "{", $"    return {ctx.R};", "}", string.Empty]);
        }

        var error = named ? "onException(exception)" : "Result.ErrorOf(exception, onException)";

        lines.AddRange(["try", "{"]);
        lines.AddRange(shape.Act(ctx).Select(line => line.Length == 0 ? line : "    " + line));
        lines.Add("}");
        lines.Add(named
            ? "catch (TException exception) when (Result.Matches(when, exception))"
            : "catch (Exception exception) when (ResultConfiguration.Current.ShouldCatch(exception))");
        lines.AddRange(["{", $"    return {string.Format(System.Globalization.CultureInfo.InvariantCulture, shape.FailureFormat, error)};", "}"]);

        return [.. lines];
    }

    private static TestCaseSpec[] TryTests(TryShape shape, bool named, IfCondition? condition) => new TryTestBuilder(shape, named, condition).Build();

    private static string TryLeadingArgument(IfCondition condition, bool holds) => condition switch
    {
        IfCondition.Bool => holds ? "true" : "false",
        IfCondition.Function => $"() => {{ conditionCalls++; return {(holds ? "true" : "false")}; }}",
        _ => $"x => {{ conditionCalls++; return x == {(holds ? "2" : "3")}; }}",
    };

    /// <summary>One test of a <c>*Try</c> operation: what it runs, with which handler and condition, and what it expects.</summary>
    /// <param name="Name">The name of the test.</param>
    /// <param name="Receiver">The receiver to call the operation on.</param>
    /// <param name="Test">How the delegate behaves.</param>
    /// <param name="Asserts">What the test asserts.</param>
    /// <param name="Handler">The handler to pass, or <see langword="null"/> to leave it out.</param>
    /// <param name="Leading">The condition to pass, or <see langword="null"/> when the operation has none.</param>
    /// <param name="ConditionCalls">How often the test expects the condition to have been evaluated.</param>
    private sealed record TryCase(string Name, string Receiver, DelegateTest Test, string[] Asserts, string? Handler, string? Leading, int ConditionCalls);

    /// <summary>Writes the generated test cases of one <c>*Try</c> operation.</summary>
    private sealed class TryTestBuilder(TryShape shape, bool named, IfCondition? condition)
    {
        private readonly bool _counted = condition is IfCondition.Function or IfCondition.OfValue;
        private readonly string? _yes = condition is { } holds ? TryLeadingArgument(holds, holds: true) : null;
        private readonly string? _no = condition is { } fails ? TryLeadingArgument(fails, holds: false) : null;
        private readonly string? _handler = named ? NamedHandler : null;

        internal TestCaseSpec[] Build()
        {
            var cases = new List<TryCase>
            {
                new("returns_the_outcome_of_the_delegate_when_the_receiver_succeeded", shape.GateReceiver, shape.Test, shape.RunAsserts, _handler, _yes, 1),
            };

            if (shape.Conditional)
            {
                cases.Add(new(
                    "does_not_run_the_delegate_when_the_condition_does_not_hold",
                    shape.GateReceiver,
                    shape.Test,
                    [AssertTrueOutcomeIsSuccess, .. shape.SkipAsserts],
                    _handler,
                    _no,
                    1));
            }

            cases.Add(new(
                "returns_the_error_without_running_the_delegate_when_the_receiver_failed",
                shape.OtherReceiver,
                shape.Test,
                shape.OtherAsserts,
                _handler,
                _yes,
                0));
            cases.AddRange(ThrowingCases());

            return [.. cases.Select(Make)];
        }

        private IEnumerable<TryCase> ThrowingCases()
        {
            var calls = $"Assert.Equal(1, {shape.ThrowingTest.DelegateName}Calls);";

            if (named)
            {
                yield return ThrowingCase(
                    "returns_the_error_of_the_handler_when_the_delegate_throws_the_named_exception",
                    [AssertSameHandledOutcomeGetError, calls],
                    _handler);

                yield break;
            }

            string[] cause = ["Assert.True(outcome.IsFailure);", "Assert.IsType<InvalidOperationException>(outcome.GetError().Exception);", calls];
            yield return ThrowingCase("returns_a_failure_that_keeps_the_exception_as_the_cause_when_the_delegate_throws", cause, null);
            yield return ThrowingCase("returns_the_error_of_the_handler_when_the_delegate_throws", [AssertSameHandledOutcomeGetError], "_ => handled");
        }

        private TryCase ThrowingCase(string name, string[] asserts, string? handler) =>
            new(name, shape.GateReceiver, shape.ThrowingTest, asserts, handler, _yes, 1);

        // Only a test whose delegate is the plain one uses the arrangement of the shape; only one with a handler that refers to it needs `handled`.
        private TestCaseSpec Make(TryCase test)
        {
            var usesTest = test.Test == shape.Test;
            var setup = new List<string>();

            if (_counted)
            {
                setup.Add("var conditionCalls = 0;");
            }

            if (usesTest)
            {
                setup.AddRange(shape.Setup ?? []);
            }

            if (test.Handler?.Contains("handled", StringComparison.Ordinal) == true)
            {
                setup.Add(HandledSetup);
            }

            var asserts = _counted ? [.. test.Asserts, $"Assert.Equal({test.ConditionCalls}, conditionCalls);"] : test.Asserts;
            string[] handler = test.Handler is null ? [] : [test.Handler];
            string[]? leading = test.Leading is null ? null : [test.Leading];

            return new(test.Name, test.Receiver, [test.Test], handler, asserts, [.. setup], leading);
        }
    }

    /// <summary>What distinguishes one <c>*Try</c> operation from another; the rest of the spec is derived from it.</summary>
    private sealed record TryShape
    {
        /// <summary>Gets the name of the extension method.</summary>
        internal required string Name { get; init; }

        /// <summary>Gets the part of the file names when it is not the method name, to tell shapes with the same method name apart.</summary>
        internal string? StemPart { get; init; }

        /// <summary>Gets a value indicating whether the receiver is a typed result.</summary>
        internal required bool Typed { get; init; }

        /// <summary>Gets a value indicating whether the operation takes a condition, in three forms.</summary>
        internal bool Conditional { get; init; }

        /// <summary>Gets a value indicating whether the operation keeps the value of its receiver.</summary>
        internal bool KeepsValue { get; init; }

        /// <summary>Gets the summary of the operation, before the sentence about the exception.</summary>
        internal required string Summary { get; init; }

        /// <summary>Gets the type parameters.</summary>
        internal required string[] TypeParameters { get; init; }

        /// <summary>Gets the documentation of the type parameters.</summary>
        internal required TypeParameterDoc[] TypeParameterDocs { get; init; }

        /// <summary>Gets the delegate that may throw.</summary>
        internal required DelegateSpec Delegate { get; init; }

        /// <summary>Gets the synchronous return type.</summary>
        internal required string ReturnType { get; init; }

        /// <summary>Gets the documentation of the return value.</summary>
        internal required string ReturnDescription { get; init; }

        /// <summary>Gets the expression that returns the failure of a failed receiver.</summary>
        internal required Func<BodyContext, string> OnReceiverFailed { get; init; }

        /// <summary>Gets the format of the expression that creates a failed result, with the error as its argument.</summary>
        internal required string FailureFormat { get; init; }

        /// <summary>Gets the lines that run the delegate and return the outcome, which go inside the <c>try</c>.</summary>
        internal required Func<BodyContext, string[]> Act { get; init; }

        /// <summary>Gets the receiver for which the delegate runs.</summary>
        internal required string GateReceiver { get; init; }

        /// <summary>Gets the receiver for which the delegate never runs.</summary>
        internal required string OtherReceiver { get; init; }

        /// <summary>Gets how the delegate behaves in the tests that do not throw.</summary>
        internal required DelegateTest Test { get; init; }

        /// <summary>Gets how the delegate behaves in the tests that throw.</summary>
        internal required DelegateTest ThrowingTest { get; init; }

        /// <summary>Gets the lines that arrange the tests that use <see cref="Test"/>.</summary>
        internal string[]? Setup { get; init; }

        /// <summary>Gets what the tests assert when the delegate ran.</summary>
        internal required string[] RunAsserts { get; init; }

        /// <summary>Gets what the tests assert when the condition did not hold.</summary>
        internal string[] SkipAsserts { get; init; } = [];

        /// <summary>Gets what the tests assert for the receiver for which the delegate never runs.</summary>
        internal required string[] OtherAsserts { get; init; }
    }
}
