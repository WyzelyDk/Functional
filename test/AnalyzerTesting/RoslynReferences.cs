using System.Reflection;
using System.Xml.Linq;

namespace Wyzely.Functional.AnalyzerTesting;

// A compiler refuses an analyzer that references a newer Roslyn than the compiler itself (error CS9057), so the Roslyn that the shipped analyzers
// are built against is the oldest compiler that can use the packages. Test-AnalyzerCompilerCompat.ps1 runs that compiler; these checks are the quick form.
internal static class RoslynReferences
{
    // The oldest supported Roslyn, read from the property that the analyzer projects are built with.
    internal static Version OldestSupported()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var props = XDocument.Load(Path.Combine(directory.FullName, "Directory.Packages.props"));
        var text = props.Descendants("AnalyzerRoslynVersion").Single().Value.Trim();

        var parsed = Version.Parse(text);

        // An assembly reference always has four parts (4.8.0.0), while the text of the property has three.
        return new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
    }

    // The references of the assembly to Roslyn (Microsoft.CodeAnalysis and its parts) that are newer than the limit, with their versions.
    internal static IReadOnlyList<string> NewerThan(Assembly assembly, Version limit)
    {
        var references = assembly.GetReferencedAssemblies();
        var roslyn = references.Where(static name => name.Name!.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));

        return [.. roslyn.Where(name => name.Version > limit).Select(static name => $"{name.Name} {name.Version}")];
    }

    // How many references to Roslyn the assembly has, so that a test cannot pass because it found none.
    internal static int Count(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Count(static name => name.Name!.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
}
