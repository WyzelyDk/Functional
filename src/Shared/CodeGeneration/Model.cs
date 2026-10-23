namespace Wyzely.Functional.CodeGeneration;

/// <summary>A delegate parameter of an operation, for example <c>Func&lt;T, Maybe&lt;TResult&gt;&gt; bind</c>.</summary>
internal sealed record DelegateSpec(string Name, string[] ParameterTypes, string ReturnType, string Description)
{
    /// <summary>Gets a value indicating whether the delegate returns nothing.</summary>
    internal bool ReturnsVoid => ReturnType == "void";

    /// <summary>
    /// Renders the delegate type for a wrapper: <c>Func&lt;..., X&gt;</c>, <c>Func&lt;..., Task&lt;X&gt;&gt;</c> or <c>Func&lt;...,
    /// ValueTask&lt;X&gt;&gt;</c>.
    /// </summary>
    internal string TypeFor(Wrapper wrapper)
    {
        var parameters = string.Join(", ", ParameterTypes);

        if (ReturnsVoid)
        {
            if (wrapper != Wrapper.None)
            {
                return Func(parameters, wrapper.WrapVoid());
            }

            return ParameterTypes.Length == 0 ? "Action" : $"Action<{parameters}>";
        }

        return Func(parameters, wrapper.Wrap(ReturnType));
    }

    private static string Func(string parameters, string result) =>
        parameters.Length == 0 ? $"Func<{result}>" : $"Func<{parameters}, {result}>";
}

/// <summary>A plain (non-delegate) parameter after the delegates.</summary>
/// <param name="Type">The type of the parameter.</param>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Description">The documentation of the parameter.</param>
/// <param name="Validated">
/// <see langword="false"/> for a parameter that must not be checked for <see langword="null"/>, for example a value of an unconstrained
/// type parameter.
/// </param>
internal sealed record ExtraParameter(string Type, string Name, string Description, bool Validated = true);

/// <summary>The documentation of a type parameter.</summary>
internal sealed record TypeParameterDoc(string Name, string Description);

/// <summary>What the body of an operation can ask while it is being written for one variant.</summary>
internal sealed record BodyContext(Variant Variant, string ReturnType, GeneratorProfile Profile)
{
    /// <summary>Gets the name of the receiver inside the body.</summary>
    internal string R => Variant.Receiver == Wrapper.None ? Profile.ReceiverParameter : "outcome";

    /// <summary>
    /// Renders an already completed task (or value task) that holds a value, for a body that returns the task of the delegate
    /// itself and only needs to create one when it short-circuits. It is the kind of the delegate: <c>Task.FromResult</c> or a
    /// <c>ValueTask</c> constructor call.
    /// </summary>
    internal string Done(string expression) => Variant.Delegate switch
    {
        Wrapper.Task when Profile.CachesCompletedNone && NoneOf(expression) is { } inner => $"MaybeTasks<{inner}>.None",
        Wrapper.Task => $"Task.FromResult({expression})",
        Wrapper.ValueTask => $"new ValueTask<{ReturnType}>({expression})",
        _ => expression,
    };

    // For `default(Maybe<X>)` returns X, so that the cached completed task of none can be used instead of allocating one.
    private static string? NoneOf(string expression) =>
        expression.StartsWith("default(Maybe<", StringComparison.Ordinal) && expression.EndsWith(">)", StringComparison.Ordinal)
            ? expression["default(Maybe<".Length..^">)".Length]
            : null;

    /// <summary>Renders a call of a delegate that returns a value.</summary>
    internal string Call(string delegateName, string arguments) => Variant.Call(delegateName, arguments, Profile.ConfigurationClass);

    /// <summary>Renders a call of a delegate that returns nothing, as a statement.</summary>
    internal string CallStatement(string delegateName, string arguments) => Call(delegateName, arguments) + ";";
}

/// <summary>How one delegate behaves in one generated test.</summary>
internal sealed record DelegateTest(string DelegateName, string[] Parameters, string Statements, string? Return, string ReturnType);

/// <summary>One generated test: what to arrange, what to call, and what to assert, for every variant.</summary>
internal sealed record TestCaseSpec(
    string Name,
    string Receiver,
    DelegateTest[] Delegates,
    string[] ExtraArguments,
    string[] Asserts,
    string[]? Setup = null);

/// <summary>One family of generated methods: an operation on one kind of receiver, in all nine variants.</summary>
internal sealed record OperationSpec
{
    /// <summary>Gets the stem of the generated file names.</summary>
    internal required string Stem { get; init; }

    /// <summary>Gets the name of the extension method.</summary>
    internal required string Name { get; init; }

    /// <summary>Gets the type parameters, in order. <c>T</c> comes first.</summary>
    internal required string[] TypeParameters { get; init; }

    /// <summary>Gets the documentation of the type parameters.</summary>
    internal required TypeParameterDoc[] TypeParameterDocs { get; init; }

    /// <summary>Gets the names of the type parameters that carry a <c>notnull</c> constraint, because they appear as <c>Maybe&lt;X&gt;</c>.</summary>
    internal string[] NotNull { get; init; } = [];

    /// <summary>
    /// Gets the names of the type parameters that carry a <c>struct</c> constraint, for the overload that accepts a delegate returning a
    /// nullable value type.
    /// </summary>
    internal string[] Structs { get; init; } = [];

    /// <summary>Gets the summary of the operation.</summary>
    internal required string Summary { get; init; }

    /// <summary>Gets the delegate parameters, in order.</summary>
    internal DelegateSpec[] Delegates { get; init; } = [];

    /// <summary>Gets the plain parameters that follow the delegates.</summary>
    internal ExtraParameter[] ExtraParameters { get; init; } = [];

    /// <summary>Gets the variants to generate: nine for an operation with a delegate, three (one per receiver) for one without.</summary>
    internal IReadOnlyList<Variant> Variants => Delegates.Length == 0 ? Variant.ReceiverOnly : Variant.All;

    /// <summary>Gets the synchronous return type, or <c>void</c>.</summary>
    internal required string ReturnType { get; init; }

    /// <summary>Gets the documentation of the return value.</summary>
    internal required string ReturnDescription { get; init; }

    /// <summary>Gets the function that writes the body of a variant.</summary>
    internal required Func<BodyContext, string[]> Body { get; init; }

    /// <summary>
    /// Gets the optional body for a synchronous receiver with an asynchronous delegate. It is for operations that return the
    /// result of the delegate as it is: the body hands the task of the delegate back instead of awaiting it, which saves the
    /// state machine and a second task.
    /// </summary>
    internal Func<BodyContext, string[]>? ForwardBody { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation never changes the value of its receiver: it returns the receiver as it was or a
    /// result in another state (a failure, none). On a completed task such an operation hands back the task it received when the state
    /// is the same, instead of allocating a new one.
    /// </summary>
    internal bool KeepsValue { get; init; }

    /// <summary>Gets how an operation that keeps the value can change the state of its receiver. It decides what is generated for a completed task.</summary>
    internal StateChange StateChange { get; init; }

    /// <summary>Gets the test cases that are generated for every variant.</summary>
    internal required TestCaseSpec[] Tests { get; init; }

    /// <summary>Gets the receiver type name in the library, for example <c>Result&lt;T&gt;</c> or <c>Result</c>.</summary>
    internal required string ReceiverType { get; init; }

    /// <summary>Gets the receiver type name in the generated tests, for example <c>Result&lt;int&gt;</c>.</summary>
    internal required string ReceiverTestType { get; init; }

    /// <summary>Gets a value indicating whether the operation returns nothing.</summary>
    internal bool ReturnsVoid => ReturnType == "void";

    /// <summary>Gets a readable description of the receiver for documentation.</summary>
    internal required string ReceiverDescription { get; init; }

    /// <summary>Gets the generic parameter list, for example <c>&lt;T, TResult&gt;</c>.</summary>
    internal string TypeParameterList => TypeParameters.Length == 0 ? string.Empty : "<" + string.Join(", ", TypeParameters) + ">";
}
