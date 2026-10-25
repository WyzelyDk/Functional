using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// Small conveniences on the railway: <c>Tap</c> for a side effect that needs no value, <c>EnsureNot</c> for a condition that must not hold, and
/// <c>EnsureNotNull</c> for a value that must not be <see langword="null"/>, which also changes the type from <c>Result&lt;T?&gt;</c> to
/// <c>Result&lt;T&gt;</c>.
/// </content>
internal static partial class Operations
{
    private const string MissingMessage = "\"Missing.\"";
    private const string ErrorFactoryParameter = "errorFactory";
    private const string ErrorFactoryType = "Func<Error>";
    private const string NullWord = "<see langword=\"null\"/>";

    /// <summary>Whether the value of an <c>EnsureNotNull</c> is a reference type or a nullable value type.</summary>
    private enum NullKind
    {
        /// <summary>A reference type: only the annotation changes.</summary>
        Class,

        /// <summary>A nullable value type: the value is unwrapped.</summary>
        Struct,
    }

    /// <summary>How the error of an <c>EnsureNotNull</c> is given.</summary>
    private enum NullError
    {
        /// <summary>An error.</summary>
        Error,

        /// <summary>A message.</summary>
        Message,

        /// <summary>A function that creates the error, called only when it is needed.</summary>
        Factory,
    }

    private static IEnumerable<OperationSpec> Conveniences()
    {
        yield return TapWithoutValueFromResultOfT();
        yield return EnsureNotSpec(message: false);
        yield return EnsureNotSpec(message: true);

        foreach (var kind in new[] { NullKind.Class, NullKind.Struct })
        {
            foreach (var form in new[] { NullError.Error, NullError.Message, NullError.Factory })
            {
                yield return EnsureNotNullSpec(kind, form);
            }
        }
    }

    private static OperationSpec TapWithoutValueFromResultOfT() => new()
    {
        Stem = "TapWithoutValueFromResultOfT",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "Tap",
        ReceiverType = ResultOfT,
        ReceiverTestType = ResultOfInt,
        ReceiverDescription = ResultOfTReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        Summary = "Runs a side effect that needs no value for a successful result and returns the result unchanged. The delegate is not called " +
            "for a failed result.",
        Delegates = [new("tap", [], "void", "The side effect to run.")],
        ReturnType = ResultOfT,
        ReturnDescription = ReceiverUnchangedDescription,
        Body = ctx =>
        [
            Unpack(ctx, "_"),
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
                SuccessOfTwo,
                [Does("tap", [])],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(1, tapCalls);"]),
            Case(
                "does_not_run_the_delegate_when_the_receiver_failed",
                FailureOfInt,
                [Does("tap", [])],
                [AssertSameErrorOutcomeGetError, "Assert.Equal(0, tapCalls);"]),
        ],
    };

    // The mirror of Ensure: the value is rejected when the predicate holds.
    private static OperationSpec EnsureNotSpec(bool message)
    {
        var failure = message ? "Result.Failure<T>(message)" : "Result<T>.Failed(error)";

        return new OperationSpec
        {
            Stem = message ? "EnsureNotWithMessageFromResultOfT" : "EnsureNotFromResultOfT",
            KeepsValue = true,
            Name = "EnsureNot",
            ReceiverType = ResultOfT,
            ReceiverTestType = ResultOfInt,
            ReceiverDescription = ResultOfTReceiverDescription,
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", TDoc)],
            Summary = "Turns a successful result into a failure when its value satisfies a condition that must not hold. A failed result passes " +
                "through with its own error and the predicate is not called.",
            Delegates = [new(PredicateName, ["T"], "bool", "The condition the value must not satisfy.")],
            ExtraParameters = [EnsureNotParameter(message)],
            ReturnType = ResultOfT,
            ReturnDescription = "The receiver when it failed or its value does not satisfy the condition, otherwise a failed result with " +
                (message ? "an error created from <paramref name=\"message\"/>." : "<paramref name=\"error\"/>."),
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
                $"return holds ? {failure} : {ctx.R};",
            ],
            Tests = EnsureNotTests(message),
        };
    }

    /// <summary>Gets the parameter of <c>EnsureNot</c> that carries the error, or the message of the error.</summary>
    private static ExtraParameter EnsureNotParameter(bool message) => message
        ? new ExtraParameter(StringType, "message", "The message of the error of the failed result that is returned when the condition holds.")
        : new ExtraParameter(ErrorType, ErrorName, "The error of the failed result that is returned when the condition holds.");

    /// <summary>Gets the generated tests of <c>EnsureNot</c>.</summary>
    private static TestCaseSpec[] EnsureNotTests(bool message)
    {
        var argument = message ? TooSmallMessage : ErrorName;

        return
        [
            new(
                "returns_the_receiver_when_the_value_does_not_satisfy_the_condition",
                SuccessOfTwo,
                [Returns(PredicateName, ["x"], "x > 5", "bool")],
                [argument],
                [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1PredicateCalls]),
            new(
                "returns_the_failure_when_the_value_satisfies_the_condition",
                SuccessOfTwo,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [argument],
                [message ? "Assert.Equal(\"Too small.\", outcome.GetError().Message);" : AssertSameErrorOutcomeGetError, AssertEqual1PredicateCalls]),
            new(
                "keeps_the_error_of_the_receiver_without_running_the_predicate_when_the_receiver_failed",
                "Result.Failure<int>(other)",
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [argument],
                [AssertSameOtherOutcomeGetError, "Assert.Equal(0, predicateCalls);"],
                [OtherErrorSetup]),
        ];
    }

    // Result<T?> becomes Result<T>: a null value is the failure, and what remains is known to be there. A reference type only changes its
    // annotation; a nullable value type is unwrapped, which needs a second overload with its own constraint.
    private static OperationSpec EnsureNotNullSpec(NullKind kind, NullError form)
    {
        var isStruct = kind == NullKind.Struct;
        var failure = EnsureNotNullFailure(form);

        return new OperationSpec
        {
            Stem = $"EnsureNotNull{kind}{form}FromResultOfT",
            Name = "EnsureNotNull",
            ReceiverType = "Result<T?>",
            ReceiverTestType = $"Result<{(isStruct ? "int?" : "string?")}>",
            ReceiverDescription = "a <see cref=\"Result{T}\"/> whose value may be <see langword=\"null\"/>",
            TypeParameters = ["T"],
            TypeParameterDocs = [new("T", "The type of the value, without the nullable annotation.")],
            Structs = isStruct ? ["T"] : [],
            Constraints = isStruct ? [] : ["where T : class"],
            Summary = "Turns a successful result whose value is <see langword=\"null\"/> into a failure, and returns a result of the same value " +
                "without the nullable annotation otherwise. A failed result passes through with its own error.",
            ExtraParameters = [EnsureNotNullParameter(form)],
            ReturnType = "Result<T>",
            ReturnDescription = "The result with a value that is not null, or a failed result.",
            Body = ctx => EnsureNotNullBody(ctx, isStruct, failure),
            Tests = EnsureNotNullTests(isStruct, form),
        };
    }

    /// <summary>Gets the parameter of <c>EnsureNotNull</c> that carries the error, its message, or a factory of it.</summary>
    private static ExtraParameter EnsureNotNullParameter(NullError form) => form switch
    {
        NullError.Error => new ExtraParameter(ErrorType, ErrorName, $"The error of the failed result that is returned when the value is {NullWord}."),
        NullError.Message => new ExtraParameter(StringType, "message", $"The message of the error that is returned when the value is {NullWord}."),
        _ => new ExtraParameter(
            ErrorFactoryType,
            ErrorFactoryParameter,
            $"Creates the error that is returned when the value is {NullWord}. It is called only then."),
    };

    /// <summary>Gets the expression that creates the failed result of <c>EnsureNotNull</c>.</summary>
    private static string EnsureNotNullFailure(NullError form) => form switch
    {
        NullError.Error => "Result<T>.Failed(error)",
        NullError.Message => "Result.Failure<T>(message)",
        _ => $"Result.Failure<T>({ErrorFactoryParameter}())",
    };

    /// <summary>Gets the argument that the generated tests of <c>EnsureNotNull</c> pass for the error.</summary>
    private static string EnsureNotNullArgument(NullError form) => form switch
    {
        NullError.Error => ErrorName,
        NullError.Message => MissingMessage,
        _ => "() => { factoryCalls++; return error; }",
    };

    /// <summary>Gets the body of <c>EnsureNotNull</c>.</summary>
    private static string[] EnsureNotNullBody(BodyContext ctx, bool isStruct, string failure) =>
    [
        Unpack(ctx),
        string.Empty,
        IfFailureIsNotNull,
        "{",
        "    return Result<T>.Failed(failure);",
        "}",
        string.Empty,
        isStruct ? "if (!input.HasValue)" : "if (input is null)",
        "{",
        $"    return {failure};",
        "}",
        string.Empty,
        isStruct ? "return Result<T>.Succeeded(input.GetValueOrDefault());" : "return Result<T>.Succeeded(input);",
    ];

    /// <summary>Gets the generated tests of <c>EnsureNotNull</c>.</summary>
    private static TestCaseSpec[] EnsureNotNullTests(bool isStruct, NullError form)
    {
        var present = isStruct ? "5" : "\"a\"";
        var testType = isStruct ? "int?" : "string?";
        var argument = EnsureNotNullArgument(form);
        var factory = form == NullError.Factory;
        string[] setup = factory ? ["var factoryCalls = 0;"] : [];
        string[] noCalls = factory ? ["Assert.Equal(0, factoryCalls);"] : [];
        string[] oneCall = factory ? ["Assert.Equal(1, factoryCalls);"] : [];
        var missing = form == NullError.Message ? "Assert.Equal(\"Missing.\", outcome.GetError().Message);" : AssertSameErrorOutcomeGetError;

        return
        [
            new(
                "returns_the_value_without_the_annotation_when_it_is_not_null",
                $"Result.Success<{testType}>({present})",
                [],
                [argument],
                [AssertTrueOutcomeIsSuccess, $"Assert.Equal({present}, outcome.GetValue());", .. noCalls],
                setup),
            new(
                "returns_the_failure_when_the_value_is_null",
                $"Result.Success<{testType}>(null)",
                [],
                [argument],
                [missing, .. oneCall],
                setup),
            new(
                "keeps_the_error_of_the_receiver_when_the_receiver_failed",
                $"Result.Failure<{testType}>(other)",
                [],
                [argument],
                [AssertSameOtherOutcomeGetError, .. noCalls],
                [OtherErrorSetup, .. setup]),
        ];
    }
}
