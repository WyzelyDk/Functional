namespace Wyzely.Functional.SharedTests;

// The folder of the repository, for the tests that read its files.
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
}
