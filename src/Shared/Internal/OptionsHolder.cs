using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.Internal;

/// <summary>
/// Holds the active settings of a package and changes them atomically. Each package wraps one instance in its public configuration
/// class, so that the rules (readers never see a half-changed state, a failing change changes nothing, a kept reference cannot reach
/// the active settings) are written once.
/// </summary>
/// <typeparam name="TOptions">The settings class.</typeparam>
internal sealed class OptionsHolder<TOptions>
    where TOptions : class
{
    [SuppressMessage("Style", "IDE0330", Justification = "System.Threading.Lock does not exist on the older targets.")]
    private readonly object _gate = new();

    private readonly Func<TOptions> _create;
    private readonly Func<TOptions, TOptions> _clone;
    private TOptions _active;

    /// <summary>Initializes a new instance of the <see cref="OptionsHolder{TOptions}"/> class with default settings.</summary>
    /// <param name="create">Creates a settings object with the default values.</param>
    /// <param name="clone">Creates a copy of a settings object.</param>
    internal OptionsHolder(Func<TOptions> create, Func<TOptions, TOptions> clone)
    {
        _create = create;
        _clone = clone;
        _active = create();
    }

    /// <summary>
    /// Gets the active settings. The instance is never handed to callers outside the package, so it cannot be changed behind the library's
    /// back.
    /// </summary>
    internal TOptions Current => Volatile.Read(ref _active);

    /// <summary>Changes the settings. The action receives a copy of the current settings; they are applied when it returns.</summary>
    /// <param name="configure">Changes the settings it is given. If it throws, nothing is changed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    internal void Configure(Action<TOptions> configure)
    {
        Guard.NotNull(configure, nameof(configure));

        lock (_gate)
        {
            var draft = _clone(Current);
            configure(draft);

            // A second copy: the action may have kept a reference to the draft and could change it later.
            Volatile.Write(ref _active, _clone(draft));
        }
    }

    /// <summary>Restores every setting to its default.</summary>
    internal void Reset()
    {
        lock (_gate)
        {
            Volatile.Write(ref _active, _create());
        }
    }
}
