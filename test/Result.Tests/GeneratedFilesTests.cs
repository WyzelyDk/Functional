using Wyzely.Functional.CodeGeneration;
using Wyzely.Functional.ResultCodeGeneration;

namespace Wyzely.Functional.ResultTests;

public sealed class GeneratedFilesTests
{
    [Fact]
    internal void Generated_files_are_up_to_date_with_the_operation_catalog()
    {
        var root = Generator.FindRoot(AppContext.BaseDirectory);
        Assert.NotNull(root);
        var expected = Generator.Generate(stubs: false);

        var actualPaths = new[] { Generator.LibraryDirectory, Generator.TestDirectory }
            .SelectMany(directory => Directory.GetFiles(Path.Combine(root, directory), "*.g.cs", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);

        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actualPaths);
    }

    [Fact]
    internal void Each_library_file_holds_one_combination_in_a_folder_named_after_its_operation()
    {
        var root = Generator.FindRoot(AppContext.BaseDirectory);
        Assert.NotNull(root);
        var directory = Path.Combine(root, Generator.LibraryDirectory);
        var files = Directory.GetFiles(directory, "*.g.cs", SearchOption.AllDirectories);
        var operationNames = Operations.All.Select(operation => operation.Name).Append("Zip").ToHashSet(StringComparer.Ordinal);
        var zipFiles = (GeneratorSettings.MaxZipArity - 1) * 2;

        Assert.Multiple(
            () => Assert.Equal((Operations.All.Count * Variant.All.Count) + zipFiles, files.Length),
            () => Assert.All(files, file => Assert.Contains(Path.GetFileName(Path.GetDirectoryName(file))!, operationNames)),
            () => Assert.All(files, file => Assert.Single(File.ReadAllLines(file), line => line.StartsWith("    public static ", StringComparison.Ordinal))));
    }

    [Fact]
    internal void Every_generated_file_has_the_content_the_generator_produces()
    {
        var root = Generator.FindRoot(AppContext.BaseDirectory);
        Assert.NotNull(root);
        var stale = new List<string>();

        foreach (var (relativePath, content) in Generator.Generate(stubs: false))
        {
            var onDisk = File.ReadAllText(Path.Combine(root, relativePath));

            if (Normalize(onDisk) != Normalize(content))
            {
                stale.Add(relativePath);
            }
        }

        Assert.True(stale.Count == 0, "Run `dotnet run --project src/Result.CodeGeneration`. Stale files: " + string.Join(", ", stale));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
