namespace Wyzely.Functional.SharedTests;

// The rule of AGENTS.md about lambdas: no block body (a private method or a local function instead), and a body with two or more calls is a chain
// with a line per dot or it is named. It holds for the libraries, for the generated code (the Generated folders are part of the library folders),
// for the samples and for the tooling: the generator catalogs and engine, the analyzers and the code fixes. The tests and the benchmarks are exempt.
public sealed class NamedLambdaRuleTests
{
    private static readonly string[] CheckedFolders =
    [
        "src/Result",
        "src/Maybe",
        "src/Functional",
        "src/Shared/Internal",
        "src/Shared/CodeGeneration",
        "src/Shared/Analyzers",
        "src/Result.CodeGeneration",
        "src/Maybe.CodeGeneration",
        "src/Result.Analyzers",
        "src/Maybe.Analyzers",
        "src/Result.Analyzers.CodeFixes",
        "src/Maybe.Analyzers.CodeFixes",
        "samples",
    ];

    [Fact]
    internal void No_lambda_in_the_libraries_the_tooling_and_the_samples_has_a_block_body_or_a_chain_on_one_line()
    {
        var root = RepositoryRoot.Find();

        var violations = CheckedFolders
            .SelectMany(folder => RepositoryRoot.CSharpFilesOf(root, folder))
            .SelectMany(path => NamedLambdaFinder.Find(File.ReadAllText(path)).Select(problem => Describe(root, path, problem)))
            .ToList();

        var message = $"{violations.Count} lambda(s) break the rule:{Environment.NewLine}" + string.Join(Environment.NewLine, violations.Take(40));

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
            () => Assert.Contains(files, static path => path.StartsWith("src/Result.CodeGeneration/", StringComparison.Ordinal)),
            () => Assert.Contains(files, static path => path.StartsWith("src/Maybe.Analyzers.CodeFixes/", StringComparison.Ordinal)),
            () => Assert.Contains(files, static path => path.StartsWith("samples/Aot.Samples/", StringComparison.Ordinal)),
            () => Assert.DoesNotContain(files, static path => RepositoryRoot.IsBuildOutput(path)));
    }

    private static string Describe(string root, string path, NamedLambdaFinder.LambdaProblem problem)
    {
        var text = problem.Text.Length <= 90 ? problem.Text : problem.Text[..90] + "...";

        return $"{RepositoryRoot.Relative(root, path)}:{problem.Line}  {problem.Reason}: {text}";
    }
}
