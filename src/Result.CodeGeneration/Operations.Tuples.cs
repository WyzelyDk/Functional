using System.Globalization;
using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <content>
/// Operations on a result that holds a tuple, up to <see cref="GeneratorSettings.MaxZipArity"/> elements, like the one <c>Zip</c> returns:
/// <c>BindZip</c> keeps the value next to the value of the next step (and grows a tuple by one element), and <c>Map</c> and <c>Bind</c> take the
/// elements of the tuple as separate parameters.
/// </content>
internal static partial class Operations
{
    // A method, not a field: the order in which static fields of partial files are initialized is not defined, and All reads this.
    private static string[] TupleElementNames() => ["First", "Second", "Third", "Fourth", "Fifth"];

    private static IEnumerable<OperationSpec> TupleOperations()
    {
        // BindZip starts from a single value (arity 1) and grows a tuple until it reaches the largest arity.
        for (var arity = 1; arity < GeneratorSettings.MaxZipArity; arity++)
        {
            yield return BindZipSpec(arity);
        }

        for (var arity = 2; arity <= GeneratorSettings.MaxZipArity; arity++)
        {
            yield return SpreadSpec(arity, bind: false);
            yield return SpreadSpec(arity, bind: true);
        }
    }

    // The type parameters of a receiver with this many values: T for one value, T1 to Tn for a tuple.
    private static string[] ValueTypes(int arity) => arity == 1 ? ["T"] : [.. Enumerable.Range(1, arity).Select(index => $"T{index}")];

    private static TypeParameterDoc[] ValueTypeDocs(int arity) => arity == 1
        ? [new("T", TDoc)]
        : [.. Enumerable.Range(1, arity).Select(index => new TypeParameterDoc($"T{index}", $"The type of value number {index} of the receiver result."))];

    private static string TupleOf(IEnumerable<string> types) => "(" + string.Join(", ", types) + ")";

    // The same tuple with the element names that Zip uses, so that a result of BindZip reads like one of Zip.
    private static string NamedTuple(IEnumerable<string> types) =>
        "(" + string.Join(", ", types.Select((type, index) => $"{type} {TupleElementNames()[index]}")) + ")";

    private static string ItemsOf(int arity, string tuple) =>
        string.Join(", ", Enumerable.Range(1, arity).Select(index => $"{tuple}.Item{index}"));

    // The values the tests put into the receiver: 2 for a single value, and 1, 2, 3 and so on for a tuple.
    private static string[] ReceiverValues(int arity) => arity == 1
        ? ["2"]
        : [.. Enumerable.Range(1, arity).Select(index => index.ToString(CultureInfo.InvariantCulture))];

    private static string[] SpreadAct(BodyContext ctx, string name, string spread, bool bind) => bind
        ? [$"return {ctx.Call(name, spread)};"]
        : [$"var value = {ctx.Call(name, spread)};", "return Result<TResult>.Succeeded(value);"];

    private static string[] SpreadBindForwarded(BodyContext ctx, string name, string spread) =>
    [
        Unpack(ctx),
        string.Empty,
        IfFailureIsNotNull,
        "{",
        $"    return {ctx.Done("Result<TResult>.Failed(failure)")};",
        "}",
        string.Empty,
        $"return {name}({spread});",
    ];

    private static OperationSpec BindZipSpec(int arity)
    {
        var values = ValueTypes(arity);
        var returnType = $"Result<{NamedTuple([.. values, TResultName])}>";
        var elements = arity == 1 ? "input" : ItemsOf(arity, "input");
        var parameter = arity == 1 ? "T" : TupleOf(values);

        return new OperationSpec
        {
            Stem = $"BindZipArity{arity}FromResultOfT",
            Name = "BindZip",
            ReceiverType = arity == 1 ? ResultOfT : $"Result<{TupleOf(values)}>",
            ReceiverTestType = arity == 1 ? ResultOfInt : $"Result<{TupleOf(Enumerable.Repeat("int", arity))}>",
            ReceiverDescription = "a <see cref=\"Result{T}\"/>",
            TypeParameters = [.. values, TResultName],
            TypeParameterDocs = [.. ValueTypeDocs(arity), new(TResultName, "The type of the value of the result that the delegate returns.")],
            Summary = BindZipSummary(arity),
            Delegates = [new("bind", [parameter], ResultOfTResult, "The operation to run with the value of the successful result.")],
            ReturnType = returnType,
            ReturnDescription = "A successful result that holds the value of the receiver and the value of the delegate, or a failed result with the " +
                "error of the receiver or of the delegate.",
            Body = ctx => BindZipBody(ctx, returnType, elements),
            Tests = BindZipTests(arity),
        };
    }

    /// <summary>Gets the summary of <c>BindZip</c>, which differs for a single value and for a tuple.</summary>
    private static string BindZipSummary(int arity) => arity == 1
        ? "Chains an operation that can fail and keeps the value of the receiver next to the value of that operation, in a tuple. The delegate " +
            "runs only when the receiver succeeded; a failed result passes through with its error."
        : $"Chains an operation that can fail and adds its value to the tuple of the receiver, which becomes a tuple of {arity + 1} values. " +
            "The delegate runs only when the receiver succeeded; a failed result passes through with its error.";

    /// <summary>Gets the body of <c>BindZip</c>.</summary>
    private static string[] BindZipBody(BodyContext ctx, string returnType, string elements) =>
    [
        Unpack(ctx),
        string.Empty,
        IfFailureIsNotNull,
        "{",
        $"    return {returnType}.Failed(failure);",
        "}",
        string.Empty,
        $"var next = {ctx.Call("bind", InputName)};",
        "var nextFailure = next.Unpack(out var value);",
        string.Empty,
        "if (nextFailure is not null)",
        "{",
        $"    return {returnType}.Failed(nextFailure);",
        "}",
        string.Empty,
        $"return {returnType}.Succeeded(({elements}, value));",
    ];

    /// <summary>Gets the generated tests of <c>BindZip</c>.</summary>
    private static TestCaseSpec[] BindZipTests(int arity)
    {
        var testReceiver = arity == 1 ? SuccessOfTwo : $"Result.Success({TupleOf(ReceiverValues(arity))})";
        var testFailed = arity == 1 ? FailureOfInt : $"Result.Failure<{TupleOf(Enumerable.Repeat("int", arity))}>(error)";
        var first = arity == 1 ? "x" : "t.Item1";
        var firstValue = arity == 1 ? 2 : 1;
        var expected = TupleOf([.. ReceiverValues(arity), (firstValue * 10).ToString(CultureInfo.InvariantCulture)]);
        var delegateParameter = arity == 1 ? "x" : "t";

        return
        [
            Case(
                "keeps_the_value_of_the_receiver_next_to_the_value_of_the_delegate_when_both_succeeded",
                testReceiver,
                [Returns("bind", [delegateParameter], $"Result.Success({first} * 10)", ResultOfInt)],
                [AssertTrueOutcomeIsSuccess, $"Assert.Equal({expected}, outcome.GetValue());", AssertEqual1BindCalls]),
            Case(
                ReturnsTheErrorOfTheDelegateWhenTheDelegate,
                testReceiver,
                [Returns("bind", [delegateParameter], FailureOfInt, ResultOfInt)],
                [AssertSameErrorOutcomeGetError, AssertEqual1BindCalls]),
            Case(
                ReceiverFailed,
                testFailed,
                [Returns("bind", [delegateParameter], $"Result.Success({first} * 10)", ResultOfInt)],
                [AssertSameErrorOutcomeGetError, AssertEqual0BindCalls]),
        ];
    }

    // Map or Bind on a result that holds a tuple, with the elements as separate parameters of the delegate.
    private static OperationSpec SpreadSpec(int arity, bool bind)
    {
        var values = ValueTypes(arity);
        var name = bind ? "bind" : "map";
        var spread = ItemsOf(arity, InputName);

        return new OperationSpec
        {
            Stem = $"{(bind ? "Bind" : "Map")}SpreadArity{arity}FromResultOfT",
            Name = bind ? "Bind" : "Map",
            ReceiverType = $"Result<{TupleOf(values)}>",
            ReceiverTestType = $"Result<{TupleOf(Enumerable.Repeat("int", arity))}>",
            ReceiverDescription = "a <see cref=\"Result{T}\"/> that holds a tuple",
            TypeParameters = [.. values, TResultName],
            TypeParameterDocs = [.. ValueTypeDocs(arity), new(TResultName, SpreadResultDoc(bind))],
            Summary = SpreadSummary(arity, bind),
            Delegates = [new(name, values, bind ? ResultOfTResult : TResultName, SpreadDelegateDoc(bind))],
            ReturnType = ResultOfTResult,
            ReturnDescription = SpreadReturnDescription(bind),
            Body = ctx => SpreadBody(ctx, name, spread, bind),
            ForwardBody = bind ? ctx => SpreadBindForwarded(ctx, name, spread) : null,
            Tests = SpreadTests(arity, bind),
        };
    }

    /// <summary>Gets the documentation of the result type parameter of the spread operations.</summary>
    private static string SpreadResultDoc(bool bind) =>
        bind ? "The type of the value of the result that the delegate returns." : "The type of the value that the delegate produces.";

    /// <summary>Gets the documentation of the delegate of the spread operations.</summary>
    private static string SpreadDelegateDoc(bool bind) => bind
        ? "The operation to run with the values of the successful result."
        : "Computes the new value from the values of the successful result.";

    /// <summary>Gets the documentation of the return value of the spread operations.</summary>
    private static string SpreadReturnDescription(bool bind) => bind
        ? "The result of the delegate, or a failed result with the error of the receiver."
        : "A successful result that holds the value computed by the delegate, or a failed result with the error of the receiver.";

    /// <summary>Gets the summary of the spread operations.</summary>
    private static string SpreadSummary(int arity, bool bind) => bind
        ? $"Chains an operation that can fail and takes the {arity} values of the tuple of the receiver as separate parameters. The delegate runs " +
            "only when the receiver succeeded; a failed result passes through with its error."
        : $"Transforms the {arity} values of the tuple of a successful result, which the delegate takes as separate parameters. A failed result " +
            "passes through unchanged: its error is kept and the delegate is not called.";

    /// <summary>Gets the body of the spread operations.</summary>
    private static string[] SpreadBody(BodyContext ctx, string name, string spread, bool bind) =>
    [
        Unpack(ctx),
        string.Empty,
        IfFailureIsNotNull,
        "{",
        ReturnResultTResultFailedFailure,
        "}",
        string.Empty,
        .. SpreadAct(ctx, name, spread, bind),
    ];

    /// <summary>Gets the generated tests of the spread operations.</summary>
    private static TestCaseSpec[] SpreadTests(int arity, bool bind)
    {
        var name = bind ? "bind" : "map";
        var parameterNames = Enumerable.Range(0, arity).Select(index => $"v{index}").ToArray();
        var sum = string.Join(" + ", parameterNames);
        var delegateTest = bind
            ? Returns(name, parameterNames, $"Result.Success({sum})", ResultOfInt)
            : Returns(name, parameterNames, sum, "int");
        var receiver = $"Result.Success({TupleOf(ReceiverValues(arity))})";

        return
        [
            Case(
                bind ? ReturnsTheResultOfTheDelegateWhenTheReceiver : "runs_the_delegate_with_the_values_and_wraps_its_value_when_the_receiver_succeeded",
                receiver,
                [delegateTest],
                [AssertTrueOutcomeIsSuccess, $"Assert.Equal({Enumerable.Range(1, arity).Sum()}, outcome.GetValue());", $"Assert.Equal(1, {name}Calls);"]),
            Case(
                ReceiverFailed,
                $"Result.Failure<{TupleOf(Enumerable.Repeat("int", arity))}>(error)",
                [delegateTest],
                [AssertSameErrorOutcomeGetError, $"Assert.Equal(0, {name}Calls);"]),
        ];
    }
}
