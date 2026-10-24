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

    /// <summary>WYZ0202: <c>Filter(p).IsSome</c> is <c>Any(p)</c>.</summary>
    internal static readonly DiagnosticDescriptor UseAny = new(
        id: "WYZ0202",
        title: "Use Any instead of Filter followed by IsSome",
        messageFormat: "'Filter' followed by 'IsSome' asks whether the value satisfies a condition; 'Any' says so directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "maybe.Filter(predicate).IsSome is maybe.Any(predicate): true when there is a value and it satisfies the predicate.",
        helpLinkUri: HelpLinkBase + "WYZ0202.md");

    /// <summary>WYZ0203: a conditional on <c>IsSome</c> that returns the value or a fallback is <c>ValueOr</c>.</summary>
    internal static readonly DiagnosticDescriptor UseValueOr = new(
        id: "WYZ0203",
        title: "Use ValueOr instead of a conditional on IsSome",
        messageFormat: "This conditional returns the value of the maybe or a fallback; 'ValueOr' says so directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseValueOrDescription,
        helpLinkUri: HelpLinkBase + "WYZ0203.md");

    /// <summary>WYZ0204: a null check that chooses between none and some of the same value is <c>Maybe.From</c>.</summary>
    internal static readonly DiagnosticDescriptor UseFrom = new(
        id: "WYZ0204",
        title: "Use Maybe.From instead of a null check",
        messageFormat: "This null check chooses between none and some of the same value; 'Maybe.From' does that",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "x is null ? Maybe.None<T>() : Maybe.Some(x) is Maybe.From(x): none for null, otherwise a maybe that holds the value.",
        helpLinkUri: HelpLinkBase + "WYZ0204.md");

    private const string Category = "Usage";

    private const string SuggestionCategory = "Design";

    private const string UseValueOrDescription =
        "maybe.IsSome ? maybe.GetValue() : fallback is maybe.ValueOr(fallback). ValueOr evaluates the fallback on every call and the " +
        "conditional only for none, so the rule reports it only when the fallback is a constant, a local, a parameter or a field.";

    private const string UncheckedAccessDescription =
        "GetValue() throws InvalidOperationException when the maybe has no value. Call it only where a value is known: inside an if " +
        "on IsSome or IsNone, after an early return, or after TryGetValue. TryGetValue, ValueOr, Match and the railway methods need no check.";

    private const string HelpLinkBase = "https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/analyzers/";
}
