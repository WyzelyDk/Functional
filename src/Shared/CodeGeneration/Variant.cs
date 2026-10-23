namespace Wyzely.Functional.CodeGeneration;

/// <summary>
/// One combination of a receiver wrapper and delegate wrappers. <see cref="Delegate"/> is the kind of every delegate of an operation;
/// <see cref="Second"/> is the kind of the delegates that are marked independent, for an operation whose two delegates are async on their own.
/// </summary>
internal readonly record struct Variant(Wrapper Receiver, Wrapper Delegate, Wrapper Second = Wrapper.None)
{
    /// <summary>Gets all nine combinations of a receiver and one kind of delegate.</summary>
    internal static IReadOnlyList<Variant> All { get; } = CreateAll(independent: false);

    /// <summary>Gets all 27 combinations of a receiver, a kind of delegate and a second, independent kind of delegate.</summary>
    internal static IReadOnlyList<Variant> AllWithSecond { get; } = CreateAll(independent: true);

    /// <summary>
    /// Gets the three combinations of an operation without a delegate: only the receiver can be wrapped, because a delegate that does not
    /// exist has no kind.
    /// </summary>
    internal static IReadOnlyList<Variant> ReceiverOnly { get; } = [.. CreateAll(independent: false).Where(variant => variant.Delegate == Wrapper.None)];

    /// <summary>Gets a value indicating whether neither the receiver nor a delegate is asynchronous.</summary>
    internal bool IsSync => Receiver == Wrapper.None && !HasAsyncDelegate;

    /// <summary>Gets a value indicating whether at least one delegate is asynchronous.</summary>
    internal bool HasAsyncDelegate => Delegate != Wrapper.None || Second != Wrapper.None;

    /// <summary>Gets the wrapper of the return value: the one of the receiver, else the one of the first asynchronous delegate.</summary>
    internal Wrapper Result => (Receiver, Delegate) switch
    {
        (not Wrapper.None, _) => Receiver,
        (_, not Wrapper.None) => Delegate,
        _ => Second,
    };

    /// <summary>Gets the wrapper of a delegate: the second kind for an independent delegate, else the first.</summary>
    /// <param name="delegateSpec">The delegate.</param>
    internal Wrapper WrapperOf(DelegateSpec delegateSpec) => delegateSpec.Independent ? Second : Delegate;

    /// <summary>Renders the call of a delegate, awaited when the delegate is asynchronous.</summary>
    /// <param name="delegateName">The name of the delegate parameter.</param>
    /// <param name="arguments">The arguments of the call.</param>
    /// <param name="configurationClass">The configuration class whose <c>ContinueOnCapturedContext</c> setting the await follows.</param>
    internal string Call(string delegateName, string arguments, string configurationClass) => Render(Delegate, delegateName, arguments, configurationClass);

    /// <summary>Renders the call of an independent delegate, awaited when that delegate is asynchronous.</summary>
    /// <param name="delegateName">The name of the delegate parameter.</param>
    /// <param name="arguments">The arguments of the call.</param>
    /// <param name="configurationClass">The configuration class whose <c>ContinueOnCapturedContext</c> setting the await follows.</param>
    internal string CallSecond(string delegateName, string arguments, string configurationClass) => Render(Second, delegateName, arguments, configurationClass);

    private static string Render(Wrapper wrapper, string delegateName, string arguments, string configurationClass) => wrapper == Wrapper.None
        ? $"{delegateName}({arguments})"
        : $"await {delegateName}({arguments}).ConfigureAwait({configurationClass}.ContinueOnCapturedContext)";

    private static Variant[] CreateAll(bool independent)
    {
        var wrappers = new[] { Wrapper.None, Wrapper.Task, Wrapper.ValueTask };
        var seconds = independent ? wrappers : wrappers[..1]; // only Wrapper.None, because without an independent delegate there is no second kind
        var variants = new List<Variant>();

        foreach (var receiver in wrappers)
        {
            foreach (var @delegate in wrappers)
            {
                foreach (var second in seconds)
                {
                    variants.Add(new Variant(receiver, @delegate, second));
                }
            }
        }

        return [.. variants];
    }
}
