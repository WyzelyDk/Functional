using System.Text;

namespace Wyzely.Functional.CodeGeneration;

/// <summary>Generates the files of an operation catalog for one library and saves them in the repository.</summary>
/// <param name="profile">What differs between the libraries that share this engine.</param>
/// <param name="operations">The operation catalog of the library.</param>
internal sealed class GeneratorEngine(GeneratorProfile profile, IReadOnlyList<OperationSpec> operations)
{
    /// <summary>The name of the solution file that marks the repository root.</summary>
    internal const string SolutionFileName = "Wyzely.Functional.slnx";

    /// <summary>Finds the repository root: the closest directory, starting here and going up, that contains the solution file.</summary>
    /// <param name="start">The directory to start from.</param>
    /// <returns>The repository root, or <see langword="null"/> when no solution file was found.</returns>
    internal static string? FindRoot(string start)
    {
        var directory = new DirectoryInfo(start);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>Generates every file. The keys are paths relative to the repository root, with forward slashes.</summary>
    /// <param name="stubs"><see langword="true"/> to emit method bodies that do nothing useful (only for the red phase of test-driven work).</param>
    /// <returns>The generated files by relative path.</returns>
    internal IReadOnlyDictionary<string, string> Generate(bool stubs)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var libraryEmitter = new LibraryEmitter(profile);
        var testEmitter = new TestEmitter(profile);

        foreach (var operation in operations)
        {
            foreach (var variant in Variant.All)
            {
                var fileName = $"{operation.Stem}.{FilePart(variant.Receiver)}.{FilePart(variant.Delegate)}.g.cs";
                files[$"{profile.LibraryDirectory}/{operation.Name}/{fileName}"] = libraryEmitter.Emit(operation, variant, stubs);
            }

            files[$"{profile.TestDirectory}/{operation.Stem}MatrixTests.g.cs"] = testEmitter.Emit(operation);
        }

        if (profile.Zip is { } zipKind)
        {
            var zip = new ZipEmitter(profile, zipKind);

            for (var arity = 2; arity <= GeneratorSettings.MaxZipArity; arity++)
            {
                files[zip.LibraryPath(arity, selector: false)] = zip.EmitLibrary(arity, selector: false, stubs);
                files[zip.LibraryPath(arity, selector: true)] = zip.EmitLibrary(arity, selector: true, stubs);

                if (zipKind == ZipKind.Maybe)
                {
                    files[zip.LibraryPath(arity, selector: true, nullableStruct: true)] = zip.EmitLibrary(arity, selector: true, stubs, nullableStruct: true);
                }

                files[zip.TestPath(arity)] = zip.EmitTests(arity);
            }
        }

        return files;
    }

    /// <summary>Replaces the generated files in the repository with the given ones. Files are written with CRLF line endings.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="files">The files to write, by relative path.</param>
    internal void Write(string root, IReadOnlyDictionary<string, string> files)
    {
        foreach (var directory in new[] { profile.LibraryDirectory, profile.TestDirectory })
        {
            var path = Path.Combine(root, directory);
            Directory.CreateDirectory(path);

            foreach (var stale in Directory.GetFiles(path, "*.g.cs", SearchOption.AllDirectories))
            {
                File.Delete(stale);
            }

            foreach (var empty in Directory.GetDirectories(path).Where(subdirectory => Directory.GetFileSystemEntries(subdirectory).Length == 0))
            {
                Directory.Delete(empty);
            }
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        foreach (var (relativePath, content) in files)
        {
            var file = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, content.Replace("\n", "\r\n"), encoding);
        }
    }

    // The part of a file name that says how a receiver or delegate is wrapped: Sync, Task or ValueTask.
    private static string FilePart(Wrapper wrapper) => wrapper == Wrapper.None ? "Sync" : wrapper.ToString();
}
