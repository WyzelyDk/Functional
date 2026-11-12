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

    /// <summary>WYZ0205: a <c>Match</c> that returns the value or a fallback is <c>ValueOr</c>.</summary>
    internal static readonly DiagnosticDescriptor UseValueOrMatch = new(
        id: "WYZ0205",
        title: "Use ValueOr instead of Match that returns the value or a fallback",
        messageFormat: "This 'Match' returns the value or a fallback; 'ValueOr' says so directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseValueOrMatchDescription,
        helpLinkUri: HelpLinkBase + "WYZ0205.md");

    /// <summary>WYZ0206: a <c>Bind</c> that only wraps a value of a value type in <c>Some</c> is <c>Map</c>.</summary>
    internal static readonly DiagnosticDescriptor UseMapInsteadOfBind = new(
        id: "WYZ0206",
        title: "Use Map instead of Bind that only wraps the value in Some",
        messageFormat: "This 'Bind' always returns 'Maybe.Some' of a value that cannot be null; 'Map' does that without the wrapping",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseMapInsteadOfBindDescription,
        helpLinkUri: HelpLinkBase + "WYZ0206.md");

    /// <summary>WYZ0207: a step that leaves the maybe as it was.</summary>
    internal static readonly DiagnosticDescriptor RemoveNoOpStep = new(
        id: "WYZ0207",
        title: "Remove a step that does not change the maybe",
        messageFormat: "This step returns the maybe it was given, so it does nothing; remove it",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: RemoveNoOpStepDescription,
        helpLinkUri: HelpLinkBase + "WYZ0207.md");

    /// <summary>WYZ0208: a <c>ValueOr</c> with a fallback that does work, which is done also when there is a value.</summary>
    internal static readonly DiagnosticDescriptor UseValueOrFunction = new(
        id: "WYZ0208",
        title: "Pass a function to ValueOr when the fallback does work",
        messageFormat: "The fallback is evaluated even when the maybe has a value; pass a function to 'ValueOr' to evaluate it only for none",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseValueOrFunctionDescription,
        helpLinkUri: HelpLinkBase + "WYZ0208.md");

    private const string Category = "Usage";

    private const string SuggestionCategory = "Design";

    private const string UseValueOrDescription =
        "maybe.IsSome ? maybe.GetValue() : fallback is maybe.ValueOr(fallback), and maybe.ValueOr(() => fallback) when the fallback does work. " +
        "ValueOr(fallback) evaluates the fallback on every call and the conditional only for none, so a fallback that is a constant, " +
        "a local, a parameter or a field is passed as it is, and any other fallback is passed as a function, which is evaluated only for none.";

    private const string UseValueOrMatchDescription =
        "maybe.Match(value => value, () => fallback) is maybe.ValueOr(fallback) for a fallback that is a constant, a local, a parameter or a field, " +
        "and maybe.ValueOr(() => fallback) for any other fallback, which is evaluated only when there is no value, as Match does. " +
        "A Task or ValueTask of a maybe has no form that takes the fallback itself, so it always gets the function.";

    private const string RemoveNoOpStepDescription =
        "Map(x => x), Bind(x => Maybe.Some(x)) and OrElse(() => Maybe.None<T>()) return a maybe with the same state, the same value and the same type.";

    private const string UseMapInsteadOfBindDescription =
        "Bind(x => Maybe.Some(f(x))) is Map(x => f(x)) when f(x) is a value of a value type that cannot be null: " +
        "Some throws for null and Map turns null into none, so the rule leaves a reference type and a nullable value type alone.";

    private const string UseValueOrFunctionDescription =
        "maybe.ValueOr(fallback) evaluates the fallback even when the maybe has a value. " +
        "A fallback that calls a method or creates an object is better passed as a function, maybe.ValueOr(() => fallback), which is evaluated only for none.";

    private const string UncheckedAccessDescription =
        "GetValue() throws InvalidOperationException when the maybe has no value. " +
        "Call it only where a value is known: inside an if on IsSome or IsNone, after an early return, or after TryGetValue. " +
        "TryGetValue, ValueOr, Match and the railway methods need no check.";

    private const string HelpLinkBase = "https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/analyzers/";
}
