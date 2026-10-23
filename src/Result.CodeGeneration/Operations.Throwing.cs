using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// The explicit ways to leave the railway with an exception: <c>GetValueOrThrow</c> and <c>ThrowIfFailure</c>, with a function that creates the
/// exception from the error, and without one for a <c>Task</c> or <c>ValueTask</c> receiver (the synchronous ones are <c>GetValue()</c> and
/// <c>ThrowIfFailure()</c>, which are members of the result).
/// </content>
internal static partial class Operations
{
    private const string ExceptionFactoryName = "exceptionFactory";
    private const string ExceptionFactoryType = "Func<Error, Exception>";
    private const string ThrownSetup = """var thrown = new InvalidOperationException("Thrown.");""";
    private const string ThrowTheFactoryException = "    throw Guard.NotNull(exceptionFactory(failure), nameof(exceptionFactory));";
    private const string AssertSameThrownException = "Assert.Same(thrown, exception);";
    private const string AssertNoException = "Assert.Null(exception);";
    private const string FactoryReturnsThrown = "_ => thrown";
    private const string FactorySeesError = "e => { seen = e; return thrown; }";
    private const string SeenErrorSetup = "Error? seen = null;";
    private const string AssertFailedExceptionCarriesError = "Assert.Same(error, Assert.IsType<ResultFailedException>(exception).Error);";

    private static IEnumerable<OperationSpec> Throwing()
    {
        yield return GetValueOrThrowWithFactory();
        yield return GetValueOrThrowWithoutFactory();
        yield return ThrowIfFailureWithFactory();
        yield return ThrowIfFailureWithoutFactory();
    }

    private static ExtraParameter ExceptionFactoryParameter() =>
        new(ExceptionFactoryType, ExceptionFactoryName, "Creates the exception to throw from the error of the failed result.");

    private static OperationSpec GetValueOrThrowWithFactory() => new()
    {
        Stem = "GetValueOrThrowWithFactoryFromResultOfT",
        Name = "GetValueOrThrow",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Leaves the railway: returns the value of a successful result, or throws the exception that a function creates from the error " +
            "of a failed result, so that a failure can become an exception of your own type.",
        ExtraParameters = [ExceptionFactoryParameter()],
        SkipsCompletedFastPath = true,
        ReturnType = "T",
        ReturnDescription = "The value of the successful result.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            ThrowTheFactoryException,
            "}",
            string.Empty,
            "return input;",
        ],
        Tests =
        [
            new(
                "returns_the_value_when_the_receiver_succeeded",
                SuccessOfTwo,
                [],
                [FactoryReturnsThrown],
                ["Assert.Equal(2, outcome);"],
                [ThrownSetup]),
            new(
                "throws_the_exception_of_the_factory_when_the_receiver_failed",
                FailureOfInt,
                [],
                [FactoryReturnsThrown],
                [AssertSameThrownException],
                [ThrownSetup],
                null,
                true),
            new(
                "gives_the_error_of_the_receiver_to_the_factory",
                FailureOfInt,
                [],
                [FactorySeesError],
                ["Assert.Same(error, seen);", AssertSameThrownException],
                [ThrownSetup, SeenErrorSetup],
                null,
                true),
        ],
    };

    private static OperationSpec GetValueOrThrowWithoutFactory() => new()
    {
        Stem = "GetValueOrThrowFromResultOfT",
        Name = "GetValueOrThrow",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Leaves the railway: returns the value of a successful result, or throws a <see cref=\"ResultFailedException\"/> that carries the " +
            "error of a failed result. It is the explicit form of <see cref=\"Result{T}.GetValue\"/> for a result that is still to come.",
        SkipsSyncReceiver = true,
        SkipsCompletedFastPath = true,
        ReturnType = "T",
        ReturnDescription = "The value of the successful result.",
        Body = ctx => [$"return {ctx.R}.GetValue();"],
        Tests =
        [
            new("returns_the_value_when_the_receiver_succeeded", SuccessOfTwo, [], [], ["Assert.Equal(2, outcome);"]),
            new(
                "throws_a_failed_exception_with_the_error_when_the_receiver_failed",
                FailureOfInt,
                [],
                [],
                [AssertFailedExceptionCarriesError],
                null,
                null,
                true),
        ],
    };

    private static OperationSpec ThrowIfFailureWithFactory() => new()
    {
        Stem = "ThrowIfFailureWithFactoryFromResult",
        Name = "ThrowIfFailure",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Leaves the railway: throws the exception that a function creates from the error of a failed result, so that a failure can " +
            "become an exception of your own type. A successful result does nothing.",
        ExtraParameters = [ExceptionFactoryParameter()],
        SkipsCompletedFastPath = true,
        ReturnType = "void",
        ReturnDescription = "Nothing.",
        Body = ctx =>
        [
            Failure(ctx),
            string.Empty,
            IfFailureIsNotNull,
            "{",
            ThrowTheFactoryException,
            "}",
        ],
        Tests =
        [
            new(
                "does_not_throw_when_the_receiver_succeeded",
                SuccessWithoutValue,
                [],
                [FactoryReturnsThrown],
                [AssertNoException],
                [ThrownSetup],
                null,
                true),
            new(
                "throws_the_exception_of_the_factory_when_the_receiver_failed",
                FailureWithError,
                [],
                [FactoryReturnsThrown],
                [AssertSameThrownException],
                [ThrownSetup],
                null,
                true),
            new(
                "gives_the_error_of_the_receiver_to_the_factory",
                FailureWithError,
                [],
                [FactorySeesError],
                ["Assert.Same(error, seen);", AssertSameThrownException],
                [ThrownSetup, SeenErrorSetup],
                null,
                true),
        ],
    };

    private static OperationSpec ThrowIfFailureWithoutFactory() => new()
    {
        Stem = "ThrowIfFailureFromResult",
        Name = "ThrowIfFailure",
        ReceiverType = ResultType,
        ReceiverTestType = ResultType,
        ReceiverDescription = ResultReceiverDescription,
        TypeParameters = [],
        TypeParameterDocs = [],
        Summary = "Leaves the railway: throws a <see cref=\"ResultFailedException\"/> that carries the error of a failed result. A successful result " +
            "does nothing. It is the explicit form of <see cref=\"Result.ThrowIfFailure\"/> for a result that is still to come.",
        SkipsSyncReceiver = true,
        SkipsCompletedFastPath = true,
        ReturnType = "void",
        ReturnDescription = "Nothing.",
        Body = ctx => [$"{ctx.R}.ThrowIfFailure();"],
        Tests =
        [
            new("does_not_throw_when_the_receiver_succeeded", SuccessWithoutValue, [], [], [AssertNoException], null, null, true),
            new(
                "throws_a_failed_exception_with_the_error_when_the_receiver_failed",
                FailureWithError,
                [],
                [],
                [AssertFailedExceptionCarriesError],
                null,
                null,
                true),
        ],
    };
}
