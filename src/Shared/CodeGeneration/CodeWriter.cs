using System.Text;

namespace Wyzely.Functional.CodeGeneration;

/// <summary>A tiny indenting text writer. It always writes <c>\n</c>; the generator converts the line endings when it saves.</summary>
internal sealed class CodeWriter
{
    private const string IndentUnit = "    ";

    private readonly StringBuilder _builder = new();
    private int _depth;

    /// <summary>
    /// Writes one line at the current indentation. An empty text writes an empty line. A line that would be wider than
    /// <see cref="GeneratorSettings.MaxLineWidth"/> is wrapped (see <see cref="LineWrapper"/>).
    /// </summary>
    internal void Line(string text = "")
    {
        if (text.Length == 0)
        {
            _builder.Append('\n');

            return;
        }

        var indentation = _depth * IndentUnit.Length;

        foreach (var line in LineWrapper.Wrap(text, indentation, GeneratorSettings.MaxLineWidth, GeneratorSettings.PreferredDocWidth))
        {
            _builder.Append(' ', indentation).Append(line).Append('\n');
        }
    }

    /// <summary>Writes several lines.</summary>
    internal void Lines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            Line(line);
        }
    }

    /// <summary>Writes an opening brace and indents.</summary>
    internal void Open()
    {
        Line("{");
        _depth++;
    }

    /// <summary>Unindents and writes a closing brace.</summary>
    internal void Close()
    {
        _depth--;
        Line("}");
    }

    /// <summary>Indents by one level.</summary>
    internal void Indent() => _depth++;

    /// <summary>Unindents by one level.</summary>
    internal void Unindent() => _depth--;

    /// <inheritdoc />
    public override string ToString() => _builder.ToString();
}
