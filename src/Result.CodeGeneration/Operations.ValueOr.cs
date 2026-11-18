using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// <c>ValueOr</c> leaves the railway with a value: the value of a success, or a fallback for a failure.
/// The fallback is computed by a delegate that receives the error or by one that does not,
/// and a <c>Task</c> or <c>ValueTask</c> receiver can also take the value itself
/// (the synchronous form is the instance method <c>ValueOr(T)</c>, which is a member of the result).
/// </content>
internal static partial class Operations
{
    private const string FallbackName = "fallback";
    private const string FallbackFactoryName = "fallbackFactory";
    private const string FallbackReturnsNine = "9";

    private static IEnumerable<OperationSpec> ValueOrFamily()
    {
        yield return ValueOrWithErrorFromResultOfT();
        yield return ValueOrWithoutErrorFromResultOfT();
        yield return ValueOrWithValueFromResultOfT();
    }

    private static OperationSpec ValueOrWithErrorFromResultOfT() => new()
    {
        Stem = "ValueOrWithErrorFromResultOfT",
        Name = "ValueOr",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Leaves the railway: returns the value of a successful result, or computes a fallback from the error of a failed result. " +
            "The delegate is not called when the result succeeded.",
        Delegates = [new(FallbackFactoryName, [ErrorType], "T", "Computes the value to return from the error of the failed result.")],
        ReturnType = "T",
        ReturnDescription = "The value of the successful result, or the value computed by the delegate.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            "    return input;",
            "}",
            string.Empty,
            $"return {ctx.Call(FallbackFactoryName, FailureName)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.Done(InputName)};",
            "}",
            string.Empty,
            "return fallbackFactory(failure);",
        ],
        Tests =
        [
            Case(
                "returns_the_value_without_running_the_delegate_when_the_receiver_succeeded",
                SuccessOfTwo,
                [Returns(FallbackFactoryName, ["e"], FallbackReturnsNine, "int")],
                ["Assert.Equal(2, outcome);", "Assert.Equal(0, fallbackFactoryCalls);"]),
            Case(
                "returns_the_value_of_the_delegate_when_the_receiver_failed",
                FailureOfInt,
                [Returns(FallbackFactoryName, ["e"], FallbackReturnsNine, "int")],
                ["Assert.Equal(9, outcome);", "Assert.Equal(1, fallbackFactoryCalls);"]),
            Case(
                "gives_the_error_of_the_receiver_to_the_delegate",
                FailureOfInt,
                [Returns(FallbackFactoryName, ["e"], "e.Message.Length", "int")],
                ["Assert.Equal(5, outcome);"]),
        ],
    };

    private static OperationSpec ValueOrWithoutErrorFromResultOfT() => new()
    {
        Stem = "ValueOrWithoutErrorFromResultOfT",
        Name = "ValueOr",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Leaves the railway: returns the value of a successful result, or computes a fallback when the result failed. " +
            "The delegate is not called when the result succeeded.",
        Delegates = [new(FallbackFactoryName, [], "T", "Computes the value to return when the result failed.")],
        ReturnType = "T",
        ReturnDescription = "The value of the successful result, or the value computed by the delegate.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            "    return input;",
            "}",
            string.Empty,
            $"return {ctx.Call(FallbackFactoryName, string.Empty)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNull,
            "{",
            $"    return {ctx.Done(InputName)};",
            "}",
            string.Empty,
            "return fallbackFactory();",
        ],
        Tests =
        [
            Case(
                "returns_the_value_without_running_the_delegate_when_the_receiver_succeeded",
                SuccessOfTwo,
                [Returns(FallbackFactoryName, [], FallbackReturnsNine, "int")],
                ["Assert.Equal(2, outcome);", "Assert.Equal(0, fallbackFactoryCalls);"]),
            Case(
                "returns_the_value_of_the_delegate_when_the_receiver_failed",
                FailureOfInt,
                [Returns(FallbackFactoryName, [], FallbackReturnsNine, "int")],
                ["Assert.Equal(9, outcome);", "Assert.Equal(1, fallbackFactoryCalls);"]),
        ],
    };

    private static OperationSpec ValueOrWithValueFromResultOfT() => new()
    {
        Stem = "ValueOrWithValueFromResultOfT",
        Name = "ValueOr",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Leaves the railway: returns the value of a successful result, or a fallback value when the result failed. " +
            "It is the form of <see cref=\"Result{T}.ValueOr(T)\"/> for a result that is still to come.",
        ExtraParameters = [new("T", FallbackName, "The value to return for a failed result.", false)],
        SkipsSyncReceiver = true,
        SkipsCompletedFastPath = true,
        ReturnType = "T",
        ReturnDescription = "The value of the successful result, or <paramref name=\"fallback\"/>.",
        Body = ctx => [$"return {ctx.R}.ValueOr(fallback);"],
        Tests =
        [
            new("returns_the_value_when_the_receiver_succeeded", SuccessOfTwo, [], [FallbackReturnsNine], ["Assert.Equal(2, outcome);"]),
            new("returns_the_fallback_when_the_receiver_failed", FailureOfInt, [], [FallbackReturnsNine], ["Assert.Equal(9, outcome);"]),
        ],
    };
}
