using Microsoft.CodeAnalysis;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>The diagnostic descriptors of the analyzers in this package.</summary>
internal static class Rules
{
    /// <summary>WYZ0101: the result of an operation is thrown away, so a failure would go unnoticed.</summary>
    internal static readonly DiagnosticDescriptor UnusedResult = new(
        id: "WYZ0101",
        title: "A Result is not used",
        messageFormat: "The result returned by '{0}' is not used, so a failure would go unnoticed; check it, return it, or discard it explicitly with '_ ='",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: UnusedResultDescription,
        helpLinkUri: HelpLinkBase + "WYZ0101.md");

    /// <summary>WYZ0102: <c>GetError()</c>, <c>GetValue()</c> or <c>ConvertFailure()</c> is called without checking the state of the result first.</summary>
    internal static readonly DiagnosticDescriptor UncheckedAccess = new(
        id: "WYZ0102",
        title: "Check the state of a Result before reading its error or value",
        messageFormat: "'{0}' throws unless the result is {1}; check '{2}' first, or use {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: UncheckedAccessDescription,
        helpLinkUri: HelpLinkBase + "WYZ0102.md");

    private const string Category = "Usage";

    private const string UnusedResultDescription =
        "A Result (or a Task or ValueTask of a Result) that is neither awaited and used nor assigned means a failed operation can be " +
        "ignored silently. Handle the result, return it, or write '_ = ...' to show that ignoring it is intended.";

    private const string UncheckedAccessDescription =
        "GetError() and ConvertFailure() throw when the result succeeded and GetValue() throws when it failed. Call them only where the state is known: " +
        "inside an if on IsFailure or IsSuccess, after an early return, or after TryGetError or TryGetValue. Match, ValueOr and the railway " +
        "methods need no check.";

    private const string HelpLinkBase = "https://github.com/WyzelyDk/Functional/blob/main/docs/result/analyzers/";
}
