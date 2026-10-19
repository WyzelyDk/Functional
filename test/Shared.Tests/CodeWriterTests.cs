using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.SharedTests;

public sealed class CodeWriterTests
{
    [Fact]
    internal void Line_wraps_a_line_that_is_too_wide_only_with_its_indentation_counted()
    {
        var call = "Call(" + string.Join(", ", Enumerable.Range(0, 22).Select(index => $"arg{index:00}")) + ");";
        var flat = new CodeWriter();
        flat.Line(call);
        var indented = new CodeWriter();
        indented.Open();
        indented.Line(call);

        var flatLines = flat.ToString().TrimEnd('\n').Split('\n');
        var indentedLines = indented.ToString().TrimEnd('\n').Split('\n');

        Assert.Multiple(
            () => Assert.True(call.Length <= GeneratorSettings.MaxLineWidth && call.Length + 4 > GeneratorSettings.MaxLineWidth),
            () => Assert.Single(flatLines),
            () => Assert.True(indentedLines.Length > 3),
            () => Assert.Equal("    Call(", indentedLines[1]),
            () => Assert.Equal("        arg00,", indentedLines[2]),
            () => Assert.All(indentedLines, line => Assert.True(line.Length <= GeneratorSettings.MaxLineWidth)));
    }
}
