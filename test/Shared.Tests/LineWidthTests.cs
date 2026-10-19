using System.Text.RegularExpressions;

namespace Wyzely.Functional.SharedTests;

public sealed class LineWidthTests
{
    private const int MaxWidth = 160;

    private static readonly string[] SourceFolders = ["src", "test", "samples", "benchmarks"];

    [Fact]
    internal void No_line_of_C_sharp_is_wider_than_the_limit()
    {
        var root = FindRoot();

        var tooWide = SourceFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (Path: Path.GetRelativePath(root, path), Number: index + 1, Text: line)))
            .Where(line => line.Text.Length > MaxWidth && !IsNameOnly(line.Text))
            .Select(line => $"{line.Path.Replace('\\', '/')}:{line.Number} ({line.Text.Length})")
            .ToList();

        Assert.True(tooWide.Count == 0, $"{tooWide.Count} line(s) are wider than {MaxWidth} characters. First: {string.Join(", ", tooWide.Take(20))}");
    }

    // A method without parameters, such as a generated test, has nothing that can be broken but its name, and a name stays in one piece.
    private static bool IsNameOnly(string line) =>
        Regex.IsMatch(line, @"^\s*(internal|public|private) (async )?(void|Task) \w+\(\)$", RegexOptions.None, TimeSpan.FromSeconds(1));

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');

        return segments.Contains("bin") || segments.Contains("obj");
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
