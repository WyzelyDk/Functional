namespace Wyzely.Functional.SharedTests;

// The rule of AGENTS.md: no call, object creation with arguments or await is the argument of another call. It holds for the libraries, for what the
// generators emit (the Generated folders are part of the library folders) and for the samples. The tests, the benchmarks and the tooling (the generator
// catalogs, the Roslyn code fixes, the analyzers and the generator engine) are exempt, because a builder style such as SyntaxFactory or the catalog's
// Returns(...) nests by design.
public sealed class NestedCallRuleTests
{
    private static readonly string[] CheckedFolders =
    [
        "src/Result",
        "src/Maybe",
        "src/Functional",
        "src/Shared/Internal",
        "samples",
    ];

    [Fact]
    internal void No_call_in_the_libraries_the_generated_code_and_the_samples_is_the_argument_of_another_call()
    {
        var root = RepositoryRoot.Find();

        var violations = CheckedFolders
            .SelectMany(folder => RepositoryRoot.CSharpFilesOf(root, folder))
            .SelectMany(path => NestedCallFinder.Find(File.ReadAllText(path)).Select(call => Describe(root, path, call)))
            .ToList();

        var message = $"{violations.Count} call(s) are the argument of another call; put the inner call in a local first:{Environment.NewLine}"
            + string.Join(Environment.NewLine, violations.Take(30));

        Assert.True(violations.Count == 0, message);
    }

    [Fact]
    internal void The_checked_folders_hold_the_code_that_the_rule_is_about()
    {
        var root = RepositoryRoot.Find();

        var files = CheckedFolders
            .SelectMany(folder => RepositoryRoot.CSharpFilesOf(root, folder))
            .Select(path => RepositoryRoot.Relative(root, path))
            .ToList();

        Assert.Multiple(
            () => Assert.Contains(files, static path => path.StartsWith("src/Result/Generated/", StringComparison.Ordinal)),
            () => Assert.Contains(files, static path => path.StartsWith("src/Maybe/Generated/", StringComparison.Ordinal)),
            () => Assert.Contains(files, static path => path.StartsWith("src/Functional/", StringComparison.Ordinal)),
            () => Assert.Contains(files, static path => path.StartsWith("samples/Aot.Samples/", StringComparison.Ordinal)),
            () => Assert.DoesNotContain(files, static path => RepositoryRoot.IsBuildOutput(path)));
    }

    private static string Describe(string root, string path, NestedCallFinder.NestedCall call)
    {
        var text = call.Text.Length <= 110 ? call.Text : call.Text[..110] + "...";

        return $"{RepositoryRoot.Relative(root, path)}:{call.Line}  {text}";
    }
}
