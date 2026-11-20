using System.Reflection;
using System.Text.RegularExpressions;

namespace Wyzely.Functional.ApiSurface;

// Compares the public API of an assembly with the approved one, which is a text file for each assembly and target framework in test/ApiSurface/Approved.
// A change of the public API is therefore a change of that file, and it shows in the review of the pull request. When the change is intended, run the
// tests once with the environment variable UPDATE_PUBLIC_API set to 1: the approved files are rewritten, and the test fails once to say so.
internal static partial class PublicApiApproval
{
    private const string UpdateVariable = "UPDATE_PUBLIC_API";
    private const int ShownLines = 40;

    internal static void Check(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        var framework = FrameworkMoniker();
        var path = Path.Combine(RepositoryRoot(), "test", "ApiSurface", "Approved", $"{name}.{framework}.txt");
        var actual = PublicApiDump.Of(assembly);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, actual);
            Assert.Fail($"The approved public API was written to {path}. Review its diff, and run the tests again without {UpdateVariable}.");
        }

        if (!File.Exists(path))
        {
            Assert.Fail($"There is no approved public API for {name} on {framework} ({path}). Run the tests once with {UpdateVariable}=1 to write it.");
        }

        var approved = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);

        if (approved != actual)
        {
            Assert.Fail(Difference(name, framework, approved, actual));
        }
    }

    private static string Difference(string? name, string framework, string approved, string actual)
    {
        var approvedLines = approved.Split('\n').ToHashSet(StringComparer.Ordinal);
        var actualLines = actual.Split('\n').ToHashSet(StringComparer.Ordinal);
        var removed = approvedLines.Where(line => !actualLines.Contains(line)).Order(StringComparer.Ordinal).Select(static line => "- " + line);
        var added = actualLines.Where(line => !approvedLines.Contains(line)).Order(StringComparer.Ordinal).Select(static line => "+ " + line);
        var changes = removed.Concat(added).ToList();
        var shown = string.Join('\n', changes.Take(ShownLines));
        var more = changes.Count > ShownLines ? $"\n... and {changes.Count - ShownLines} more lines" : string.Empty;

        var header = $"The public API of {name} on {framework} is not the approved one ({changes.Count} lines differ; '-' is approved, '+' is now):";

        return $"{header}\n{shown}{more}\n" +
            $"If the change is intended, run the tests once with {UpdateVariable}=1, review the diff of test/ApiSurface/Approved, and commit it.";
    }

    // net10.0, from the name of the target framework that the test runs on.
    private static string FrameworkMoniker()
    {
        var match = FrameworkVersion().Match(AppContext.TargetFrameworkName ?? string.Empty);

        return match.Success ? $"net{match.Groups[1].Value}.{match.Groups[2].Value}" : throw new InvalidOperationException("The target framework is not .NET.");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The root of the repository was not found.");
    }

    [GeneratedRegex(@"Version=v(\d+)\.(\d+)")]
    private static partial Regex FrameworkVersion();
}
