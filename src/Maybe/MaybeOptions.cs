using System.Globalization;

namespace Wyzely.Functional;

/// <summary>
/// The settings of the library that a consumer may want to change. Every setting has a default that is sensible for most
/// applications, so nothing has to be configured; change a setting once, at application start, with
/// <see cref="MaybeConfiguration.Configure(Action{MaybeOptions})"/>.
/// </summary>
/// <remarks>
/// The settings are read each time a maybe is shown or read, so they take effect at once; configure them at start-up so that
/// the output of the whole process is consistent.
/// </remarks>
public sealed class MaybeOptions
{
    /// <summary>
    /// Gets or sets the format of <see cref="Maybe{T}.ToString"/> for a maybe that has a value, where <c>{0}</c> is the value (or the
    /// redacted text). The default is <c>Some({0})</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The value is empty, white space, has no <c>{0}</c> placeholder, or is not a valid format string.</exception>
    public string SomeFormat
    {
        get;
        set => field = ValidateFormat(value);
    } = "Some({0})";

    /// <summary>Gets or sets the text of <see cref="Maybe{T}.ToString"/> for a maybe that has no value. The default is <c>None</c>.</summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public string NoneText
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = "None";

    /// <summary>
    /// Gets or sets the message of the exception that <see cref="Maybe{T}.GetValue"/> throws for a maybe that has no value. The default is
    /// <c>The maybe has no value. Check IsSome first, or use TryGetValue.</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The value is empty or white space.</exception>
    public string NoValueMessage
    {
        get;
        set => field = Guard.NotNullOrWhiteSpace(value, nameof(value));
    } = "The maybe has no value. Check IsSome first, or use TryGetValue.";

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Maybe{T}.ToString"/> shows the value. Set it to <see langword="false"/> when
    /// maybes may hold sensitive data that must not reach logs; the text of <see cref="RedactedValue"/> is shown instead. The default
    /// is <see langword="true"/>.
    /// </summary>
    public bool ShowValueInToString { get; set; } = true;

    /// <summary>
    /// Gets or sets the text that replaces the value in <see cref="Maybe{T}.ToString"/> when <see cref="ShowValueInToString"/> is
    /// <see langword="false"/>. The default is <c>***</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public string RedactedValue
    {
        get;
        set => field = Guard.NotNull(value, nameof(value));
    } = "***";

    /// <summary>
    /// Gets or sets a value indicating whether the asynchronous methods of the library continue on the captured synchronization context after an
    /// <see langword="await"/>. The default is <see langword="false"/>, which is right for a library and avoids deadlocks; set it to
    /// <see langword="true"/> only in an application that depends on the context (for example a UI thread) inside the
    /// delegates that it passes in.
    /// </summary>
    public bool ContinueOnCapturedContext { get; set; }

    /// <summary>Creates a copy, so that the active settings can never be changed through an instance that a caller still holds.</summary>
    /// <returns>A new instance with the same values.</returns>
    internal MaybeOptions Clone() => (MaybeOptions)MemberwiseClone();

    private static string ValidateFormat(string? value)
    {
        var format = Guard.NotNullOrWhiteSpace(value, nameof(value));

        if (!format.Contains("{0}"))
        {
            throw new ArgumentException("The format must contain the placeholder {0} for the value.", nameof(value));
        }

        try
        {
            _ = string.Format(CultureInfo.InvariantCulture, format, "x");
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The format is not a valid format string: " + exception.Message, nameof(value), exception);
        }

        return format;
    }
}
