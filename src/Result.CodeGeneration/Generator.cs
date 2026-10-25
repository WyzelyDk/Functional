using Wyzely.Functional.CodeGeneration;

namespace Wyzely.Functional.ResultCodeGeneration;

/// <summary>Generates the files of the Result operation catalog and saves them in the repository.</summary>
internal static class Generator
{
    internal const string LibraryDirectory = "src/Result/Generated";
    internal const string TestDirectory = "test/Result.Tests/Generated";
    internal const string SolutionFileName = GeneratorEngine.SolutionFileName;

    private static readonly GeneratorEngine Engine = new(
        new GeneratorProfile(
            ToolNamespace: "Wyzely.Functional.Result.CodeGeneration",
            ToolProject: "src/Result.CodeGeneration",
            ExtensionsClass: "ResultExtensions",
            ConfigurationClass: "ResultConfiguration",
            OptionsClass: "ResultOptions",
            ReceiverParameter: "result",
            LibraryDirectory: LibraryDirectory,
            TestDirectory: TestDirectory,
            TestNamespace: "Wyzely.Functional.ResultTests",
            TestPreamble: ["""var error = Error.Create("Boom.");"""],
            Zip: ZipKind.Result,
            CachesCompletedSuccess: true,
            ConditionalFactories: true),
        Operations.All);

    /// <summary>Generates every file. The keys are paths relative to the repository root, with forward slashes.</summary>
    /// <param name="stubs"><see langword="true"/> to emit method bodies that do nothing useful (only for the red phase of test-driven work).</param>
    internal static IReadOnlyDictionary<string, string> Generate(bool stubs) => Engine.Generate(stubs);

    /// <summary>Replaces the generated files in the repository with the given ones.</summary>
    internal static void Write(string root, IReadOnlyDictionary<string, string> files) => Engine.Write(root, files);

    /// <summary>Finds the repository root: the closest directory, starting here and going up, that contains the solution file.</summary>
    internal static string? FindRoot(string start) => GeneratorEngine.FindRoot(start);

    /// <summary>Runs the tool. See <see cref="GeneratorEngine.RunAsync"/>.</summary>
    /// <param name="args">The arguments of the command line.</param>
    /// <param name="currentDirectory">The directory to look for the repository root from first.</param>
    /// <param name="baseDirectory">The directory of the tool, to look from when the current directory is not inside the repository.</param>
    /// <param name="output">Where the summary is written.</param>
    /// <param name="error">Where the error is written when the repository root is not found.</param>
    /// <returns>0 when the files were written, 1 when the repository root was not found.</returns>
    internal static Task<int> RunAsync(string[] args, string currentDirectory, string baseDirectory, TextWriter output, TextWriter error) =>
        Engine.RunAsync(args, currentDirectory, baseDirectory, output, error);
}
