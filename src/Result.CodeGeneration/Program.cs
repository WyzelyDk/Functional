using Wyzely.Functional.ResultCodeGeneration;

// Usage: dotnet run --project src/Result.CodeGeneration [-- --stubs]
//   --stubs   write method bodies that do nothing useful, to see the generated tests fail first (test-driven work)
var stubs = args.Contains("--stubs", StringComparer.Ordinal);
var root = Generator.FindRoot(Directory.GetCurrentDirectory()) ?? Generator.FindRoot(AppContext.BaseDirectory);

if (root is null)
{
    await Console.Error.WriteLineAsync($"Could not find {Generator.SolutionFileName} above the current directory.");

    return 1;
}

var files = Generator.Generate(stubs);
Generator.Write(root, files);
await Console.Out.WriteLineAsync($"Generated {files.Count} files{(stubs ? " (stubs)" : string.Empty)} in {root}.");

return 0;
