using System.Text;

namespace Wyzely.Functional;

/// <content>
/// The text of an error: rendering the message template with its details, and the full description with the context frames.
/// </content>
public sealed partial class Error
{
    /// <summary>Returns the message prefixed with the context frames, outermost first, for example <c>Import: Meny: Upload failed.</c>.</summary>
    /// <returns>The full description of the error, including sensitive details.</returns>
    public override string ToString() => Describe(redact: false);

    /// <summary>Returns the same text as <see cref="ToString"/> but with sensitive details replaced by <see cref="ResultOptions.RedactedValue"/>.</summary>
    /// <returns>The full description of the error that is safe to log.</returns>
    public string ToRedactedString() => Describe(redact: true);

    private static string Format(string template, IReadOnlyList<ErrorDetail> details, bool redact)
    {
        if (template.IndexOf('{') < 0 && template.IndexOf('}') < 0)
        {
            return template;
        }

        var builder = new StringBuilder(template.Length + 16);
        var position = 0;

        while (position < template.Length)
        {
            var current = template[position];

            if (current == '{' && position + 1 < template.Length && template[position + 1] == '{')
            {
                builder.Append('{');
                position += 2;
            }
            else if (current == '}' && position + 1 < template.Length && template[position + 1] == '}')
            {
                builder.Append('}');
                position += 2;
            }
            else if (current == '{')
            {
                position = AppendPlaceholder(builder, template, position, details, redact);
            }
            else
            {
                builder.Append(current);
                position++;
            }
        }

        return builder.ToString();
    }

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

        return Context.Count == 0 ? message : string.Join(separator, Context) + separator + message;
    }
}
