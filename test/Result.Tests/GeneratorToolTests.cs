using Wyzely.Functional.ResultCodeGeneration;

namespace Wyzely.Functional.ResultTests;

// The tool that writes the generated files: the stub mode for the red phase, the replacement of the files in the repository, the search for the
// repository root, and the command that ties them together. Everything is written to a folder of its own, never to the repository.
public sealed class GeneratorToolTests
{
    [Fact]
    internal void The_stubs_have_the_files_of_the_real_output_but_bodies_that_do_nothing_useful()
    {
        var real = Generator.Generate(stubs: false);
        var stubs = Generator.Generate(stubs: true);
        var libraryFiles = stubs.Where(file => file.Key.StartsWith(Generator.LibraryDirectory, StringComparison.Ordinal)).ToList();

        Assert.Multiple(
            () => Assert.Equal(real.Keys, stubs.Keys),
            () => Assert.Contains(real, file => file.Value.Contains("ConfigureAwait", StringComparison.Ordinal)),
            () => Assert.DoesNotContain(libraryFiles, file => file.Value.Contains("ConfigureAwait", StringComparison.Ordinal)),
            () => Assert.Contains(libraryFiles, file => file.Value.Contains("return default!;", StringComparison.Ordinal)),
            () => Assert.Contains(libraryFiles, file => file.Value.Contains("return Task.FromResult(default(", StringComparison.Ordinal)),
            () => Assert.Contains(libraryFiles, file => file.Value.Contains("return new ValueTask<", StringComparison.Ordinal)));
    }

    [Fact]
    internal void Write_replaces_the_generated_files_removes_stale_ones_and_empty_folders_and_writes_crlf_without_a_byte_order_mark()
    {
        using var folder = new TemporaryFolder();
        var library = Path.Combine(folder.Path, Generator.LibraryDirectory);
        Directory.CreateDirectory(Path.Combine(library, "Old"));
        File.WriteAllText(Path.Combine(library, "Old", "Stale.g.cs"), "stale");
        Directory.CreateDirectory(Path.Combine(library, "Empty"));
        var kept = Path.Combine(library, "Kept.txt");
        File.WriteAllText(kept, "not generated");
        var written = Generator.LibraryDirectory + "/Map/Fresh.g.cs";

        Generator.Write(folder.Path, new Dictionary<string, string> { [written] = "first\nsecond\n" });

        var bytes = File.ReadAllBytes(Path.Combine(folder.Path, written));
        Assert.Multiple(
            () => Assert.False(File.Exists(Path.Combine(library, "Old", "Stale.g.cs"))),
            () => Assert.False(Directory.Exists(Path.Combine(library, "Old"))),
            () => Assert.False(Directory.Exists(Path.Combine(library, "Empty"))),
            () => Assert.True(File.Exists(kept)),
            () => Assert.Equal("first\r\nsecond\r\n", System.Text.Encoding.UTF8.GetString(bytes)),
            () => Assert.NotEqual(0xEF, bytes[0]));
    }

    [Fact]
    internal void FindRoot_finds_the_folder_with_the_solution_file_above_the_start_and_is_null_without_one()
    {
        using var with = new TemporaryFolder();
        File.WriteAllText(Path.Combine(with.Path, Generator.SolutionFileName), string.Empty);
        var nested = Directory.CreateDirectory(Path.Combine(with.Path, "src", "deeper")).FullName;
        using var without = new TemporaryFolder();

        Assert.Multiple(
            () => Assert.Equal(with.Path, Generator.FindRoot(nested)),
            () => Assert.Equal(with.Path, Generator.FindRoot(with.Path)),
            () => Assert.Null(Generator.FindRoot(without.Path)));
    }

    [Fact]
    internal async Task The_command_writes_the_files_and_reports_how_many()
    {
        using var folder = TemporaryFolder.Repository();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await Generator.RunAsync([], folder.Path, folder.Path, output, error);

        var expected = Generator.Generate(stubs: false);
        Assert.Multiple(
            () => Assert.Equal(0, exitCode),
            () => Assert.Equal($"Generated {expected.Count} files in {folder.Path}.", output.ToString().Trim()),
            () => Assert.Empty(error.ToString()),
            () => Assert.All(expected.Keys, path => Assert.True(File.Exists(Path.Combine(folder.Path, path)), path)));
    }

    [Fact]
    internal async Task The_command_with_stubs_says_so_and_looks_in_the_folder_of_the_tool_when_the_current_folder_is_not_in_the_repository()
    {
        using var repository = TemporaryFolder.Repository();
        using var elsewhere = new TemporaryFolder();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await Generator.RunAsync(["--stubs"], elsewhere.Path, repository.Path, output, error);

        Assert.Multiple(
            () => Assert.Equal(0, exitCode),
            () => Assert.EndsWith($" files (stubs) in {repository.Path}.", output.ToString().Trim(), StringComparison.Ordinal));
    }

    [Fact]
    internal async Task The_command_fails_with_a_message_when_there_is_no_repository()
    {
        using var nowhere = new TemporaryFolder();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await Generator.RunAsync([], nowhere.Path, nowhere.Path, output, error);

        Assert.Multiple(
            () => Assert.Equal(1, exitCode),
            () => Assert.Contains(Generator.SolutionFileName, error.ToString(), StringComparison.Ordinal),
            () => Assert.Empty(output.ToString()));
    }

    // A folder that is deleted with its content when the test ends.
    private sealed class TemporaryFolder : IDisposable
    {
        internal TemporaryFolder() => Path = Directory.CreateTempSubdirectory("wyzely-generator-").FullName;

        internal string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);

        // A folder that looks like a repository root: it holds the solution file.
        internal static TemporaryFolder Repository()
        {
            var folder = new TemporaryFolder();
            File.WriteAllText(System.IO.Path.Combine(folder.Path, Generator.SolutionFileName), string.Empty);

            return folder;
        }
    }
}
