using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// The <c>*If</c> family: <c>MapIf</c>, <c>BindIf</c>, <c>TapIf</c>, <c>EnsureIf</c> and <c>TapErrorIf</c>, which run their delegate only when a condition
/// holds. Each exists for a <c>bool</c>, which forwards to the plain operation, and for a function that is evaluated only when it matters.
/// </content>
internal static partial class Operations
{
    private const string TapName = "tap";
    private const string TapIfName = "TapIf";
    private const string BindIfName = "BindIf";
    private const string EnsureIfName = "EnsureIf";
    private const string TapErrorIfName = "TapErrorIf";
    private const string AssertNoTapCalls = "Assert.Equal(0, tapCalls);";
    private const string AssertNoTapErrorCalls = "Assert.Equal(0, tapErrorCalls);";
    private const string AssertNoValidationCalls = "Assert.Equal(0, validationCalls);";
    private const string BindName = "bind";

    /// <summary>How the condition of a <c>*If</c> operation is given.</summary>
    private enum IfCondition
    {
        /// <summary>A plain <see cref="bool"/>: the operation forwards to the plain one when it is true.</summary>
        Bool,

        /// <summary>A function without parameters, called only when the receiver is in the state the operation works on.</summary>
        Function,

        /// <summary>A function that gets the value (or, for <c>TapErrorIf</c>, the error) of the receiver.</summary>
        OfValue,
    }

    // A method, not a field: the order in which static fields of partial files are initialized is not defined, and All reads this.
    private static IfShape[] CreateIfShapes() =>
    [
        new()
        {
            Name = "MapIf",
            BaseName = "Map",
            StemPart = "MapIf",
            Typed = true,
            Summary = "Transforms the value of a successful result, keeping its type, when a condition holds. Otherwise, and for a failed result, " +
                "the receiver is returned as it was and the delegate is not called.",
            Delegate = new("map", ["T"], "T", "Computes the new value from the value of the successful result."),
            ReturnDescription = "A successful result that holds the value computed by the delegate when the condition held, otherwise the receiver.",
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns("map", ["x"], "x * 10", "int"),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(20, outcome.GetValue());", "Assert.Equal(1, mapCalls);"],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(0, mapCalls);"],
            OtherAsserts = [AssertSameErrorOutcomeGetError, "Assert.Equal(0, mapCalls);"],
            Act = ctx => [$"var mapped = {ctx.Call("map", InputName)};", "return Result<T>.Succeeded(mapped);"],
        },
        new()
        {
            Name = BindIfName,
            BaseName = "Bind",
            StemPart = BindIfName,
            Typed = true,
            Summary = "Chains an operation that can fail and returns a result of the same type, when a condition holds. Otherwise, and for a failed " +
                "result, the receiver is returned as it was and the delegate is not called.",
            Delegate = new(BindName, ["T"], ResultOfT, "The operation to run with the value of the successful result."),
            ReturnDescription = "The result of the delegate when the condition held, otherwise the receiver.",
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns(BindName, ["x"], "Result.Success(x * 10)", ResultOfInt),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(20, outcome.GetValue());", AssertEqual1BindCalls],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual0BindCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls],
            Act = ctx => [$"return {ctx.Call(BindName, InputName)};"],
            ActForwarded = [$"return {BindName}({InputName});"],
        },
        new()
        {
            Name = TapIfName,
            BaseName = "Tap",
            StemPart = TapIfName,
            Typed = true,
            KeepsValue = true,
            StateChange = StateChange.Never,
            Summary = "Runs a side effect with the value of a successful result when a condition holds, and returns the result unchanged. The delegate " +
                "is not called for a failed result or when the condition does not hold.",
            Delegate = new(TapName, ["T"], "void", "The side effect to run with the value of the successful result."),
            ReturnDescription = ReceiverUnchangedDescription,
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Does(TapName, ["x"], "seen = x;"),
            Setup = ["var seen = 0;"],
            RunAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(2, seen);", "Assert.Equal(1, tapCalls);"],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, "Assert.Equal(0, seen);", AssertNoTapCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoTapCalls],
            Act = ctx => [ctx.CallStatement(TapName, InputName), string.Empty, $"return {ctx.R};"],
        },
        new()
        {
            Name = EnsureIfName,
            BaseName = "Ensure",
            StemPart = EnsureIfName,
            Typed = true,
            KeepsValue = true,
            Summary = "Validates the value of a successful result with an operation that returns a result, when a condition holds, and keeps the " +
                "original value when that operation succeeds. The delegate is not called for a failed result or when the condition does not hold.",
            Delegate = new(ValidationName, ["T"], ResultType, "The validation to run with the value of the successful result."),
            ReturnDescription = "The receiver when it failed, the condition did not hold or the validation succeeded, otherwise a failed result with " +
                "the error of the validation.",
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns(ValidationName, ["x"], "Result.Failure(other)", ResultType),
            PassTest = Returns(ValidationName, ["x"], SuccessWithoutValue, ResultType),
            Setup = [OtherErrorSetup],
            RunAsserts = [AssertSameOtherOutcomeGetError, AssertEqual1ValidationCalls],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertNoValidationCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoValidationCalls],
            Act = ctx => EnsureAct(ctx, "validationResult.FailureOrNull()"),
        },
        new()
        {
            Name = EnsureIfName,
            BaseName = "Ensure",
            StemPart = "EnsureIfTyped",
            Typed = true,
            KeepsValue = true,
            TypeParameters = ["T", TResultName],
            TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value of the validation result, which is ignored.")],
            Summary = "Validates the value of a successful result with an operation that returns a result of any value type, when a condition holds, " +
                "and keeps the original value when that operation succeeds. The delegate is not called for a failed result or when the condition " +
                "does not hold.",
            Delegate = new(
                ValidationName,
                ["T"],
                ResultOfTResult,
                "The validation to run with the value of the successful result. The value of its result is ignored."),
            ReturnDescription = "The receiver when it failed, the condition did not hold or the validation succeeded, otherwise a failed result with " +
                "the error of the validation.",
            GateReceiver = SuccessOfTwo,
            OtherReceiver = FailureOfInt,
            Test = Returns(ValidationName, ["x"], "Result.Failure<string>(other)", ResultOfString),
            PassTest = Returns(ValidationName, ["x"], SuccessOfChecked, ResultOfString),
            Setup = [OtherErrorSetup],
            RunAsserts = [AssertSameOtherOutcomeGetError, AssertEqual1ValidationCalls],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertNoValidationCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoValidationCalls],
            Act = ctx => EnsureAct(ctx, "validationResult.Unpack(out _)"),
        },
        new()
        {
            Name = TapErrorIfName,
            BaseName = "TapError",
            StemPart = TapErrorIfName,
            Typed = true,
            FailureGated = true,
            KeepsValue = true,
            StateChange = StateChange.Never,
            Summary = "Runs a side effect with the error of a failed result when a condition holds, and returns the result unchanged. The delegate is " +
                "not called for a successful result or when the condition does not hold.",
            Delegate = new(TapErrorName, [ErrorType], "void", "The side effect to run with the error of the failed result."),
            ReturnDescription = ReceiverUnchangedDescription,
            GateReceiver = FailureOfInt,
            OtherReceiver = SuccessOfTwo,
            Test = Does(TapErrorName, ["e"], "seen = e;"),
            Setup = ["Error? seen = null;"],
            RunAsserts = [AssertSameErrorOutcomeGetError, "Assert.Same(error, seen);", "Assert.Equal(1, tapErrorCalls);"],
            SkipAsserts = [AssertSameErrorOutcomeGetError, "Assert.Null(seen);", AssertNoTapErrorCalls],
            OtherAsserts = [AssertTrueOutcomeIsSuccess, AssertNoTapErrorCalls],
            Act = ctx => [ctx.CallStatement(TapErrorName, FailureName), string.Empty, $"return {ctx.R};"],
        },
        new()
        {
            Name = TapIfName,
            BaseName = "Tap",
            StemPart = TapIfName,
            Typed = false,
            KeepsValue = true,
            StateChange = StateChange.Never,
            Summary = "Runs a side effect for a successful result when a condition holds, and returns the result unchanged. The delegate is not " +
                "called for a failed result or when the condition does not hold.",
            Delegate = new(TapName, [], "void", "The side effect to run."),
            ReturnDescription = ReceiverUnchangedDescription,
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Does(TapName, []),
            RunAsserts = [AssertTrueOutcomeIsSuccess, "Assert.Equal(1, tapCalls);"],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertNoTapCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertNoTapCalls],
            Act = ctx => [ctx.CallStatement(TapName, string.Empty), string.Empty, $"return {ctx.R};"],
        },
        new()
        {
            Name = BindIfName,
            BaseName = "Bind",
            StemPart = BindIfName,
            Typed = false,
            Summary = "Chains an operation that can fail and returns no value, when a condition holds. Otherwise, and for a failed result, the " +
                "receiver is returned as it was and the delegate is not called.",
            Delegate = new(BindName, [], ResultType, "The operation to run."),
            ReturnDescription = "The result of the delegate when the condition held, otherwise the receiver.",
            GateReceiver = SuccessWithoutValue,
            OtherReceiver = FailureWithError,
            Test = Returns(BindName, [], "Result.Failure(other)", ResultType),
            Setup = [OtherErrorSetup],
            RunAsserts = [AssertSameOtherOutcomeGetError, AssertEqual1BindCalls],
            SkipAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual0BindCalls],
            OtherAsserts = [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls],
            Act = ctx => [$"return {ctx.Call(BindName, string.Empty)};"],
            ActForwarded = [$"return {BindName}();"],
        },
        new()
        {
            Name = TapErrorIfName,
            BaseName = "TapError",
            StemPart = TapErrorIfName,
            Typed = false,
            FailureGated = true,
            KeepsValue = true,
            StateChange = StateChange.Never,
            Summary = "Runs a side effect with the error of a failed result when a condition holds, and returns the result unchanged. The delegate is " +
                "not called for a successful result or when the condition does not hold.",
            Delegate = new(TapErrorName, [ErrorType], "void", "The side effect to run with the error of the failed result."),
            ReturnDescription = ReceiverUnchangedDescription,
            GateReceiver = FailureWithError,
            OtherReceiver = SuccessWithoutValue,
            Test = Does(TapErrorName, ["e"], "seen = e;"),
            Setup = ["Error? seen = null;"],
            RunAsserts = [AssertSameErrorOutcomeGetError, "Assert.Same(error, seen);", "Assert.Equal(1, tapErrorCalls);"],
            SkipAsserts = [AssertSameErrorOutcomeGetError, "Assert.Null(seen);", AssertNoTapErrorCalls],
            OtherAsserts = [AssertTrueOutcomeIsSuccess, AssertNoTapErrorCalls],
            Act = ctx => [ctx.CallStatement(TapErrorName, FailureName), string.Empty, $"return {ctx.R};"],
        },
    ];

    /// <summary>Gets the operations of the <c>*If</c> family: every shape with a <c>bool</c> condition and with a function that decides.</summary>
    private static IEnumerable<OperationSpec> IfFamily() => CreateIfShapes().SelectMany(IfSpecs);

    private static string[] EnsureAct(BodyContext ctx, string failureOfValidation) =>
    [
        $"var validationResult = {ctx.Call(ValidationName, InputName)};",
        $"var validationFailure = {failureOfValidation};",
        $"return validationFailure is null ? {ctx.R} : Result<T>.Failed(validationFailure);",
    ];

    private static IEnumerable<OperationSpec> IfSpecs(IfShape shape)
    {
        yield return IfSpec(shape, IfCondition.Bool);
        yield return IfSpec(shape, IfCondition.Function);

        // A condition on the value (or the error) needs something to read it from: untyped successes have no value.
        if (shape.Typed || shape.FailureGated)
        {
            yield return IfSpec(shape, IfCondition.OfValue);
        }
    }

    private static OperationSpec IfSpec(IfShape shape, IfCondition condition)
    {
        var receiver = shape.Typed ? ResultOfT : ResultType;
        var type = ConditionType(shape, condition);
        var documentation = ConditionDocumentation(shape, condition);
        var stemKind = condition switch
        {
            IfCondition.Bool => "Bool",
            IfCondition.Function => "Func",
            _ => "ValueFunc",
        };

        return new OperationSpec
        {
            Stem = $"{shape.StemPart}{stemKind}From{(shape.Typed ? "ResultOfT" : ResultType)}",
            Name = shape.Name,
            KeepsValue = shape.KeepsValue,
            StateChange = shape.StateChange,
            ReceiverType = receiver,
            ReceiverTestType = shape.Typed ? ResultOfInt : ResultType,
            ReceiverDescription = shape.Typed ? ResultOfTReceiverDescription : ResultReceiverDescription,
            TypeParameters = shape.TypeParameters ?? (shape.Typed ? ["T"] : []),
            TypeParameterDocs = shape.TypeParameterDocs ?? (shape.Typed ? [new("T", TDoc)] : []),
            Summary = shape.Summary,
            LeadingParameters = [new(type, ConditionName, documentation, Validated: condition != IfCondition.Bool)],
            Delegates = [shape.Delegate],
            ReturnType = shape.ReturnType,
            ReturnDescription = shape.ReturnDescription,
            ForwardsTo = condition == IfCondition.Bool ? shape.BaseName : null,
            Body = ctx => IfBody(shape, condition, ctx, forwarded: false),
            ForwardBody = shape.ActForwarded is null ? null : ctx => IfBody(shape, condition, ctx, forwarded: true),
            Tests = IfTests(shape, condition),
        };
    }

    private static string ConditionType(IfShape shape, IfCondition condition) => condition switch
    {
        IfCondition.Bool => "bool",
        IfCondition.Function => "Func<bool>",
        _ => shape.FailureGated ? $"Func<{ErrorType}, bool>" : "Func<T, bool>",
    };

    private static string ConditionDocumentation(IfShape shape, IfCondition condition)
    {
        var gate = shape.FailureGated ? "failed" : "succeeded";

        return condition switch
        {
            IfCondition.Bool => "Whether the delegate runs. When it is <see langword=\"false\"/> the receiver is returned as it was.",
            IfCondition.Function => $"Decides whether the delegate runs. It is called only when the receiver {gate}.",
            _ => $"Decides whether the delegate runs. It is called only when the receiver {gate}, with its {(shape.FailureGated ? "error" : "value")}.",
        };
    }

    // Reads the state of the receiver once, returns it as it was when the receiver is not in the state the operation works on or the condition
    // does not hold, and otherwise does what the plain operation does.
    private static string[] IfBody(IfShape shape, IfCondition condition, BodyContext ctx, bool forwarded)
    {
        var state = (shape.Typed, shape.FailureGated) switch
        {
            (true, false) => Unpack(ctx),
            (true, true) => Unpack(ctx, "_"),
            _ => Failure(ctx),
        };
        var skipped = forwarded ? ctx.Done(ctx.R) : ctx.R;

        // A plain bool forwards to the plain operation and has no body of its own, so only the two kinds of function are written here.
        var holds = condition switch
        {
            IfCondition.Function => $"{ConditionName}()",
            _ => $"{ConditionName}({(shape.FailureGated ? FailureName : InputName)})",
        };

        List<string> lines =
        [
            state,
            string.Empty,
            shape.FailureGated ? IfFailureIsNull : IfFailureIsNotNull,
            "{",
            $"    return {skipped};",
            "}",
            string.Empty,
            $"if (!{holds})",
            "{",
            $"    return {skipped};",
            "}",
            string.Empty,
        ];

        lines.AddRange(forwarded ? shape.ActForwarded! : shape.Act(ctx));

        return [.. lines];
    }

    private static TestCaseSpec[] IfTests(IfShape shape, IfCondition condition)
    {
        var counted = condition != IfCondition.Bool;
        var gateWord = shape.FailureGated ? "failed" : "succeeded";
        var otherWord = shape.FailureGated ? "succeeded" : "failed";
        var setup = counted ? ["var conditionCalls = 0;", .. shape.Setup ?? []] : shape.Setup;
        var yes = LeadingArgument(shape, condition, holds: true);
        var no = LeadingArgument(shape, condition, holds: false);
        var skipName = counted ? "does_not_run_the_delegate_when_the_condition_does_not_hold" : "does_not_run_the_delegate_when_the_condition_is_false";
        var otherName = counted
            ? $"does_not_evaluate_the_condition_or_run_the_delegate_when_the_receiver_{otherWord}"
            : $"does_not_run_the_delegate_when_the_receiver_{otherWord}";

        string[] WithCount(string[] asserts, int calls) => counted ? [.. asserts, $"Assert.Equal({calls}, conditionCalls);"] : asserts;

        List<TestCaseSpec> cases =
        [
            new(
                $"runs_the_delegate_when_the_receiver_{gateWord}_and_the_condition_holds",
                shape.GateReceiver,
                [shape.Test],
                [],
                WithCount(shape.RunAsserts, 1),
                setup,
                [yes]),
            new(skipName, shape.GateReceiver, [shape.Test], [], WithCount(shape.SkipAsserts, 1), setup, [no]),
            new(otherName, shape.OtherReceiver, [shape.Test], [], WithCount(shape.OtherAsserts, 0), setup, [yes]),
        ];

        if (shape.PassTest is not null)
        {
            string[] passAsserts = [AssertTrueOutcomeIsSuccess, AssertEqual2OutcomeGetValue, AssertEqual1ValidationCalls];
            string[] passSetup = counted ? ["var conditionCalls = 0;"] : [];

            cases.Add(new(
                "keeps_the_receiver_when_the_validation_succeeded",
                shape.GateReceiver,
                [shape.PassTest],
                [],
                WithCount(passAsserts, 1),
                passSetup,
                [yes]));
        }

        return [.. cases];
    }

    // The argument of a test for a condition that holds or does not: the plain value, or a function that counts its calls.
    private static string LeadingArgument(IfShape shape, IfCondition condition, bool holds) => condition switch
    {
        IfCondition.Bool => holds ? "true" : "false",
        IfCondition.Function => $"() => {{ conditionCalls++; return {(holds ? "true" : "false")}; }}",
        _ when shape.FailureGated => $"e => {{ conditionCalls++; return {(holds ? "ReferenceEquals(e, error)" : "false")}; }}",
        _ => $"x => {{ conditionCalls++; return x == {(holds ? "2" : "3")}; }}",
    };

    /// <summary>What distinguishes one <c>*If</c> operation from another; the rest of the spec is derived from it.</summary>
    private sealed record IfShape
    {
        /// <summary>Gets the name of the extension method.</summary>
        internal required string Name { get; init; }

        /// <summary>Gets the name of the plain operation that a <see cref="bool"/> condition forwards to.</summary>
        internal required string BaseName { get; init; }

        /// <summary>Gets the part of the file names that tells this shape from the other shapes with the same method name.</summary>
        internal required string StemPart { get; init; }

        /// <summary>Gets a value indicating whether the receiver is a typed result.</summary>
        internal required bool Typed { get; init; }

        /// <summary>Gets a value indicating whether the operation works on a failed result (<c>TapErrorIf</c>) instead of a successful one.</summary>
        internal bool FailureGated { get; init; }

        /// <summary>Gets a value indicating whether the operation never changes the value of its receiver.</summary>
        internal bool KeepsValue { get; init; }

        /// <summary>Gets the delegate of the test in which the validation of <c>EnsureIf</c> succeeds, or <see langword="null"/> when there is none.</summary>
        internal DelegateTest? PassTest { get; init; }

        /// <summary>Gets how the operation can change the state of its receiver.</summary>
        internal StateChange StateChange { get; init; }

        /// <summary>Gets the type parameters when they are not just <c>T</c> or none.</summary>
        internal string[]? TypeParameters { get; init; }

        /// <summary>Gets the documentation of <see cref="TypeParameters"/>.</summary>
        internal TypeParameterDoc[]? TypeParameterDocs { get; init; }

        /// <summary>Gets the summary of the operation.</summary>
        internal required string Summary { get; init; }

        /// <summary>Gets the delegate that runs when the condition holds.</summary>
        internal required DelegateSpec Delegate { get; init; }

        /// <summary>Gets the documentation of the return value.</summary>
        internal required string ReturnDescription { get; init; }

        /// <summary>Gets the receiver for which the operation can run.</summary>
        internal required string GateReceiver { get; init; }

        /// <summary>Gets the receiver for which the operation never runs.</summary>
        internal required string OtherReceiver { get; init; }

        /// <summary>Gets how the delegate behaves in the tests.</summary>
        internal required DelegateTest Test { get; init; }

        /// <summary>Gets the lines that arrange the tests.</summary>
        internal string[]? Setup { get; init; }

        /// <summary>Gets what the tests assert when the delegate ran.</summary>
        internal required string[] RunAsserts { get; init; }

        /// <summary>Gets what the tests assert when the condition did not hold.</summary>
        internal required string[] SkipAsserts { get; init; }

        /// <summary>Gets what the tests assert for the receiver that the operation does not work on.</summary>
        internal required string[] OtherAsserts { get; init; }

        /// <summary>Gets the lines that do the work once the receiver is in the right state and the condition holds.</summary>
        internal required Func<BodyContext, string[]> Act { get; init; }

        /// <summary>Gets the same lines for a synchronous receiver with an asynchronous delegate, which hands back the task of the delegate.</summary>
        internal string[]? ActForwarded { get; init; }

        /// <summary>Gets the synchronous return type.</summary>
        internal string ReturnType => Typed ? ResultOfT : ResultType;
    }
}
