using System.Text;

namespace Wyzely.Functional;

/// <content>
/// The text of an error: rendering the message template with its details, and the full description with the context frames.
/// </content>
public sealed partial class Error
{
    /// <summary>Returns the message prefixed with the context frames, outermost first, for example <c>Import: Checkout: Upload failed.</c>.</summary>
    /// <returns>The full description of the error, including sensitive details.</returns>
    public override string ToString() => Describe(redact: false);

    /// <summary>Returns the same text as <see cref="ToString"/> but with sensitive details replaced by <see cref="ResultOptions.RedactedValue"/>.</summary>
    /// <returns>The full description of the error that is safe to log.</returns>
    public string ToRedactedString() => Describe(redact: true);

    /// <summary>Renders a message template with its details.</summary>
    /// <param name="template">The template, whose <c>{Name}</c> placeholders are replaced.</param>
    /// <param name="details">The details whose names the placeholders refer to.</param>
    /// <param name="redact"><see langword="true"/> to replace the value of a sensitive detail.</param>
    /// <returns>The rendered text.</returns>
    internal static string Format(string template, IReadOnlyList<ErrorDetail> details, bool redact)
    {
        if (template.IndexOf('{') < 0 && template.IndexOf('}') < 0)
        {
            return template;
        }

        var builder = new StringBuilder(template.Length + 16);
        var position = 0;

        while (position < template.Length)
        {
            position = AppendNext(builder, template, position, details, redact);
        }

        return builder.ToString();
    }

    private static int AppendNext(StringBuilder builder, string template, int position, IReadOnlyList<ErrorDetail> details, bool redact)
    {
        var current = template[position];

        if (IsEscapedBrace(template, position))
        {
            builder.Append(current);

            return position + 2;
        }

        if (current == '{')
        {
            return AppendPlaceholder(builder, template, position, details, redact);
        }

        builder.Append(current);

        return position + 1;
    }

    private static bool IsEscapedBrace(string template, int position) =>
        (template[position] == '{' || template[position] == '}')
        && position + 1 < template.Length
        && template[position + 1] == template[position];

    private static int AppendPlaceholder(StringBuilder builder, string template, int start, IReadOnlyList<ErrorDetail> details, bool redact)
    {
        var end = template.IndexOf('}', start + 1);

        if (end < 0)
        {
            builder.Append(template, start, template.Length - start);

            return template.Length;
        }

        var name = template.Substring(start + 1, end - start - 1);
        var found = false;

        for (var index = 0; index < details.Count && !found; index++)
        {
            if (string.Equals(details[index].Name, name, StringComparison.Ordinal))
            {
                builder.Append(details[index].Render(redact));
                found = true;
            }
        }

        if (!found)
        {
            builder.Append(template, start, end - start + 1);
        }

        return end + 1;
    }

    private string BuildMessage(bool redact)
    {
        if (!IsAggregate)
        {
            return Format(Template, Details, redact);
        }

        return string.Join(ResultConfiguration.Current.ChildSeparator, Children.Select(child => child.Describe(redact)));
    }

    private string Describe(bool redact)
    {
        var message = redact ? RedactedMessage : Message;
        var separator = ResultConfiguration.Current.ContextSeparator;

        if (Context.Count == 0)
        {
            return message;
        }

        var frames = Context.Select(frame => redact ? frame.RedactedMessage : frame.Message);

        return string.Join(separator, frames) + separator + message;
    }
}
