using Microsoft.CodeAnalysis;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>The diagnostic descriptors of the analyzers in this package.</summary>
internal static class Rules
{
    /// <summary>WYZ0201: <c>GetValue()</c> is called without checking that the maybe has a value first.</summary>
    internal static readonly DiagnosticDescriptor UncheckedAccess = new(
        id: "WYZ0201",
        title: "Check that a Maybe has a value before calling GetValue()",
        messageFormat: "'GetValue()' throws unless the maybe has a value; check 'IsSome' first, or use TryGetValue, ValueOr or Match",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: UncheckedAccessDescription,
        helpLinkUri: HelpLinkBase + "WYZ0201.md");

    private const string Category = "Usage";

    private const string UncheckedAccessDescription =
        "GetValue() throws InvalidOperationException when the maybe has no value. Call it only where a value is known: inside an if " +
        "on IsSome or IsNone, after an early return, or after TryGetValue. TryGetValue, ValueOr, Match and the railway methods need no check.";

    private const string HelpLinkBase = "https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/analyzers/";
}
