using System.Text.RegularExpressions;

namespace Wyzely.Functional.SharedTests;

public sealed class LineWidthTests
{
    private const int MaxWidth = 160;

    private static readonly string[] SourceFolders = ["src", "test", "samples", "benchmarks"];

    private static readonly string[] ToolFolders = [".git", ".vs", ".idea", "node_modules", ".venv", "site", "TestResults"];

    [Fact]
    internal void No_line_of_C_sharp_is_wider_than_the_limit()
    {
        var root = FindRoot();

        var files = SourceFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path));

        AssertNoLineIsTooWide(root, files, line => !IsNameOnly(line));
    }

    [Fact]
    internal void No_line_of_Markdown_is_wider_than_the_limit_except_a_table_row()
    {
        var root = FindRoot();

        AssertNoLineIsTooWide(root, FilesOf(root, "*.md"), line => !IsMarkdownTableRow(line));
    }

    [Fact]
    internal void No_line_of_a_PowerShell_script_is_wider_than_the_limit()
    {
        var root = FindRoot();

        AssertNoLineIsTooWide(root, FilesOf(root, "*.ps1"), _ => true);
    }

    [Fact]
    internal void A_table_row_in_Markdown_may_be_wider_than_the_limit_but_a_line_of_prose_may_not()
    {
        var wide = new string('x', MaxWidth + 1);
        string[] lines = [wide, "| " + wide + " |", "  | " + wide + " |", "short"];

        var tooWide = FindTooWideLines(lines, line => !IsMarkdownTableRow(line)).ToList();

        Assert.Equal([1], tooWide);
    }

    private static IEnumerable<string> FilesOf(string root, string pattern) =>
        Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories).Where(path => !IsToolOutput(path));

    private static void AssertNoLineIsTooWide(string root, IEnumerable<string> files, Func<string, bool> isChecked)
    {
        var tooWide = files
            .SelectMany(path => FindTooWideLines(File.ReadLines(path), isChecked).Select(number => (Path: path, Number: number)))
            .Select(line => $"{Path.GetRelativePath(root, line.Path).Replace('\\', '/')}:{line.Number}")
            .ToList();

        Assert.True(tooWide.Count == 0, $"{tooWide.Count} line(s) are wider than {MaxWidth} characters. First: {string.Join(", ", tooWide.Take(20))}");
    }

    private static IEnumerable<int> FindTooWideLines(IEnumerable<string> lines, Func<string, bool> isChecked) =>
        lines
            .Select((text, index) => (Text: text, Number: index + 1))
            .Where(line => line.Text.Length > MaxWidth && isChecked(line.Text))
            .Select(line => line.Number);

    // A table row cannot be wrapped without changing what the table is, so it may be as wide as it needs to be.
    private static bool IsMarkdownTableRow(string line) => line.TrimStart().StartsWith('|');

    // A method without parameters, such as a generated test, has nothing that can be broken but its name, and a name stays in one piece.
    private static bool IsNameOnly(string line) =>
        Regex.IsMatch(line, @"^\s*(internal|public|private) (async )?(void|Task) \w+\(\)$", RegexOptions.None, TimeSpan.FromSeconds(1));

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');

        return segments.Contains("bin") || segments.Contains("obj");
    }

    private static bool IsToolOutput(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');

        return segments.Contains("bin") || segments.Contains("obj") || segments.Any(segment => ToolFolders.Contains(segment));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Wyzely.Functional.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root (the folder with Wyzely.Functional.slnx) was not found.");
    }
}
