namespace Wyzely.Functional.CodeGeneration;

/// <summary>
/// Everything that differs between the libraries that share the generator: the names in the generated code, the places it is written
/// to, and the setting its awaits follow. A library owns one profile and one operation catalog; the rest is shared.
/// </summary>
/// <param name="ToolNamespace">The namespace of the generator, named in the header comment of every generated file.</param>
/// <param name="ToolProject">The path of the generator project, named in the header comment.</param>
/// <param name="ExtensionsClass">The static partial class that receives the generated methods, for example <c>ResultExtensions</c>.</param>
/// <param name="ConfigurationClass">The static configuration class whose <c>ContinueOnCapturedContext</c> the awaits follow.</param>
/// <param name="OptionsClass">The options class that documents the setting.</param>
/// <param name="ReceiverParameter">The name of the receiver parameter, which is also the noun of the documentation (<c>result</c>, <c>maybe</c>).</param>
/// <param name="LibraryDirectory">The directory, relative to the repository root, that receives the generated library files.</param>
/// <param name="TestDirectory">The directory, relative to the repository root, that receives the generated tests.</param>
/// <param name="TestNamespace">The namespace of the generated tests.</param>
/// <param name="TestPreamble">The lines that open every generated test, for example a shared error to assert against.</param>
/// <param name="Zip">What kind of value the generated <c>Zip</c> overloads combine, or <see langword="null"/> when the library has none.</param>
/// <param name="CachesCompletedNone">
/// <see langword="true"/> when the library has a cached completed task for "no value" (<c>MaybeTasks&lt;T&gt;.None</c>) that the generated
/// fast paths should hand out.
/// </param>
internal sealed record GeneratorProfile(
    string ToolNamespace,
    string ToolProject,
    string ExtensionsClass,
    string ConfigurationClass,
    string OptionsClass,
    string ReceiverParameter,
    string LibraryDirectory,
    string TestDirectory,
    string TestNamespace,
    string[] TestPreamble,
    ZipKind? Zip = null,
    bool CachesCompletedNone = false);
