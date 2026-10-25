using System.Diagnostics.CodeAnalysis;

namespace Wyzely.Functional.MaybeCodeGeneration;

// Usage: dotnet run --project src/Maybe.CodeGeneration [-- --stubs]
//   --stubs   write method bodies that do nothing useful, to see the generated tests fail first (test-driven work)
internal static class Program
{
    [ExcludeFromCodeCoverage(Justification = "A thin entry point: GeneratorEngine.RunAsync does the work and is tested.")]
    private static Task<int> Main(string[] args) =>
        Generator.RunAsync(args, Directory.GetCurrentDirectory(), AppContext.BaseDirectory, Console.Out, Console.Error);
}
