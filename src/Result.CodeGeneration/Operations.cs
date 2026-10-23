using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <summary>
/// The catalog of operations. Adding an operation here adds it, in all nine variants and with tests, to the library.
/// </summary>
/// <remarks>
/// The bodies read the state of the receiver once (<c>Unpack</c> for a typed result, <c>Failure()</c> for an untyped one) and
/// build results with the internal constructors, so that nothing is checked or validated twice on the hot path.
/// </remarks>
internal static partial class Operations
{
    private const string TDoc = "The type of the value of the receiver result.";
    private const string ReceiverFailed = "returns_the_error_without_running_the_delegate_when_the_receiver_failed";
    private const string ReceiverSucceeded = "returns_the_receiver_without_running_the_delegate_when_the_receiver_succeeded";
    private const string FailureOfInt = "Result.Failure<int>(error)";
    private const string AssertSameErrorOutcomeGetError = "Assert.Same(error, outcome.GetError());";
    private const string AssertSameOtherOutcomeGetError = "Assert.Same(other, outcome.GetError());";
    private const string VarLogNewListString = "var log = new List<string>();";
    private const string ResultType = "Result";
    private const string ResultReceiverDescription = "a <see cref=\"Result\"/>";
    private const string SuccessWithoutValue = "Result.Success()";
    private const string FailureWithError = "Result.Failure(error)";
    private const string ReturnsTheResultOfTheDelegateWhenTheReceiver = "returns_the_result_of_the_delegate_when_the_receiver_succeeded";
    private const string AssertEqual1BindCalls = "Assert.Equal(1, bindCalls);";
    private const string ReturnsTheErrorOfTheDelegateWhenTheDelegate = "returns_the_error_of_the_delegate_when_the_delegate_failed";
    private const string AssertEqual0BindCalls = "Assert.Equal(0, bindCalls);";
    private const string SuccessOfFive = "Result.Success(5)";
    private const string ReceiverUnchangedDescription = "The receiver, unchanged.";
    private const string IfFailureIsNull = "if (failure is null)";
    private const string AssertEqual2OutcomeGetValue = "Assert.Equal(2, outcome.GetValue());";
    private const string TapErrorName = "tapError";
    private const string ErrorType = "Error";
    private const string FailureName = "failure";
    private const string MapErrorName = "mapError";
    private const string EWithContextMeny = """e.WithContext("Meny")""";
    private const string PredicateName = "predicate";
    private const string ErrorName = "error";
    private const string GreaterThanOne = "x > 1";
    private const string AssertEqual1PredicateCalls = "Assert.Equal(1, predicateCalls);";
    private const string StringType = "string";
    private const string ResultOfT = "Result<T>";
    private const string ResultOfInt = "Result<int>";
    private const string ResultOfTReceiverDescription = "a <see cref=\"Result{T}\"/>";
    private const string TResultName = "TResult";
    private const string ValidationName = "validation";
    private const string ConditionName = "condition";
    private const string OperationDoc = "The validation to run when the receiver succeeded. It gets no value.";
    private const string TypedOperationDoc = "The validation to run when the receiver succeeded. It gets no value, and the value of its result is ignored.";
    private const string SuccessOfChecked = "Result.Success(\"checked\")";
    private const string FailureOfChecked = "Result.Failure<string>(error)";
    private const string ResultOfString = "Result<string>";
    private const string AssertEqual1ConditionCalls = "Assert.Equal(1, conditionCalls);";
    private const string AssertEqual0ConditionCalls = "Assert.Equal(0, conditionCalls);";
    private const string TooSmallMessage = "\"Too small.\"";
    private const string OtherErrorSetup = """var other = Error.Create("Other.");""";
    private const string EnsureOperationName = "Ensure";
    private const string AssertEqual1ValidationCalls = "Assert.Equal(1, validationCalls);";
    private const string ResultOfTResult = "Result<TResult>";
    private const string RecoverName = "recover";
    private const string CompensateName = "compensate";
    private const string IfFailureIsNotNull = "if (failure is not null)";
    private const string ReturnResultTResultFailedFailure = "    return Result<TResult>.Failed(failure);";
    private const string InputName = "input";
    private const string OnSuccessName = "onSuccess";
    private const string OnFailureName = "onFailure";
    private const string SuccessOfTwo = "Result.Success(2)";
    private const string RunsOnlyTheSuccessDelegateWhenTheReceiverSucceeded = "runs_only_the_success_delegate_when_the_receiver_succeeded";
    private const string FailEMessage = "\"fail:\" + e.Message";
    private const string RunsOnlyTheFailureDelegateWhenTheReceiverFailed = "runs_only_the_failure_delegate_when_the_receiver_failed";
    private const string AssertTrueOutcomeIsSuccess = "Assert.True(outcome.IsSuccess);";
    private const string ErrorFactoryName = "errorFactory";
    private const string MessageFactoryName = "messageFactory";

    /// <summary>Gets every operation family.</summary>
    internal static IReadOnlyList<OperationSpec> All { get; } =
    [
        MapFromResultOfT(),
        MapFromResult(),
        BindResultOfTToResultOfT(),
        BindResultOfTToResult(),
        BindResultToResultOfT(),
        BindResultToResult(),
        TapFromResultOfT(),
        TapFromResult(),
        TapErrorFromResultOfT(),
        TapErrorFromResult(),
        MapErrorFromResultOfT(),
        MapErrorFromResult(),
        EnsureFromResultOfT(),
        EnsureWithMessageFromResultOfT(),
        EnsureWithFactoryFromResultOfT(message: false),
        EnsureWithFactoryFromResultOfT(message: true),
        EnsureWithResultFromResultOfT(),
        EnsureWithTypedResultFromResultOfT(),
        EnsureConditionFromResult(withMessage: false),
        EnsureConditionFromResult(withMessage: true),
        EnsureOperationFromResultOfT(typed: false),
        EnsureOperationFromResultOfT(typed: true),
        RecoverFromResultOfT(),
        CompensateFromResultOfT(),
        CompensateFromResult(),
        MatchFromResultOfT(),
        MatchFromResult(),
        SwitchFromResultOfT(),
        SwitchFromResult(),
        .. IfFamily(),
        .. TryFamily(),
        .. Conveniences(),
    ];

    private static DelegateTest Returns(string name, string[] parameters, string expression, string returnType, string statements = "") =>
        new(name, parameters, statements, expression, returnType);

    private static DelegateTest Does(string name, string[] parameters, string statements = "") =>
        new(name, parameters, statements, null, string.Empty);

    private static TestCaseSpec Case(
        string name,
        string receiver,
        DelegateTest[] delegates,
        string[] asserts,
        string[]? extraArguments = null,
        string[]? setup = null) =>
        new(name, receiver, delegates, extraArguments ?? [], asserts, setup);

    // Reads the state of a typed receiver once: `failure` is null for a success and `input` then holds the value.
    private static string Unpack(BodyContext ctx, string value = "var input") => $"var failure = {ctx.R}.Unpack(out {value});";

    // Reads the state of an untyped receiver once: `failure` is null for a success.
    private static string Failure(BodyContext ctx) => $"var failure = {ctx.R}.FailureOrNull();";

    private static OperationSpec MapFromResultOfT() => new()
    {
        Stem = "MapFromResultOfT",
        Name = "Map",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value that the delegate produces.")],
        Summary = "Transforms the value of a successful result. A failed result passes through unchanged: its error is kept and the delegate is not called.",
        Delegates = [new("map", ["T"], TResultName, "Computes the new value from the value of the successful result.")],
        ReturnType = ResultOfTResult,
        ReturnDescription = "A successful result that holds the value computed by the delegate, or a failed result with the error of the receiver.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            ReturnResultTResultFailedFailure,
            "}",
            string.Empty,
            $"var value = {ctx.Call("map", InputName)};",
            "return Result<TResult>.Succeeded(value);",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_and_wraps_its_value_when_the_receiver_succeeded",
                SuccessOfTwo,
                [Returns("map", ["x"], "x * 10", "int")],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(20, outcome.GetValue());", "Assert.Equal(1, mapCalls);"]),
            Case(
                ReceiverFailed,
                FailureOfInt,
                [Returns("map", ["x"], "x * 10", "int")],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, mapCalls);"]),
        ],
    };

    private static OperationSpec MapFromResult() => new()
    {
        Stem = "MapFromResult",
        Name = "Map",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [TResultName],
        TypeParameterDocs = [new(TResultName, "The type of the value that the delegate produces.")],
        Summary = "Turns a successful result without a value into a result with a value. A failed result passes through unchanged: its error is " +
            "kept and the delegate is not called.",
        Delegates = [new("map", [], TResultName, "Produces the value of the new result.")],
        ReturnType = ResultOfTResult,
        ReturnDescription = "A successful result that holds the value produced by the delegate, or a failed result with the error of the receiver.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            ReturnResultTResultFailedFailure,
            "}",
            string.Empty,
            $"var value = {ctx.Call("map", string.Empty)};",
            "return Result<TResult>.Succeeded(value);",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_and_wraps_its_value_when_the_receiver_succeeded",
                SuccessWithoutValue,
                [Returns("map", [], "7", "int")],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(7, outcome.GetValue());", "Assert.Equal(1, mapCalls);"]),
            Case(
                ReceiverFailed,
                FailureWithError,
                [Returns("map", [], "7", "int")],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, mapCalls);"]),
        ],
    };

    private static OperationSpec BindResultOfTToResultOfT() => new()
    {
        Stem = "BindFromResultOfTToResultOfT",
        Name = "Bind",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value of the result that the delegate returns.")],
        Summary = "Chains an operation that can fail and returns a value. The delegate runs only when the receiver succeeded; a failed result " +
            "passes through with its error.",
        Delegates = [new("bind", ["T"], ResultOfTResult, "The operation to run with the value of the successful result.")],
        ReturnType = ResultOfTResult,
        ReturnDescription = "The result of the delegate, or a failed result with the error of the receiver.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            ReturnResultTResultFailedFailure,
            "}",
            string.Empty,
            $"return {ctx.Call("bind", InputName)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.Done("Result<TResult>.Failed(failure)")};",
            "}",
            string.Empty,
            "return bind(input);",
        ],
        Tests =
        [
            Case(
                ReturnsTheResultOfTheDelegateWhenTheReceiver,
                SuccessOfTwo,
                [Returns("bind", ["x"], "Result.Success(x * 10)", ResultOfInt)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(20, outcome.GetValue());", AssertEqual1BindCalls]),
            Case(
                ReturnsTheErrorOfTheDelegateWhenTheDelegate,
                SuccessOfTwo,
                [Returns("bind", ["x"], FailureOfInt, ResultOfInt)],
                [AssertSameErrorOutcomeGetError, AssertEqual1BindCalls]),
            Case(
                ReceiverFailed,
                FailureOfInt,
                [Returns("bind", ["x"], "Result.Success(x * 10)", ResultOfInt)],
                [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls]),
        ],
    };

    private static OperationSpec BindResultOfTToResult() => new()
    {
        Stem = "BindFromResultOfTToResult",
        Name = "Bind",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Chains an operation that can fail and returns no value, for example saving the value. The delegate runs only when the receiver succeeded.",
        Delegates = [new("bind", ["T"], ResultType, "The operation to run with the value of the successful result.")],
        ReturnType = ResultType,
        ReturnDescription = "The result of the delegate, or a failed result with the error of the receiver.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            "    return new Result(failure);",
            "}",
            string.Empty,
            $"return {ctx.Call("bind", InputName)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.Done("new Result(failure)")};",
            "}",
            string.Empty,
            "return bind(input);",
        ],
        Tests =
        [
            Case(
                ReturnsTheResultOfTheDelegateWhenTheReceiver,
                SuccessOfTwo,
                [Returns("bind", ["x"], SuccessWithoutValue, ResultType)],
                [AssertTrueOutcomeIsSuccess, AssertEqual1BindCalls]),
            Case(
                ReturnsTheErrorOfTheDelegateWhenTheDelegate,
                SuccessOfTwo,
                [Returns("bind", ["x"], FailureWithError, ResultType)],
                [AssertSameErrorOutcomeGetError, AssertEqual1BindCalls]),
            Case(
                ReceiverFailed,
                FailureOfInt,
                [Returns("bind", ["x"], SuccessWithoutValue, ResultType)],
                [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls]),
        ],
    };

    private static OperationSpec BindResultToResultOfT() => new()
    {
        Stem = "BindFromResultToResultOfT",
        Name = "Bind",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [TResultName],
        TypeParameterDocs = [new(TResultName, "The type of the value of the result that the delegate returns.")],
        Summary = "Chains an operation that returns a value after an operation that returned none. The delegate runs only when the receiver succeeded.",
        Delegates = [new("bind", [], ResultOfTResult, "The operation to run.")],
        ReturnType = ResultOfTResult,
        ReturnDescription = "The result of the delegate, or a failed result with the error of the receiver.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            ReturnResultTResultFailedFailure,
            "}",
            string.Empty,
            $"return {ctx.Call("bind", string.Empty)};",
        ],
        ForwardBody = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.Done("Result<TResult>.Failed(failure)")};",
            "}",
            string.Empty,
            "return bind();",
        ],
        Tests =
        [
            Case(
                ReturnsTheResultOfTheDelegateWhenTheReceiver,
                SuccessWithoutValue,
                [Returns("bind", [], SuccessOfFive, ResultOfInt)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(5, outcome.GetValue());", AssertEqual1BindCalls]),
            Case(
                ReturnsTheErrorOfTheDelegateWhenTheDelegate,
                SuccessWithoutValue,
                [Returns("bind", [], FailureOfInt, ResultOfInt)],
                [AssertSameErrorOutcomeGetError, AssertEqual1BindCalls]),
            Case(
                ReceiverFailed,
                FailureWithError,
                [Returns("bind", [], SuccessOfFive, ResultOfInt)],
                [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls]),
        ],
    };

    private static OperationSpec BindResultToResult() => new()
    {
        Stem = "BindFromResultToResult",
        Name = "Bind",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Chains an operation that returns no value after another one. The delegate runs only when the receiver succeeded.",
        Delegates = [new("bind", [], ResultType, "The operation to run.")],
        ReturnType = ResultType,
        ReturnDescription = "The result of the delegate, or the failed receiver.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"return {ctx.Call("bind", string.Empty)};",
        ],
        ForwardBody = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.Done(ctx.R)};",
            "}",
            string.Empty,
            "return bind();",
        ],
        Tests =
        [
            Case(
                ReturnsTheResultOfTheDelegateWhenTheReceiver,
                SuccessWithoutValue,
                [Returns("bind", [], SuccessWithoutValue, ResultType)],
                [AssertTrueOutcomeIsSuccess, AssertEqual1BindCalls]),
            Case(
                ReturnsTheErrorOfTheDelegateWhenTheDelegate,
                SuccessWithoutValue,
                [Returns("bind", [], FailureWithError, ResultType)],
                [AssertSameErrorOutcomeGetError, AssertEqual1BindCalls]),
            Case(
                ReceiverFailed,
                FailureWithError,
                [Returns("bind", [], SuccessWithoutValue, ResultType)],
                [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls]),
        ],
    };

    private static OperationSpec TapFromResultOfT() => new()
    {
        Stem = "TapFromResultOfT",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "Tap",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Runs a side effect, for example logging, with the value of a successful result and returns the result unchanged. The delegate " +
            "is not called for a failed result.",
        Delegates = [new("tap", ["T"], "void", "The side effect to run with the value of the successful result.")],
        ReturnType = ResultOfT,
        ReturnDescription = ReceiverUnchangedDescription,
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    {ctx.CallStatement("tap", InputName)}",
            "}",
            string.Empty,
            $"return {ctx.R};",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_with_the_value_and_returns_the_receiver_when_the_receiver_succeeded",
                SuccessOfTwo,
                [Does("tap", ["x"], "seen = x;")],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(2, seen);", "Assert.Equal(1, tapCalls);"],
                setup: ["var seen = 0;"]),
            Case(
                "does_not_run_the_delegate_when_the_receiver_failed",
                FailureOfInt,
                [Does("tap", ["x"])],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, tapCalls);"]),
        ],
    };

    private static OperationSpec TapFromResult() => new()
    {
        Stem = "TapFromResult",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "Tap",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Runs a side effect for a successful result and returns the result unchanged. The delegate is not called for a failed result.",
        Delegates = [new("tap", [], "void", "The side effect to run.")],
        ReturnType = ResultType,
        ReturnDescription = ReceiverUnchangedDescription,
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    {ctx.CallStatement("tap", string.Empty)}",
            "}",
            string.Empty,
            $"return {ctx.R};",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_and_returns_the_receiver_when_the_receiver_succeeded",
                SuccessWithoutValue,
                [Does("tap", [])],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(1, tapCalls);"]),
            Case(
                "does_not_run_the_delegate_when_the_receiver_failed",
                FailureWithError,
                [Does("tap", [])],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, tapCalls);"]),
        ],
    };

    private static OperationSpec TapErrorFromResultOfT() => new()
    {
        Stem = "TapErrorFromResultOfT",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "TapError",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Runs a side effect, for example logging, with the error of a failed result and returns the result unchanged. The delegate is " +
            "not called for a successful result.",
        Delegates = [new(TapErrorName, [ErrorType], "void", "The side effect to run with the error of the failed result.")],
        ReturnType = ResultOfT,
        ReturnDescription = ReceiverUnchangedDescription,
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    {ctx.CallStatement(TapErrorName, FailureName)}",
            "}",
            string.Empty,
            $"return {ctx.R};",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_with_the_error_and_returns_the_receiver_when_the_receiver_failed",
                FailureOfInt,
                [Does(TapErrorName, ["e"], "seen = e;")],
                [AssertSameErrorOutcomeGetError, "Assert.Same(error, seen);", "Assert.Equal(1, tapErrorCalls);"],
                setup: ["Error? seen = null;"]),
            Case(
                "does_not_run_the_delegate_when_the_receiver_succeeded",
                SuccessOfTwo,
                [Does(TapErrorName, ["e"])],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(0, tapErrorCalls);"]),
        ],
    };

    private static OperationSpec TapErrorFromResult() => new()
    {
        Stem = "TapErrorFromResult",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "TapError",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Runs a side effect with the error of a failed result and returns the result unchanged. The delegate is not called for a successful result.",
        Delegates = [new(TapErrorName, [ErrorType], "void", "The side effect to run with the error of the failed result.")],
        ReturnType = ResultType,
        ReturnDescription = ReceiverUnchangedDescription,
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    {ctx.CallStatement(TapErrorName, FailureName)}",
            "}",
            string.Empty,
            $"return {ctx.R};",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_with_the_error_and_returns_the_receiver_when_the_receiver_failed",
                FailureWithError,
                [Does(TapErrorName, ["e"], "seen = e;")],
                [AssertSameErrorOutcomeGetError, "Assert.Same(error, seen);", "Assert.Equal(1, tapErrorCalls);"],
                setup: ["Error? seen = null;"]),
            Case(
                "does_not_run_the_delegate_when_the_receiver_succeeded",
                SuccessWithoutValue,
                [Does(TapErrorName, ["e"])],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(0, tapErrorCalls);"]),
        ],
    };

    private static OperationSpec MapErrorFromResultOfT() => new()
    {
        Stem = "MapErrorFromResultOfT",
        KeepsValue = true,
        Name = "MapError",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Replaces the error of a failed result, for example to add context. A successful result passes through unchanged and the " +
            "delegate is not called.",
        Delegates = [new(MapErrorName, [ErrorType], ErrorType, "Computes the new error from the error of the failed result.")],
        ReturnType = ResultOfT,
        ReturnDescription = "A failed result with the error computed by the delegate, or the successful receiver.",
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var mapped = {ctx.Call(MapErrorName, FailureName)};",
            "return Result.Failure<T>(mapped);",
        ],
        Tests =
        [
            Case(
                "returns_the_error_of_the_delegate_when_the_receiver_failed",
                FailureOfInt,
                [Returns(MapErrorName, ["e"], EWithContextMeny, ErrorType)],
                ["""Assert.Equal(["Meny"], outcome.GetError().Context);""", "Assert.Equal(1, mapErrorCalls);"]),
            Case(
                ReceiverSucceeded,
                SuccessOfTwo,
                [Returns(MapErrorName, ["e"], EWithContextMeny, ErrorType)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(0, mapErrorCalls);"]),
        ],
    };

    private static OperationSpec MapErrorFromResult() => new()
    {
        Stem = "MapErrorFromResult",
        KeepsValue = true,
        Name = "MapError",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Replaces the error of a failed result, for example to add context. A successful result passes through unchanged and the " +
            "delegate is not called.",
        Delegates = [new(MapErrorName, [ErrorType], ErrorType, "Computes the new error from the error of the failed result.")],
        ReturnType = ResultType,
        ReturnDescription = "A failed result with the error computed by the delegate, or the successful receiver.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var mapped = {ctx.Call(MapErrorName, FailureName)};",
            "return Result.Failure(mapped);",
        ],
        Tests =
        [
            Case(
                "returns_the_error_of_the_delegate_when_the_receiver_failed",
                FailureWithError,
                [Returns(MapErrorName, ["e"], EWithContextMeny, ErrorType)],
                ["""Assert.Equal(["Meny"], outcome.GetError().Context);""", "Assert.Equal(1, mapErrorCalls);"]),
            Case(
                ReceiverSucceeded,
                SuccessWithoutValue,
                [Returns(MapErrorName, ["e"], EWithContextMeny, ErrorType)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(0, mapErrorCalls);"]),
        ],
    };

    private static OperationSpec EnsureFromResultOfT() => new()
    {
        Stem = "EnsureFromResultOfT",
        KeepsValue = true,
        Name = EnsureOperationName,
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Turns a successful result into a failure when its value does not satisfy a condition. A failed result passes through with its " +
            "own error and the predicate is not called.",
        Delegates = [new(PredicateName, ["T"], "bool", "The condition the value must satisfy.")],
        ExtraParameters = [new(ErrorType, ErrorName, "The error of the failed result that is returned when the condition does not hold.")],
        ReturnType = ResultOfT,
        ReturnDescription = "The receiver when it failed or its value satisfies the condition, otherwise a failed result with <paramref name=\"error\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var holds = {ctx.Call(PredicateName, InputName)};",
            $"return holds ? {ctx.R} : Result<T>.Failed(error);",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_value_satisfies_the_condition",
                SuccessOfTwo,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1PredicateCalls],
                extraArguments: [ErrorName]),
            Case(
                "returns_the_given_error_when_the_value_does_not_satisfy_the_condition",
                SuccessOfTwo,
                [Returns(PredicateName, ["x"], "x > 5", "bool")],
                [AssertSameErrorOutcomeGetError, AssertEqual1PredicateCalls],
                extraArguments: [ErrorName]),
            Case(
                "keeps_the_error_of_the_receiver_without_running_the_predicate_when_the_receiver_failed",
                "Result.Failure<int>(other)",
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertSameOtherOutcomeGetError, "Assert.Equal(0, predicateCalls);"],
                extraArguments: [ErrorName],
                setup: ["""var other = Error.Create("Other.");"""]),
        ],
    };

    private static OperationSpec EnsureWithMessageFromResultOfT() => new()
    {
        Stem = "EnsureWithMessageFromResultOfT",
        KeepsValue = true,
        Name = EnsureOperationName,
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Turns a successful result into a failure with the given message when its value does not satisfy a condition. A failed result " +
            "passes through with its own error and the predicate is not called.",
        Delegates = [new(PredicateName, ["T"], "bool", "The condition the value must satisfy.")],
        ExtraParameters = [new(StringType, "message", "The message of the error of the failed result that is returned when the condition does not hold.")],
        ReturnType = ResultOfT,
        ReturnDescription = "The receiver when it failed or its value satisfies the condition, otherwise a failed result whose error was created " +
            "from <paramref name=\"message\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var holds = {ctx.Call(PredicateName, InputName)};",
            $"return holds ? {ctx.R} : Result.Failure<T>(message);",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_value_satisfies_the_condition",
                SuccessOfTwo,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1PredicateCalls],
                extraArguments: [TooSmallMessage]),
            Case(
                "returns_an_error_with_the_given_message_when_the_value_does_not_satisfy_the_condition",
                SuccessOfTwo,
                [Returns(PredicateName, ["x"], "x > 5", "bool")],
                ["Assert.Equal(\"Too small.\", outcome.GetError().Message);", AssertEqual1PredicateCalls],
                extraArguments: [TooSmallMessage]),
            Case(
                "keeps_the_error_of_the_receiver_without_running_the_predicate_when_the_receiver_failed",
                "Result.Failure<int>(other)",
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertSameOtherOutcomeGetError, "Assert.Equal(0, predicateCalls);"],
                extraArguments: [TooSmallMessage],
                setup: ["""var other = Error.Create("Other.");"""]),
        ],
    };

    // The condition and the factory that builds the error are asynchronous independently of each other, so this family has 27 variants.
    private static OperationSpec EnsureWithFactoryFromResultOfT(bool message)
    {
        var factoryName = message ? MessageFactoryName : ErrorFactoryName;
        var factoryReturns = message ? StringType : ErrorType;
        var factoryResult = message ? TooSmallMessage : ErrorName;
        var factoryCalls = $"{factoryName}Calls";
        var factoryDoc = message
            ? "Creates the message of the error from the value. It is called only when the condition does not hold."
            : "Creates the error from the value. It is called only when the condition does not hold.";

        return new()
        {
            Stem = message ? "EnsureWithMessageFactoryFromResultOfT" : "EnsureWithErrorFactoryFromResultOfT",
            KeepsValue = true,
            Name = EnsureOperationName,
            ReceiverType = ResultOfT,
            ReceiverTestType = ResultOfInt,
            ReceiverDescription = ResultOfTReceiverDescription,
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", TDoc)],
            Summary = message
                ? "Turns a successful result into a failure when its value does not satisfy a condition. The message of the error is created from the " +
                    "value, and only when the condition does not hold. A failed result passes through with its own error and neither delegate is called."
                : "Turns a successful result into a failure when its value does not satisfy a condition. The error is created from the value, and only " +
                    "when the condition does not hold. A failed result passes through with its own error and neither delegate is called.",
            Delegates =
            [
                new(PredicateName, ["T"], "bool", "The condition the value must satisfy."),
                new(factoryName, ["T"], factoryReturns, factoryDoc, Independent: true),
            ],
            ReturnType = ResultOfT,
            ReturnDescription = message
                ? "The receiver when it failed or its value satisfies the condition, otherwise a failed result whose error was created from the " +
                    $"message of <paramref name=\"{factoryName}\"/>."
                : "The receiver when it failed or its value satisfies the condition, otherwise a failed result with the error of " +
                    $"<paramref name=\"{factoryName}\"/>.",
            Body = ctx =>
            [
                Unpack(ctx),
                string.Empty,
                IfFailureIsNotNull,
                "{",
                $"    return {ctx.R};",
                "}",
                string.Empty,
                $"var holds = {ctx.Call(PredicateName, InputName)};",
                string.Empty,
                "if (holds)",
                "{",
                $"    return {ctx.R};",
                "}",
                string.Empty,
                $"var {(message ? "message" : ErrorName)} = {ctx.CallSecond(factoryName, InputName)};",
                $"return Result.Failure<T>({(message ? "message" : ErrorName)});",
            ],
            Tests =
            [
                Case(
                    "returns_the_receiver_without_creating_an_error_when_the_value_satisfies_the_condition",
                    SuccessOfTwo,
                    [Returns(PredicateName, ["x"], GreaterThanOne, "bool"), Returns(factoryName, ["x"], factoryResult, factoryReturns)],
                    [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1PredicateCalls, $"Assert.Equal(0, {factoryCalls});"]),
                Case(
                    "returns_the_failure_the_factory_creates_from_the_value_when_the_value_does_not_satisfy_the_condition",
                    SuccessOfTwo,
                    [Returns(PredicateName, ["x"], "x > 5", "bool"), Returns(factoryName, ["x"], factoryResult, factoryReturns, "seen = x;")],
                    [
                        message ? "Assert.Equal(\"Too small.\", outcome.GetError().Message);" : AssertSameErrorOutcomeGetError,
                        AssertEqual1PredicateCalls,
                        $"Assert.Equal(1, {factoryCalls});",
                        "Assert.Equal(2, seen);",
                    ],
                    setup: ["var seen = 0;"]),
                Case(
                    "keeps_the_error_of_the_receiver_without_running_a_delegate_when_the_receiver_failed",
                    "Result.Failure<int>(other)",
                    [Returns(PredicateName, ["x"], GreaterThanOne, "bool"), Returns(factoryName, ["x"], factoryResult, factoryReturns)],
                    [AssertSameOtherOutcomeGetError, "Assert.Equal(0, predicateCalls);", $"Assert.Equal(0, {factoryCalls});"],
                    setup: [OtherErrorSetup]),
            ],
        };
    }

    private static OperationSpec EnsureWithResultFromResultOfT() => new()
    {
        Stem = "EnsureWithResultFromResultOfT",
        KeepsValue = true,
        Name = EnsureOperationName,
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Validates the value of a successful result with an operation that returns a result, and keeps the original value when " +
            "that operation succeeds. A failed result passes through and the delegate is not called.",
        Delegates = [new(ValidationName, ["T"], ResultType, "The validation to run with the value of the successful result.")],
        ReturnType = ResultOfT,
        ReturnDescription = "The receiver when it failed or the validation succeeded, otherwise a failed result with the error of the validation.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var validationResult = {ctx.Call(ValidationName, InputName)};",
            "var validationFailure = validationResult.FailureOrNull();",
            $"return validationFailure is null ? {ctx.R} : Result<T>.Failed(validationFailure);",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_validation_succeeded",
                SuccessOfTwo,
                [Returns(ValidationName, ["x"], SuccessWithoutValue, ResultType)],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1ValidationCalls]),
            Case(
                "returns_the_error_of_the_validation_when_the_validation_failed",
                SuccessOfTwo,
                [Returns(ValidationName, ["x"], FailureWithError, ResultType)],
                [AssertSameErrorOutcomeGetError, AssertEqual1ValidationCalls]),
            Case(
                ReceiverFailed,
                FailureOfInt,
                [Returns(ValidationName, ["x"], SuccessWithoutValue, ResultType)],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, validationCalls);"]),
        ],
    };

    private static OperationSpec EnsureWithTypedResultFromResultOfT() => new()
    {
        Stem = "EnsureWithTypedResultFromResultOfT",
        KeepsValue = true,
        Name = EnsureOperationName,
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value of the validation result, which is ignored.")],
        Summary = "Validates the value of a successful result with an operation that returns a result of any value type, and keeps the original value when " +
            "that operation succeeds. A failed result passes through and the delegate is not called.",
        Delegates =
        [
            new(
                ValidationName,
                ["T"],
                ResultOfTResult,
                "The validation to run with the value of the successful result. The value of its result is ignored."),
        ],
        ReturnType = ResultOfT,
        ReturnDescription = "The receiver when it failed or the validation succeeded, otherwise a failed result with the error of the validation.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var validationResult = {ctx.Call(ValidationName, InputName)};",
            "var validationFailure = validationResult.Unpack(out _);",
            $"return validationFailure is null ? {ctx.R} : Result<T>.Failed(validationFailure);",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_validation_succeeded",
                SuccessOfTwo,
                [Returns(ValidationName, ["x"], SuccessOfChecked, ResultOfString)],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1ValidationCalls]),
            Case(
                "returns_the_error_of_the_validation_when_the_validation_failed",
                SuccessOfTwo,
                [Returns(ValidationName, ["x"], FailureOfChecked, ResultOfString)],
                [AssertSameErrorOutcomeGetError, AssertEqual1ValidationCalls]),
            Case(
                ReceiverFailed,
                FailureOfInt,
                [Returns(ValidationName, ["x"], SuccessOfChecked, ResultOfString)],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, validationCalls);"]),
        ],
    };

    private static OperationSpec EnsureConditionFromResult(bool withMessage) => new()
    {
        Stem = withMessage ? "EnsureWithMessageFromResult" : "EnsureFromResult",
        KeepsValue = true,
        Name = EnsureOperationName,
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = withMessage
            ? "Turns a successful result into a failure with the given message when a condition does not hold. A failed result passes through " +
                "with its own error and the condition is not evaluated."
            : "Turns a successful result into a failure when a condition does not hold. A failed result passes through with its own error and " +
                "the condition is not evaluated.",
        Delegates = [new(ConditionName, [], "bool", "The condition that must hold for the result to stay a success.")],
        ExtraParameters =
        [
            withMessage
                ? new(StringType, "message", "The message of the error of the failed result that is returned when the condition does not hold.")
                : new(ErrorType, ErrorName, "The error of the failed result that is returned when the condition does not hold."),
        ],
        ReturnType = ResultType,
        ReturnDescription = withMessage
            ? "The receiver when it failed or the condition holds, otherwise a failed result whose error was created from <paramref name=\"message\"/>."
            : "The receiver when it failed or the condition holds, otherwise a failed result with <paramref name=\"error\"/>.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var holds = {ctx.Call(ConditionName, string.Empty)};",
            withMessage ? $"return holds ? {ctx.R} : Result.Failure(message);" : $"return holds ? {ctx.R} : new Result(error);",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_condition_holds",
                SuccessWithoutValue,
                [Returns(ConditionName, [], "true", "bool")],
                [AssertTrueOutcomeIsSuccess, AssertEqual1ConditionCalls],
                extraArguments: [withMessage ? TooSmallMessage : ErrorName]),
            Case(
                withMessage
                    ? "returns_an_error_with_the_given_message_when_the_condition_does_not_hold"
                    : "returns_the_given_error_when_the_condition_does_not_hold",
                SuccessWithoutValue,
                [Returns(ConditionName, [], "false", "bool")],
                [withMessage ? "Assert.Equal(\"Too small.\", outcome.GetError().Message);" : AssertSameErrorOutcomeGetError, AssertEqual1ConditionCalls],
                extraArguments: [withMessage ? TooSmallMessage : ErrorName]),
            Case(
                "keeps_the_error_of_the_receiver_without_evaluating_the_condition_when_the_receiver_failed",
                "Result.Failure(other)",
                [Returns(ConditionName, [], "true", "bool")],
                [AssertSameOtherOutcomeGetError, AssertEqual0ConditionCalls],
                extraArguments: [withMessage ? TooSmallMessage : ErrorName],
                setup: [OtherErrorSetup]),
        ],
    };

    private static OperationSpec EnsureOperationFromResultOfT(bool typed) => new()
    {
        Stem = typed ? "EnsureWithTypedOperationFromResultOfT" : "EnsureWithOperationFromResultOfT",
        KeepsValue = true,
        Name = EnsureOperationName,
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = typed ? ["T", TResultName] : ["T"],
        TypeParameterDocs = typed
            ? [new("T", TDoc), new(TResultName, "The type of the value of the validation result, which is ignored.")]
            : [new("T", TDoc)],
        Summary = "Validates a successful result with an operation that returns a result" + (typed ? " of any value type" : string.Empty) +
            " and does not need the value, and keeps the original value when that operation succeeds. A failed result passes through and the " +
            "delegate is not called.",
        Delegates =
        [
            new(
                ValidationName,
                [],
                typed ? ResultOfTResult : ResultType,
                typed ? TypedOperationDoc : OperationDoc),
        ],
        ReturnType = ResultOfT,
        ReturnDescription = "The receiver when it failed or the validation succeeded, otherwise a failed result with the error of the validation.",
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var validationResult = {ctx.Call(ValidationName, string.Empty)};",
            typed ? "var validationFailure = validationResult.Unpack(out _);" : "var validationFailure = validationResult.FailureOrNull();",
            $"return validationFailure is null ? {ctx.R} : Result<T>.Failed(validationFailure);",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_validation_succeeded",
                SuccessOfTwo,
                [Returns(ValidationName, [], typed ? SuccessOfChecked : SuccessWithoutValue, typed ? ResultOfString : ResultType)],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1ValidationCalls]),
            Case(
                "returns_the_error_of_the_validation_when_the_validation_failed",
                SuccessOfTwo,
                [Returns(ValidationName, [], typed ? FailureOfChecked : FailureWithError, typed ? ResultOfString : ResultType)],
                [AssertSameErrorOutcomeGetError, AssertEqual1ValidationCalls]),
            Case(
                ReceiverFailed,
                FailureOfInt,
                [Returns(ValidationName, [], typed ? SuccessOfChecked : SuccessWithoutValue, typed ? ResultOfString : ResultType)],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, validationCalls);"]),
        ],
    };

    private static OperationSpec RecoverFromResultOfT() => new()
    {
        Stem = "RecoverFromResultOfT",
        KeepsValue = true,
        Name = "Recover",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Turns a failed result into a success by computing a replacement value from the error. A successful result passes through " +
            "unchanged and the delegate is not called.",
        Delegates = [new(RecoverName, [ErrorType], "T", "Computes the replacement value from the error of the failed result.")],
        ReturnType = ResultOfT,
        ReturnDescription = "The successful receiver, or a successful result that holds the replacement value.",
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"var value = {ctx.Call(RecoverName, FailureName)};",
            "return Result<T>.Succeeded(value);",
        ],
        Tests =
        [
            Case(
                "returns_a_success_with_the_replacement_value_when_the_receiver_failed",
                FailureOfInt,
                [Returns(RecoverName, ["e"], "5", "int")],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(5, outcome.GetValue());", "Assert.Equal(1, recoverCalls);"]),
            Case(
                ReceiverSucceeded,
                SuccessOfTwo,
                [Returns(RecoverName, ["e"], "5", "int")],
                [AssertEqual2OutcomeGetValue, "Assert.Equal(0, recoverCalls);"]),
        ],
    };

    private static OperationSpec CompensateFromResultOfT() => new()
    {
        Stem = "CompensateFromResultOfT",
        KeepsValue = true,
        Name = "Compensate",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Tries a fallback operation after a failure. The delegate receives the error of a failed result and returns a new result; a " +
            "successful result passes through unchanged and the delegate is not called.",
        Delegates = [new(CompensateName, [ErrorType], ResultOfT, "The fallback operation to run with the error of the failed result.")],
        ReturnType = ResultOfT,
        ReturnDescription = "The successful receiver, or the result of the fallback operation.",
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"return {ctx.Call(CompensateName, FailureName)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.Done(ctx.R)};",
            "}",
            string.Empty,
            "return compensate(failure);",
        ],
        Tests =
        [
            Case(
                "returns_the_result_of_the_delegate_when_the_receiver_failed",
                FailureOfInt,
                [Returns(CompensateName, ["e"], SuccessOfFive, ResultOfInt)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(5, outcome.GetValue());", "Assert.Equal(1, compensateCalls);"]),
            Case(
                ReceiverSucceeded,
                SuccessOfTwo,
                [Returns(CompensateName, ["e"], SuccessOfFive, ResultOfInt)],
                [AssertEqual2OutcomeGetValue, "Assert.Equal(0, compensateCalls);"]),
        ],
    };

    private static OperationSpec CompensateFromResult() => new()
    {
        Stem = "CompensateFromResult",
        KeepsValue = true,
        Name = "Compensate",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Tries a fallback operation after a failure. The delegate receives the error of a failed result and returns a new result; a " +
            "successful result passes through unchanged and the delegate is not called.",
        Delegates = [new(CompensateName, [ErrorType], ResultType, "The fallback operation to run with the error of the failed result.")],
        ReturnType = ResultType,
        ReturnDescription = "The successful receiver, or the result of the fallback operation.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"return {ctx.Call(CompensateName, FailureName)};",
        ],
        ForwardBody = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.Done(ctx.R)};",
            "}",
            string.Empty,
            "return compensate(failure);",
        ],
        Tests =
        [
            Case(
                "returns_the_result_of_the_delegate_when_the_receiver_failed",
                FailureWithError,
                [Returns(CompensateName, ["e"], SuccessWithoutValue, ResultType)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(1, compensateCalls);"]),
            Case(
                ReceiverSucceeded,
                SuccessWithoutValue,
                [Returns(CompensateName, ["e"], "Result.Failure(e)", ResultType)],
                [AssertTrueOutcomeIsSuccess, "Assert.Equal(0, compensateCalls);"]),
        ],
    };

    private static OperationSpec MatchFromResultOfT() => new()
    {
        Stem = "MatchFromResultOfT",
        Name = "Match",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value that both delegates produce.")],
        Summary = "Leaves the railway: turns a result into a single value by running one of two delegates, depending on whether the result succeeded.",
        Delegates =
        [
            new(OnSuccessName, ["T"], TResultName, "Produces the value from the value of a successful result."),
            new(OnFailureName, [ErrorType], TResultName, "Produces the value from the error of a failed result."),
        ],
        ReturnType = TResultName,
        ReturnDescription = "The value produced by <paramref name=\"onSuccess\"/> or <paramref name=\"onFailure\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.Call(OnSuccessName, InputName)};",
            "}",
            string.Empty,
            $"return {ctx.Call(OnFailureName, FailureName)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            "    return onSuccess(input);",
            "}",
            string.Empty,
            "return onFailure(failure);",
        ],
        Tests =
        [
            Case(
                RunsOnlyTheSuccessDelegateWhenTheReceiverSucceeded,
                SuccessOfTwo,
                [Returns(OnSuccessName, ["x"], "\"ok:\" + x", StringType), Returns(OnFailureName, ["e"], FailEMessage, StringType)],
                ["""Assert.Equal("ok:2", outcome);""", "Assert.Equal(1, onSuccessCalls);", "Assert.Equal(0, onFailureCalls);"]),
            Case(
                RunsOnlyTheFailureDelegateWhenTheReceiverFailed,
                FailureOfInt,
                [Returns(OnSuccessName, ["x"], "\"ok:\" + x", StringType), Returns(OnFailureName, ["e"], FailEMessage, StringType)],
                ["""Assert.Equal("fail:Boom.", outcome);""", "Assert.Equal(0, onSuccessCalls);", "Assert.Equal(1, onFailureCalls);"]),
        ],
    };

    private static OperationSpec MatchFromResult() => new()
    {
        Stem = "MatchFromResult",
        Name = "Match",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [TResultName],
        TypeParameterDocs = [new(TResultName, "The type of the value that both delegates produce.")],
        Summary = "Leaves the railway: turns a result into a single value by running one of two delegates, depending on whether the result succeeded.",
        Delegates =
        [
            new(OnSuccessName, [], TResultName, "Produces the value for a successful result."),
            new(OnFailureName, [ErrorType], TResultName, "Produces the value from the error of a failed result."),
        ],
        ReturnType = TResultName,
        ReturnDescription = "The value produced by <paramref name=\"onSuccess\"/> or <paramref name=\"onFailure\"/>.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.Call(OnSuccessName, string.Empty)};",
            "}",
            string.Empty,
            $"return {ctx.Call(OnFailureName, FailureName)};",
        ],
        ForwardBody = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            "    return onSuccess();",
            "}",
            string.Empty,
            "return onFailure(failure);",
        ],
        Tests =
        [
            Case(
                RunsOnlyTheSuccessDelegateWhenTheReceiverSucceeded,
                SuccessWithoutValue,
                [Returns(OnSuccessName, [], "\"ok\"", StringType), Returns(OnFailureName, ["e"], FailEMessage, StringType)],
                ["""Assert.Equal("ok", outcome);""", "Assert.Equal(1, onSuccessCalls);", "Assert.Equal(0, onFailureCalls);"]),
            Case(
                RunsOnlyTheFailureDelegateWhenTheReceiverFailed,
                FailureWithError,
                [Returns(OnSuccessName, [], "\"ok\"", StringType), Returns(OnFailureName, ["e"], FailEMessage, StringType)],
                ["""Assert.Equal("fail:Boom.", outcome);""", "Assert.Equal(0, onSuccessCalls);", "Assert.Equal(1, onFailureCalls);"]),
        ],
    };

    private static OperationSpec SwitchFromResultOfT() => new()
    {
        Stem = "SwitchFromResultOfT",
        Name = "Switch",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Leaves the railway for side effects only: runs one of two delegates, depending on whether the result succeeded.",
        Delegates =
        [
            new(OnSuccessName, ["T"], "void", "The side effect to run with the value of a successful result."),
            new(OnFailureName, [ErrorType], "void", "The side effect to run with the error of a failed result."),
        ],
        ReturnType = "void",
        ReturnDescription = "Nothing; for the asynchronous variants a task that completes when the delegate has completed.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    {ctx.CallStatement(OnSuccessName, InputName)}",
            "}",
            "else",
            "{",
            $"    {ctx.CallStatement(OnFailureName, FailureName)}",
            "}",
        ],
        Tests =
        [
            Case(
                RunsOnlyTheSuccessDelegateWhenTheReceiverSucceeded,
                SuccessOfTwo,
                [Does(OnSuccessName, ["x"], """log.Add("ok:" + x);"""), Does(OnFailureName, ["e"], """log.Add("fail");""")],
                ["""Assert.Equal(["ok:2"], log);"""],
                setup: [VarLogNewListString]),
            Case(
                RunsOnlyTheFailureDelegateWhenTheReceiverFailed,
                FailureOfInt,
                [Does(OnSuccessName, ["x"], """log.Add("ok");"""), Does(OnFailureName, ["e"], """log.Add("fail:" + e.Message);""")],
                ["""Assert.Equal(["fail:Boom."], log);"""],
                setup: [VarLogNewListString]),
        ],
    };

    private static OperationSpec SwitchFromResult() => new()
    {
        Stem = "SwitchFromResult",
        Name = "Switch",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Leaves the railway for side effects only: runs one of two delegates, depending on whether the result succeeded.",
        Delegates =
        [
            new(OnSuccessName, [], "void", "The side effect to run for a successful result."),
            new(OnFailureName, [ErrorType], "void", "The side effect to run with the error of a failed result."),
        ],
        ReturnType = "void",
        ReturnDescription = "Nothing; for the asynchronous variants a task that completes when the delegate has completed.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    {ctx.CallStatement(OnSuccessName, string.Empty)}",
            "}",
            "else",
            "{",
            $"    {ctx.CallStatement(OnFailureName, FailureName)}",
            "}",
        ],
        Tests =
        [
            Case(
                RunsOnlyTheSuccessDelegateWhenTheReceiverSucceeded,
                SuccessWithoutValue,
                [Does(OnSuccessName, [], """log.Add("ok");"""), Does(OnFailureName, ["e"], """log.Add("fail");""")],
                ["""Assert.Equal(["ok"], log);"""],
                setup: [VarLogNewListString]),
            Case(
                RunsOnlyTheFailureDelegateWhenTheReceiverFailed,
                FailureWithError,
                [Does(OnSuccessName, [], """log.Add("ok");"""), Does(OnFailureName, ["e"], """log.Add("fail:" + e.Message);""")],
                ["""Assert.Equal(["fail:Boom."], log);"""],
                setup: [VarLogNewListString]),
        ],
    };
}
