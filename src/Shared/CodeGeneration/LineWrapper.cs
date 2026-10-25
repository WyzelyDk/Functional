using System.Text.RegularExpressions;

namespace Wyzely.Functional.CodeGeneration;

/// <summary>Breaks a line of generated code that is too wide into several lines, at places where a person would break it.</summary>
/// <remarks>
/// Documentation and comments are filled word by word (a documentation tag is never torn apart). Code is broken at the arguments
/// of the last call on the line, before the logical operators of a condition, or inside the braces of a lambda. A line that offers
/// no such place, for example a declaration whose only long part is its name, is left as it is.
/// </remarks>
internal static partial class LineWrapper
{
    private const string Continuation = "    ";

    /// <summary>Wraps a line.</summary>
    /// <param name="text">The line, without its indentation.</param>
    /// <param name="indent">The width of the indentation that the writer puts in front of every line.</param>
    /// <param name="maxWidth">The widest line that is allowed, indentation included.</param>
    /// <param name="docWidth">The width that wrapped documentation and comment text is filled to, indentation included.</param>
    /// <returns>The lines, without the indentation of the writer; continuation lines carry their own relative indentation.</returns>
    internal static IReadOnlyList<string> Wrap(string text, int indent, int maxWidth, int docWidth)
    {
        if (indent + text.Length <= maxWidth)
        {
            return [text];
        }

        var lead = text.Length - text.TrimStart().Length;

        if (lead > 0)
        {
            return [.. Wrap(text[lead..], indent + lead, maxWidth, docWidth).Select(line => text[..lead] + line)];
        }

        if (text.StartsWith("///", StringComparison.Ordinal))
        {
            return WrapDocumentation(text[3..].Trim(), indent, docWidth);
        }

        if (text.StartsWith("//", StringComparison.Ordinal))
        {
            return Fill(text[2..].Trim(), "// ", indent, docWidth);
        }

        return BreakCondition(text)
            ?? BreakArguments(text, indent, maxWidth, docWidth)
            ?? BreakBlockLambda(text, indent, maxWidth, docWidth)
            ?? [text];
    }

    // A word is a run of characters without a space, except that a tag (which may contain spaces) counts as one piece.
    [GeneratedRegex(@"(?:<[^>]*>|[^\s<]|<)+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Word();

    // A documentation element on one line: `<tag attributes>text</tag>`.
    [GeneratedRegex(@"^(?<open><(?<tag>\w+)[^>]*>)(?<text>.*)</(?<close>\w+)>$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Element();

    // A lambda with a block body: `parameters => { statements }` followed by whatever closes the call it is an argument of.
    [GeneratedRegex(@"^(?<head>.*?=>) \{ (?<body>.*) \}(?<tail>[^}]*)$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BlockLambda();

    private static List<string> WrapDocumentation(string body, int indent, int docWidth)
    {
        var match = Element().Match(body);
        var tag = match.Groups["tag"].Value;

        if (!match.Success || tag != match.Groups["close"].Value || match.Groups["text"].Value.Contains("<" + tag, StringComparison.Ordinal))
        {
            return Fill(body, "/// ", indent, docWidth);
        }

        var lines = new List<string> { "/// " + match.Groups["open"].Value };
        lines.AddRange(Fill(match.Groups["text"].Value, "/// ", indent, docWidth));
        lines.Add("/// </" + tag + ">");

        return lines;
    }

    private static List<string> Fill(string text, string prefix, int indent, int width)
    {
        var limit = width - indent;
        var lines = new List<string>();
        var current = new List<string>();
        var length = 0;

        foreach (var word in Word().Matches(text).Select(match => match.Value))
        {
            if (current.Count > 0 && prefix.Length + length + 1 + word.Length > limit)
            {
                lines.Add(prefix + string.Join(' ', current));
                current.Clear();
                length = 0;
            }

            length += (current.Count > 0 ? 1 : 0) + word.Length;
            current.Add(word);
        }

        lines.Add(prefix + string.Join(' ', current));

        return lines;
    }

    // `if (a && b && c)`: one operand per line, the operator first.
    private static List<string>? BreakCondition(string text)
    {
        var group = IsConditionStatement(text) ? FindGroups(text).FirstOrDefault() : default;

        if (group == default)
        {
            return null;
        }

        var cuts = LogicalOperators(text, group.Open + 1, group.Close);

        if (cuts.Count == 0)
        {
            return null;
        }

        var lines = new List<string> { text[..cuts[0]] };

        for (var index = 0; index < cuts.Count; index++)
        {
            var start = cuts[index] + 1;
            var end = index + 1 < cuts.Count ? cuts[index + 1] : text.Length;
            lines.Add(Continuation + text[start..end]);
        }

        return lines;
    }

    private static bool IsConditionStatement(string text) =>
        text.StartsWith("if (", StringComparison.Ordinal)
        || text.StartsWith("else if (", StringComparison.Ordinal)
        || text.StartsWith("while (", StringComparison.Ordinal);

    // `Call(first, second)`: one argument per line, wrapped again when an argument is still too wide.
    private static List<string>? BreakArguments(string text, int indent, int maxWidth, int docWidth)
    {
        var group = FindGroups(text).LastOrDefault(candidate => text[(candidate.Open + 1)..candidate.Close].Trim().Length > 0);

        if (group == default)
        {
            return null;
        }

        var arguments = SplitArguments(text, group.Open + 1, group.Close);
        var lines = new List<string> { text[..(group.Open + 1)] };

        for (var index = 0; index < arguments.Count; index++)
        {
            var last = index == arguments.Count - 1;
            var line = arguments[index].Trim() + (last ? text[group.Close..] : ",");

            foreach (var piece in Wrap(line, indent + Continuation.Length, maxWidth, docWidth))
            {
                lines.Add(Continuation + piece);
            }
        }

        return lines;
    }

    // `() => { statement; }`: the braces get their own lines, and the statement is wrapped inside them.
    private static List<string>? BreakBlockLambda(string text, int indent, int maxWidth, int docWidth)
    {
        var match = BlockLambda().Match(text);

        if (!match.Success)
        {
            return null;
        }

        var lines = new List<string> { match.Groups["head"].Value, "{" };

        foreach (var piece in Wrap(match.Groups["body"].Value, indent + Continuation.Length, maxWidth, docWidth))
        {
            lines.Add(Continuation + piece);
        }

        lines.Add("}" + match.Groups["tail"].Value);

        return lines;
    }

    // The round brackets that are not inside any other bracket.
    private static List<(int Open, int Close)> FindGroups(string text)
    {
        var groups = new List<(int Open, int Close)>();
        var depth = 0;
        var open = -1;

        foreach (var (index, character) in Significant(text, 0, text.Length))
        {
            var delta = BracketDelta(character);

            if (delta > 0)
            {
                open = IsOutermostOpeningParenthesis(character, depth) ? index : open;
            }
            else if (delta < 0 && depth == 1 && character == ')' && open >= 0)
            {
                groups.Add((open, index));
                open = -1;
            }

            depth = Math.Max(0, depth + delta);
        }

        return groups;
    }

    private static bool IsOutermostOpeningParenthesis(char character, int depth) => character == '(' && depth == 0;

    // 1 for an opening bracket, -1 for a closing bracket, 0 for any other character.
    private static int BracketDelta(char character) => character switch
    {
        '(' or '[' or '{' => 1,
        ')' or ']' or '}' => -1,
        _ => 0,
    };

    // The positions of the spaces in front of `&&` and `||` that belong to the outermost level of the text between the brackets.
    private static List<int> LogicalOperators(string text, int start, int end)
    {
        var cuts = new List<int>();
        var depth = 0;

        foreach (var (index, character) in Significant(text, start, end))
        {
            var delta = BracketDelta(character);
            depth += delta;

            if (delta == 0 && IsOperatorCut(text, index, character, depth, end))
            {
                cuts.Add(index);
            }
        }

        return cuts;
    }

    private static bool IsOperatorCut(string text, int index, char character, int depth, int end) =>
        character == ' ' && depth == 0 && index + 3 < end && IsLogicalOperator(text.AsSpan(index + 1, 3));

    private static bool IsLogicalOperator(ReadOnlySpan<char> text) => text is "&& " or "|| ";

    // Splits at the commas of the outermost level, not at those inside brackets, strings or the type arguments of a generic type.
    private static List<string> SplitArguments(string text, int start, int end)
    {
        var arguments = new List<string>();
        var depth = 0;
        var angle = 0;
        var from = start;

        foreach (var (index, character) in Significant(text, start, end))
        {
            var delta = BracketDelta(character);
            depth += delta;

            if (delta != 0)
            {
                continue;
            }

            angle += AngleDelta(text, index, end, angle);

            if (character == ',' && depth == 0 && angle == 0)
            {
                arguments.Add(text[from..index]);
                from = index + 1;
            }
        }

        arguments.Add(text[from..end]);

        return arguments;
    }

    // 1 for the `<` that opens the type arguments of a generic type, -1 for the `>` that closes them (not the `>` of `=>`), 0 otherwise.
    private static int AngleDelta(string text, int index, int end, int angle) => text[index] switch
    {
        '<' when IsTypeArgumentStart(text, index, end) => 1,
        '>' when angle > 0 && text[index - 1] != '=' => -1,
        _ => 0,
    };

    private static bool IsTypeArgumentStart(string text, int index, int end) =>
        index > 0
        && index + 1 < end
        && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_')
        && (char.IsLetter(text[index + 1]) || text[index + 1] == '(');

    // The characters outside string and character literals.
    private static IEnumerable<(int Index, char Character)> Significant(string text, int start, int end)
    {
        var index = start;

        while (index < end)
        {
            var character = text[index];

            if (character == '"')
            {
                index = EndOfLiteral(text, index, '"');
            }
            else if (character == '\'')
            {
                index = EndOfLiteral(text, index, '\'');
            }
            else
            {
                yield return (index, character);
            }

            index++;
        }
    }

    private static int EndOfLiteral(string text, int start, char quote)
    {
        var index = start + 1;

        while (index < text.Length && text[index] != quote)
        {
            index += text[index] == '\\' ? 2 : 1;
        }

        return Math.Min(index, text.Length - 1);
    }
}
