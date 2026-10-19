using System.Globalization;

namespace Wyzely.Functional;

/// <content>
/// The text of a maybe, used by the debugger and by logging.
/// </content>
public readonly partial struct Maybe<T>
{
    /// <summary>
    /// Describes the maybe as <c>Some(value)</c> or <c>None</c>. The text can be changed with <see cref="MaybeOptions.SomeFormat"/>
    /// and <see cref="MaybeOptions.NoneText"/>, and the value can be hidden with <see cref="MaybeOptions.ShowValueInToString"/>.
    /// </summary>
    /// <returns>A short description of the maybe. It never throws.</returns>
    public override string ToString()
    {
        var options = MaybeConfiguration.Current;

        if (!IsSome)
        {
            return options.NoneText;
        }

        var text = options.ShowValueInToString ? _value.ToString() ?? string.Empty : options.RedactedValue;

        return string.Format(CultureInfo.InvariantCulture, options.SomeFormat, text);
    }
}
