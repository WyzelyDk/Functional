using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// <c>OrElse</c> is what happens after a failure, in two families. An alternative operation returns a new result, which can itself fail,
/// and a replacement returns a value, which makes the result a success.
/// Each takes the error or does not, so a typed result has four forms and an untyped result, which has no value, has the two alternatives.
/// </content>
internal static partial class Operations
{
    private const string AlternativeName = "alternative";
    private const string ReplacementName = "replacement";
    private const string OrElseSuccessOfFive = "Result.Success(5)";

    private static IEnumerable<OperationSpec> OrElseFamily()
    {
        yield return OrElseAlternative(typed: true, withError: true);
        yield return OrElseAlternative(typed: true, withError: false);
        yield return OrElseReplacement(withError: true);
        yield return OrElseReplacement(withError: false);
        yield return OrElseAlternative(typed: false, withError: true);
        yield return OrElseAlternative(typed: false, withError: false);
    }

    private static string[] OrElseParameters(bool withError) => withError ? [ErrorType] : [];

    private static OperationSpec OrElseAlternative(bool typed, bool withError)
    {
        var resultType = typed ? ResultOfT : ResultType;

        return new()
        {
            Stem = "OrElse" + (withError ? "WithError" : string.Empty) + (typed ? "FromResultOfT" : "FromResult"),
            KeepsValue = true,
            Name = "OrElse",
            ReceiverType = resultType,
            ReceiverTestType = typed ? ResultOfInt : ResultType,
            ReceiverDescription = typed ? ResultOfTReceiverDescription : ResultReceiverDescription,
            TypeParameters = typed ? ["T"] : [],
            TypeParameterDocs = typed ? [new("T", TDoc)] : [],
            Summary = OrElseAlternativeSummary(withError),
            Delegates = [new(AlternativeName, OrElseParameters(withError), resultType, "The alternative operation to run when the result failed.")],
            ReturnType = resultType,
            ReturnDescription = "The successful receiver, or the result of the alternative operation.",
            Body = ctx => OrElseAlternativeBody(ctx, typed, withError),
            ForwardBody = ctx => OrElseAlternativeForwardBody(ctx, typed, withError),
            Tests = OrElseAlternativeTests(typed, withError),
        };
    }

    private static string OrElseAlternativeSummary(bool withError)
    {
        var delegateSentence = withError
            ? "The delegate receives the error of a failed result and returns a new result; "
            : "The delegate returns a new result; ";

        return "Tries an alternative operation after a failure. " + delegateSentence +
            "a successful result passes through unchanged and the delegate is not called.";
    }

    private static string[] OrElseAlternativeBody(BodyContext ctx, bool typed, bool withError) =>
    [
        typed ? Unpack(ctx, "_") : Failure(ctx),
        string.Empty,
        IfFailureIsNull,
        "{",
        $"    return {ctx.R};",
        "}",
        string.Empty,
        $"return {ctx.Call(AlternativeName, withError ? FailureName : string.Empty)};",
    ];

    private static string[] OrElseAlternativeForwardBody(BodyContext ctx, bool typed, bool withError) =>
    [
        typed ? Unpack(ctx, "_") : Failure(ctx),
        string.Empty,
        IfFailureIsNull,
        "{",
        $"    return {ctx.Done(ctx.R)};",
        "}",
        string.Empty,
        withError ? "return alternative(failure);" : "return alternative();",
    ];

    private static TestCaseSpec[] OrElseAlternativeTests(bool typed, bool withError) =>
    [
        OrElseAlternativeTookOverCase(typed, withError),
        OrElseAlternativeFailedCase(typed, withError),
        OrElseAlternativeNotNeededCase(typed, withError),
    ];

    private static TestCaseSpec OrElseAlternativeTookOverCase(bool typed, bool withError)
    {
        var testResultType = typed ? ResultOfInt : ResultType;
        var alternativeSuccess = typed ? OrElseSuccessOfFive : SuccessWithoutValue;
        var valueChecks = typed ? new[] { "Assert.Equal(5, outcome.GetValue());" } : [];

        return Case(
            "returns_the_result_of_the_delegate_when_the_receiver_failed",
            typed ? FailureOfInt : FailureWithError,
            [Returns(AlternativeName, OrElseArguments(withError), alternativeSuccess, testResultType)],
            [AssertTrueOutcomeIsSuccess, .. valueChecks, "Assert.Equal(1, alternativeCalls);"]);
    }

    private static TestCaseSpec OrElseAlternativeFailedCase(bool typed, bool withError)
    {
        var failed = typed ? FailureOfInt : FailureWithError;
        var otherFailure = typed ? "Result.Failure<int>(other)" : "Result.Failure(other)";

        return Case(
            "returns_the_failure_of_the_delegate_when_the_alternative_failed",
            failed,
            [Returns(AlternativeName, OrElseArguments(withError), otherFailure, typed ? ResultOfInt : ResultType)],
            [AssertSameOtherOutcomeGetError, "Assert.Equal(1, alternativeCalls);"],
            setup: [OtherErrorSetup]);
    }

    private static TestCaseSpec OrElseAlternativeNotNeededCase(bool typed, bool withError) =>
        Case(
            ReceiverSucceeded,
            typed ? SuccessOfTwo : SuccessWithoutValue,
            [Returns(AlternativeName, OrElseArguments(withError), typed ? FailureOfInt : FailureWithError, typed ? ResultOfInt : ResultType)],
            [typed ? AssertEqual2OutcomeGetValue : AssertTrueOutcomeIsSuccess, "Assert.Equal(0, alternativeCalls);"]);

    private static string[] OrElseArguments(bool withError) => withError ? ["e"] : [];

    private static OperationSpec OrElseReplacement(bool withError)
    {
        var parameters = withError ? new[] { "e" } : [];

        return new()
        {
            Stem = "OrElseValue" + (withError ? "WithError" : string.Empty) + "FromResultOfT",
            KeepsValue = true,
            Name = "OrElse",
            ReceiverType = ResultOfT,
            ReceiverTestType = ResultOfInt,
            ReceiverDescription = ResultOfTReceiverDescription,
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", TDoc)],
            Summary = "Turns a failed result into a success by computing a replacement value" + (withError ? " from the error" : string.Empty) + ". " +
                "A successful result passes through unchanged and the delegate is not called.",
            Delegates = [new(ReplacementName, OrElseParameters(withError), "T", "Computes the replacement value when the result failed.")],
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
                $"var value = {ctx.Call(ReplacementName, withError ? FailureName : string.Empty)};",
                "return Result<T>.Succeeded(value);",
            ],
            Tests =
            [
                Case(
                    "returns_a_success_with_the_replacement_value_when_the_receiver_failed",
                    FailureOfInt,
                    [Returns(ReplacementName, parameters, "5", "int")],
                    [AssertTrueOutcomeIsSuccess, "Assert.Equal(5, outcome.GetValue());", "Assert.Equal(1, replacementCalls);"]),
                Case(
                    ReceiverSucceeded,
                    SuccessOfTwo,
                    [Returns(ReplacementName, parameters, "5", "int")],
                    [AssertEqual2OutcomeGetValue, "Assert.Equal(0, replacementCalls);"]),
            ],
        };
    }
}
