namespace Wyzely.Functional.SharedTests;

// The folder of the repository and its source files, for the tests that read them.
internal static class RepositoryRoot
{
    internal static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Wyzely.Functional.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root (the folder with Wyzely.Functional.slnx) was not found.");
    }

    // The C# files below a folder of the repository, without build output.
    internal static IEnumerable<string> CSharpFilesOf(string root, string folder) =>
        Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories).Where(static path => !IsBuildOutput(path));

    internal static bool IsBuildOutput(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');

        return segments.Contains("bin") || segments.Contains("obj");
    }

    // The path relative to the root, with forward slashes.
    internal static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
