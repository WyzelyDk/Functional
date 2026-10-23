using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.MaybeCodeGeneration;

/// <summary>
/// The catalog of operations. Adding an operation here adds it, in all nine variants and with tests, to the library.
/// </summary>
/// <remarks>
/// The bodies read the state of the receiver once (<c>Unpack</c>) and build maybes with the internal factory, so that nothing is
/// checked or validated twice on the hot path.
/// </remarks>
internal static class Operations
{
    private const string TDoc = "The type of the value of the receiver maybe.";

    private const string NullableMapDelegateDoc =
        "Computes the new value from the value of the maybe. It may return <see langword=\"null\"/>, which becomes none.";

    private const string ReceiverNone = "returns_none_without_running_the_delegate_when_the_receiver_has_no_value";
    private const string ReceiverSome = "returns_the_receiver_without_running_the_delegate_when_the_receiver_has_a_value";

    private const string PredicateName = "predicate";
    private const string GreaterThanOne = "x > 1";
    private const string AssertEqual1PredicateCalls = "Assert.Equal(1, predicateCalls);";
    private const string AssertEqual0PredicateCalls = "Assert.Equal(0, predicateCalls);";
    private const string AssertTrueOutcome = "Assert.True(outcome);";
    private const string AssertFalseOutcome = "Assert.False(outcome);";
    private const string AssertValueIsTwo = "Assert.Equal(2, outcome.GetValue());";
    private const string IfFound = "if (found)";
    private const string TapNoneName = "tapNone";
    private const string AlternativeName = "alternative";
    private const string FallbackName = "fallback";
    private const string OnSomeName = "onSome";
    private const string OnNoneName = "onNone";
    private const string StringType = "string";
    private const string MaybeOfT = "Maybe<T>";
    private const string MaybeOfInt = "Maybe<int>";
    private const string MaybeReceiverDescription = "a <see cref=\"Maybe{T}\"/>";
    private const string TResultName = "TResult";
    private const string MaybeOfTResult = "Maybe<TResult>";
    private const string IfNotFound = "if (!found)";
    private const string ReturnDefault = "    return default;";
    private const string InputName = "input";
    private const string SomeOfTwo = "Maybe.Some(2)";
    private const string NoneOfInt = "Maybe.None<int>()";
    private const string AssertTrueOutcomeIsNone = "Assert.True(outcome.IsNone);";

    /// <summary>Gets every operation family.</summary>
    internal static IReadOnlyList<OperationSpec> All { get; } =
    [
        Map(),
        MapStruct(),
        Bind(),
        Filter(),
        Quantifier(all: false),
        Quantifier(all: true),
        Contains(),
        Tap(),
        TapNone(),
        Or(),
        ValueOr(),
        Match(),
        Switch(),
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

    // Reads the state of the receiver once: `found` is true when it has a value, and `input` then holds it.
    private static string Unpack(BodyContext ctx, string value = "var input") => $"var found = {ctx.R}.Unpack(out {value});";

    private static OperationSpec Map() => new()
    {
        Stem = "Map",
        Name = "Map",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value that the delegate produces.")],
        NotNull = ["T", TResultName],
        Summary = "Transforms the value of a maybe. A maybe without a value stays without a value and the delegate is not called. A <see " +
            "langword=\"null\"/> that the delegate produces becomes none.",
        Delegates = [new("map", ["T"], "TResult?", NullableMapDelegateDoc)],
        ReturnType = MaybeOfTResult,
        ReturnDescription = "A maybe that holds the value computed by the delegate, or none when the receiver has no value or the delegate " +
            "produced <see langword=\"null\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfNotFound,
            "{",
            ReturnDefault,
            "}",
            string.Empty,
            $"var value = {ctx.Call("map", InputName)};",
            "return Maybe<TResult>.Of(value);",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_and_wraps_its_value_when_the_receiver_has_a_value",
                SomeOfTwo,
                [Returns("map", ["x"], "x * 10", "int")],
                ["Assert.True(outcome.IsSome);", "Assert.Equal(20, outcome.GetValue());", "Assert.Equal(1, mapCalls);"]),
            Case(
                ReceiverNone,
                NoneOfInt,
                [Returns("map", ["x"], "x * 10", "int")],
                [AssertTrueOutcomeIsNone, "Assert.Equal(0, mapCalls);"]),
        ],
    };

    // The overload for a delegate that returns a nullable value type (int?). It wins over Map for such a lambda because Nullable<TResult>
    // is more specific than TResult, so a nullable struct behaves like a nullable reference: null becomes none, a value becomes some.
    private static OperationSpec MapStruct() => new()
    {
        Stem = "MapStruct",
        Name = "Map",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The value type that the delegate produces, when it is not <see langword=\"null\"/>.")],
        NotNull = ["T"],
        Structs = [TResultName],
        Summary = "Transforms the value of a maybe with a delegate that returns a nullable value type. A <see langword=\"null\"/> result becomes " +
            "none, so the result is a <c>Maybe&lt;TResult&gt;</c> and never a <c>Maybe&lt;TResult?&gt;</c>. A maybe without a value stays without " +
            "a value and the delegate is not called.",
        Delegates = [new("map", ["T"], "TResult?", NullableMapDelegateDoc)],
        ReturnType = MaybeOfTResult,
        ReturnDescription = "A maybe that holds the value computed by the delegate, or none when the receiver has no value or the delegate " +
            "produced <see langword=\"null\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfNotFound,
            "{",
            ReturnDefault,
            "}",
            string.Empty,
            $"var value = {ctx.Call("map", InputName)};",
            "return value.HasValue ? new Maybe<TResult>(value.GetValueOrDefault()) : default;",
        ],
        Tests =
        [
            Case(
                "wraps_the_value_when_the_delegate_returns_a_nullable_value",
                SomeOfTwo,
                [Returns("map", ["x"], "(int?)(x * 10)", "int?")],
                ["Assert.Equal(20, outcome.GetValue());", "Assert.Equal(1, mapCalls);"]),
            Case(
                "returns_none_when_the_delegate_returns_null",
                SomeOfTwo,
                [Returns("map", ["x"], "(int?)null", "int?")],
                [AssertTrueOutcomeIsNone, "Assert.Equal(1, mapCalls);"]),
            Case(
                ReceiverNone,
                NoneOfInt,
                [Returns("map", ["x"], "(int?)(x * 10)", "int?")],
                [AssertTrueOutcomeIsNone, "Assert.Equal(0, mapCalls);"]),
        ],
    };

    private static OperationSpec Bind() => new()
    {
        Stem = "Bind",
        Name = "Bind",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value of the maybe that the delegate returns.")],
        NotNull = ["T", TResultName],
        Summary = "Chains an operation that may produce no value. The delegate runs only when the receiver has a value; a maybe without a value " +
            "passes through as none.",
        Delegates = [new("bind", ["T"], MaybeOfTResult, "The operation to run with the value of the maybe.")],
        ReturnType = MaybeOfTResult,
        ReturnDescription = "The maybe of the delegate, or none when the receiver has no value.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfNotFound,
            "{",
            ReturnDefault,
            "}",
            string.Empty,
            $"return {ctx.Call("bind", InputName)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfNotFound,
            "{",
            $"    return {ctx.Done("default(Maybe<TResult>)")};",
            "}",
            string.Empty,
            "return bind(input);",
        ],
        Tests =
        [
            Case(
                "returns_the_maybe_of_the_delegate_when_the_receiver_has_a_value",
                SomeOfTwo,
                [Returns("bind", ["x"], "Maybe.Some(x * 10)", MaybeOfInt)],
                ["Assert.True(outcome.IsSome);", "Assert.Equal(20, outcome.GetValue());", "Assert.Equal(1, bindCalls);"]),
            Case(
                "returns_none_when_the_delegate_returns_none",
                SomeOfTwo,
                [Returns("bind", ["x"], NoneOfInt, MaybeOfInt)],
                [AssertTrueOutcomeIsNone, "Assert.Equal(1, bindCalls);"]),
            Case(
                ReceiverNone,
                NoneOfInt,
                [Returns("bind", ["x"], "Maybe.Some(x * 10)", MaybeOfInt)],
                [AssertTrueOutcomeIsNone, "Assert.Equal(0, bindCalls);"]),
        ],
    };

    private static OperationSpec Filter() => new()
    {
        Stem = "Filter",
        KeepsValue = true,
        StateChange = StateChange.ToNone,
        Name = "Filter",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Turns a maybe into none when its value does not satisfy a condition. A maybe without a value stays without a value and the " +
            "predicate is not called.",
        Delegates = [new(PredicateName, ["T"], "bool", "The condition the value must satisfy.")],
        ReturnType = MaybeOfT,
        ReturnDescription = "The receiver when it has a value that satisfies the condition, otherwise none.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfNotFound,
            "{",
            ReturnDefault,
            "}",
            string.Empty,
            $"var holds = {ctx.Call(PredicateName, InputName)};",
            $"return holds ? {ctx.R} : default;",
        ],
        Tests =
        [
            Case(
                "returns_the_receiver_when_the_value_satisfies_the_condition",
                SomeOfTwo,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertValueIsTwo, AssertEqual1PredicateCalls]),
            Case(
                "returns_none_when_the_value_does_not_satisfy_the_condition",
                SomeOfTwo,
                [Returns(PredicateName, ["x"], "x > 5", "bool")],
                [AssertTrueOutcomeIsNone, AssertEqual1PredicateCalls]),
            Case(
                ReceiverNone,
                NoneOfInt,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertTrueOutcomeIsNone, AssertEqual0PredicateCalls]),
        ],
    };

    private static OperationSpec Quantifier(bool all) => new()
    {
        Stem = all ? "All" : "Any",
        Name = all ? "All" : "Any",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = all
            ? "Leaves the railway: returns whether a maybe has no value or has a value that satisfies a condition. A maybe without a value gives " +
                "true and the predicate is not called."
            : "Leaves the railway: returns whether a maybe has a value that satisfies a condition. A maybe without a value gives false and the " +
                "predicate is not called.",
        Delegates = [new(PredicateName, ["T"], "bool", "The condition the value must satisfy.")],
        ReturnType = "bool",
        ReturnDescription = all
            ? "<see langword=\"true\"/> when the receiver has no value or its value satisfies the condition, otherwise <see langword=\"false\"/>."
            : "<see langword=\"true\"/> when the receiver has a value that satisfies the condition, otherwise <see langword=\"false\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfNotFound,
            "{",
            all ? "    return true;" : "    return false;",
            "}",
            string.Empty,
            $"return {ctx.Call(PredicateName, InputName)};",
        ],
        Tests =
        [
            Case(
                "returns_true_when_the_value_satisfies_the_condition",
                SomeOfTwo,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [AssertTrueOutcome, AssertEqual1PredicateCalls]),
            Case(
                "returns_false_when_the_value_does_not_satisfy_the_condition",
                SomeOfTwo,
                [Returns(PredicateName, ["x"], "x > 5", "bool")],
                [AssertFalseOutcome, AssertEqual1PredicateCalls]),
            Case(
                all
                    ? "returns_true_without_running_the_predicate_when_the_receiver_has_no_value"
                    : "returns_false_without_running_the_predicate_when_the_receiver_has_no_value",
                NoneOfInt,
                [Returns(PredicateName, ["x"], GreaterThanOne, "bool")],
                [all ? AssertTrueOutcome : AssertFalseOutcome, AssertEqual0PredicateCalls]),
        ],
    };

    private static OperationSpec Contains() => new()
    {
        Stem = "Contains",
        Name = "Contains",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Leaves the railway: returns whether a maybe has a value that equals the given value, compared with the default equality of " +
            "the type.",
        ExtraParameters = [new("T", "value", "The value to look for.", Validated: false)],
        ReturnType = "bool",
        ReturnDescription = "<see langword=\"true\"/> when the receiver has a value that equals <paramref name=\"value\"/>, " +
            "otherwise <see langword=\"false\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            "return found && EqualityComparer<T>.Default.Equals(input, value);",
        ],
        Tests =
        [
            Case("returns_true_when_the_value_equals_the_given_value", SomeOfTwo, [], [AssertTrueOutcome], extraArguments: ["2"]),
            Case("returns_false_when_the_value_differs_from_the_given_value", SomeOfTwo, [], [AssertFalseOutcome], extraArguments: ["3"]),
            Case("returns_false_when_the_receiver_has_no_value", NoneOfInt, [], [AssertFalseOutcome], extraArguments: ["2"]),
        ],
    };

    private static OperationSpec Tap() => new()
    {
        Stem = "Tap",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "Tap",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Runs a side effect, for example logging, with the value of a maybe and returns the maybe unchanged. The delegate is not called " +
            "when there is no value.",
        Delegates = [new("tap", ["T"], "void", "The side effect to run with the value of the maybe.")],
        ReturnType = MaybeOfT,
        ReturnDescription = "The receiver, unchanged.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFound,
            "{",
            $"    {ctx.CallStatement("tap", InputName)}",
            "}",
            string.Empty,
            $"return {ctx.R};",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_with_the_value_and_returns_the_receiver_when_the_receiver_has_a_value",
                SomeOfTwo,
                [Does("tap", ["x"], "seen = x;")],
                [AssertValueIsTwo, "Assert.Equal(2, seen);", "Assert.Equal(1, tapCalls);"],
                setup: ["var seen = 0;"]),
            Case(
                "does_not_run_the_delegate_when_the_receiver_has_no_value",
                NoneOfInt,
                [Does("tap", ["x"])],
                [AssertTrueOutcomeIsNone, "Assert.Equal(0, tapCalls);"]),
        ],
    };

    private static OperationSpec TapNone() => new()
    {
        Stem = "TapNone",
        KeepsValue = true,
        StateChange = StateChange.Never,
        Name = "TapNone",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Runs a side effect, for example logging, when a maybe has no value and returns the maybe unchanged. The delegate is not called " +
            "when there is a value.",
        Delegates = [new(TapNoneName, [], "void", "The side effect to run when the maybe has no value.")],
        ReturnType = MaybeOfT,
        ReturnDescription = "The receiver, unchanged.",
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfNotFound,
            "{",
            $"    {ctx.CallStatement(TapNoneName, string.Empty)}",
            "}",
            string.Empty,
            $"return {ctx.R};",
        ],
        Tests =
        [
            Case(
                "runs_the_delegate_and_returns_the_receiver_when_the_receiver_has_no_value",
                NoneOfInt,
                [Does(TapNoneName, [])],
                [AssertTrueOutcomeIsNone, "Assert.Equal(1, tapNoneCalls);"]),
            Case(
                ReceiverSome,
                SomeOfTwo,
                [Does(TapNoneName, [])],
                [AssertValueIsTwo, "Assert.Equal(0, tapNoneCalls);"]),
        ],
    };

    private static OperationSpec Or() => new()
    {
        Stem = "Or",
        KeepsValue = true,
        StateChange = StateChange.FromNone,
        Name = "Or",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Tries an alternative when a maybe has no value. A maybe with a value passes through unchanged and the delegate is not called.",
        Delegates = [new(AlternativeName, [], MaybeOfT, "The alternative to try when the receiver has no value.")],
        ReturnType = MaybeOfT,
        ReturnDescription = "The receiver when it has a value, otherwise the maybe of the alternative.",
        Body = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFound,
            "{",
            $"    return {ctx.R};",
            "}",
            string.Empty,
            $"return {ctx.Call(AlternativeName, string.Empty)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx, "_"),
            string.Empty,
            IfFound,
            "{",
            $"    return {ctx.Done(ctx.R)};",
            "}",
            string.Empty,
            "return alternative();",
        ],
        Tests =
        [
            Case(
                "returns_the_maybe_of_the_delegate_when_the_receiver_has_no_value",
                NoneOfInt,
                [Returns(AlternativeName, [], "Maybe.Some(5)", MaybeOfInt)],
                ["Assert.Equal(5, outcome.GetValue());", "Assert.Equal(1, alternativeCalls);"]),
            Case(
                ReceiverSome,
                SomeOfTwo,
                [Returns(AlternativeName, [], "Maybe.Some(5)", MaybeOfInt)],
                [AssertValueIsTwo, "Assert.Equal(0, alternativeCalls);"]),
        ],
    };

    private static OperationSpec ValueOr() => new()
    {
        Stem = "ValueOr",
        Name = "ValueOr",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Leaves the railway: returns the value of a maybe, or computes a fallback when there is none. The delegate is not called when " +
            "there is a value.",
        Delegates = [new(FallbackName, [], "T", "Computes the value to return when the receiver has no value.")],
        ReturnType = "T",
        ReturnDescription = "The value of the receiver, or the value computed by the delegate.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFound,
            "{",
            "    return input;",
            "}",
            string.Empty,
            $"return {ctx.Call(FallbackName, string.Empty)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFound,
            "{",
            $"    return {ctx.Done(InputName)};",
            "}",
            string.Empty,
            "return fallback();",
        ],
        Tests =
        [
            Case(
                "returns_the_value_of_the_delegate_when_the_receiver_has_no_value",
                NoneOfInt,
                [Returns(FallbackName, [], "9", "int")],
                ["Assert.Equal(9, outcome);", "Assert.Equal(1, fallbackCalls);"]),
            Case(
                "returns_the_value_without_running_the_delegate_when_the_receiver_has_a_value",
                SomeOfTwo,
                [Returns(FallbackName, [], "9", "int")],
                ["Assert.Equal(2, outcome);", "Assert.Equal(0, fallbackCalls);"]),
        ],
    };

    private static OperationSpec Match() => new()
    {
        Stem = "Match",
        Name = "Match",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T", TResultName],
        TypeParameterDocs = [new("T", TDoc), new(TResultName, "The type of the value that both delegates produce.")],
        NotNull = ["T"],
        Summary = "Leaves the railway: turns a maybe into a single value by running one of two delegates, depending on whether it has a value.",
        Delegates =
        [
            new(OnSomeName, ["T"], TResultName, "Produces the value from the value of the maybe."),
            new(OnNoneName, [], TResultName, "Produces the value when the maybe has no value."),
        ],
        ReturnType = TResultName,
        ReturnDescription = "The value produced by <paramref name=\"onSome\"/> or <paramref name=\"onNone\"/>.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFound,
            "{",
            $"    return {ctx.Call(OnSomeName, InputName)};",
            "}",
            string.Empty,
            $"return {ctx.Call(OnNoneName, string.Empty)};",
        ],
        ForwardBody = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFound,
            "{",
            "    return onSome(input);",
            "}",
            string.Empty,
            "return onNone();",
        ],
        Tests =
        [
            Case(
                "runs_only_the_some_delegate_when_the_receiver_has_a_value",
                SomeOfTwo,
                [Returns(OnSomeName, ["x"], "\"some:\" + x", StringType), Returns(OnNoneName, [], "\"none\"", StringType)],
                ["""Assert.Equal("some:2", outcome);""", "Assert.Equal(1, onSomeCalls);", "Assert.Equal(0, onNoneCalls);"]),
            Case(
                "runs_only_the_none_delegate_when_the_receiver_has_no_value",
                NoneOfInt,
                [Returns(OnSomeName, ["x"], "\"some:\" + x", StringType), Returns(OnNoneName, [], "\"none\"", StringType)],
                ["""Assert.Equal("none", outcome);""", "Assert.Equal(0, onSomeCalls);", "Assert.Equal(1, onNoneCalls);"]),
        ],
    };

    private static OperationSpec Switch() => new()
    {
        Stem = "Switch",
        Name = "Switch",
        ReceiverType = MaybeOfT,
        ReceiverTestType = MaybeOfInt,
        ReceiverDescription = MaybeReceiverDescription,
        TypeParameters = ["T"],
        TypeParameterDocs = [new("T", TDoc)],
        NotNull = ["T"],
        Summary = "Leaves the railway for side effects only: runs one of two delegates, depending on whether the maybe has a value.",
        Delegates =
        [
            new(OnSomeName, ["T"], "void", "The side effect to run with the value of the maybe."),
            new(OnNoneName, [], "void", "The side effect to run when the maybe has no value."),
        ],
        ReturnType = "void",
        ReturnDescription = "Nothing; for the asynchronous variants a task that completes when the delegate has completed.",
        Body = ctx =>
        [
            Unpack(ctx),
            string.Empty,
            IfFound,
            "{",
            $"    {ctx.CallStatement(OnSomeName, InputName)}",
            "}",
            "else",
            "{",
            $"    {ctx.CallStatement(OnNoneName, string.Empty)}",
            "}",
        ],
        Tests =
        [
            Case(
                "runs_only_the_some_delegate_when_the_receiver_has_a_value",
                SomeOfTwo,
                [Does(OnSomeName, ["x"], """log.Add("some:" + x);"""), Does(OnNoneName, [], """log.Add("none");""")],
                ["""Assert.Equal(["some:2"], log);"""],
                setup: ["var log = new List<string>();"]),
            Case(
                "runs_only_the_none_delegate_when_the_receiver_has_no_value",
                NoneOfInt,
                [Does(OnSomeName, ["x"], """log.Add("some");"""), Does(OnNoneName, [], """log.Add("none");""")],
                ["""Assert.Equal(["none"], log);"""],
                setup: ["var log = new List<string>();"]),
        ],
    };
}
