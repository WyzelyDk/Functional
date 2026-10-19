namespace Wyzely.Functional;

/// <summary>
/// Changes the settings of the library for the whole process. The defaults suit most applications; call
/// <see cref="Configure(Action{MaybeOptions})"/> once at start-up if you want something different.
/// </summary>
/// <remarks>
/// The settings are process-wide because maybes are created everywhere, and passing an options object to every call
/// would defeat the purpose of a lightweight type. A change is applied atomically: readers see either the old or the
/// new settings, never a mixture.
/// </remarks>
public static class MaybeConfiguration
{
    private static readonly OptionsHolder<MaybeOptions> Holder = new(static () => new MaybeOptions(), static options => options.Clone());

    /// <summary>Gets the active settings. The instance is never exposed, so it cannot be changed behind the library's back.</summary>
    internal static MaybeOptions Current => Holder.Current;

    /// <summary>Gets a value indicating whether the awaits of the library continue on the captured context; it is passed to <c>ConfigureAwait</c>.</summary>
    internal static bool ContinueOnCapturedContext => Current.ContinueOnCapturedContext;

    /// <summary>Changes the settings. The action receives a copy of the current settings; they are applied when it returns.</summary>
    /// <param name="configure">Changes the settings it is given. If it throws, nothing is changed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static void Configure(Action<MaybeOptions> configure) => Holder.Configure(configure);

    /// <summary>Restores every setting to its default. It is mainly useful in tests that change the configuration.</summary>
    public static void Reset() => Holder.Reset();
}
