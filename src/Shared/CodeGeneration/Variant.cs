namespace Wyzely.Functional.CodeGeneration;

/// <summary>One combination of a receiver wrapper and a delegate wrapper.</summary>
internal readonly record struct Variant(Wrapper Receiver, Wrapper Delegate)
{
    /// <summary>Gets all nine combinations.</summary>
    internal static IReadOnlyList<Variant> All { get; } = CreateAll();

    /// <summary>
    /// Gets the three combinations of an operation without a delegate: only the receiver can be wrapped, because a delegate that does not
    /// exist has no kind.
    /// </summary>
    internal static IReadOnlyList<Variant> ReceiverOnly { get; } = [.. CreateAll().Where(variant => variant.Delegate == Wrapper.None)];

    /// <summary>Gets a value indicating whether neither the receiver nor the delegate is asynchronous.</summary>
    internal bool IsSync => Receiver == Wrapper.None && Delegate == Wrapper.None;

    /// <summary>Gets the wrapper of the return value: the one of the receiver, else the one of the delegate.</summary>
    internal Wrapper Result => Receiver != Wrapper.None ? Receiver : Delegate;

    /// <summary>Renders the call of a delegate, awaited when the delegate is asynchronous.</summary>
    /// <param name="delegateName">The name of the delegate parameter.</param>
    /// <param name="arguments">The arguments of the call.</param>
    /// <param name="configurationClass">The configuration class whose <c>ContinueOnCapturedContext</c> setting the await follows.</param>
    internal string Call(string delegateName, string arguments, string configurationClass) => Delegate == Wrapper.None
        ? $"{delegateName}({arguments})"
        : $"await {delegateName}({arguments}).ConfigureAwait({configurationClass}.ContinueOnCapturedContext)";

    private static Variant[] CreateAll()
    {
        var wrappers = new[] { Wrapper.None, Wrapper.Task, Wrapper.ValueTask };
        var variants = new List<Variant>();

        foreach (var receiver in wrappers)
        {
            foreach (var @delegate in wrappers)
            {
                variants.Add(new Variant(receiver, @delegate));
            }
        }

        return [.. variants];
    }
}
