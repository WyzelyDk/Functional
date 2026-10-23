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
}
