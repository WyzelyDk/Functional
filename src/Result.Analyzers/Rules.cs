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

    /// <summary>WYZ0103: Use BindIf instead of a condition inside Bind.</summary>
    internal static readonly DiagnosticDescriptor UseBindIf = new(
        id: "WYZ0103",
        title: "Use BindIf instead of a condition inside Bind",
        messageFormat: "This 'Bind' returns the value unchanged unless a condition holds; 'BindIf' says so directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseBindIfDescription,
        helpLinkUri: HelpLinkBase + "WYZ0103.md");

    /// <summary>WYZ0104: Use MapIf instead of a condition inside Map.</summary>
    internal static readonly DiagnosticDescriptor UseMapIf = new(
        id: "WYZ0104",
        title: "Use MapIf instead of a condition inside Map",
        messageFormat: "This 'Map' returns the value unchanged unless a condition holds; 'MapIf' says so directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseMapIfDescription,
        helpLinkUri: HelpLinkBase + "WYZ0104.md");

    /// <summary>WYZ0105: Use EnsureNot instead of Ensure with a negated condition.</summary>
    internal static readonly DiagnosticDescriptor UseEnsureNot = new(
        id: "WYZ0105",
        title: "Use EnsureNot instead of Ensure with a negated condition",
        messageFormat: "'Ensure' with a negated condition is 'EnsureNot'",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Ensure(x => !condition, error) is EnsureNot(x => condition, error): the value is rejected when the condition holds.",
        helpLinkUri: HelpLinkBase + "WYZ0105.md");

    /// <summary>WYZ0106: Use EnsureNotNull instead of a null check inside Bind.</summary>
    internal static readonly DiagnosticDescriptor UseEnsureNotNull = new(
        id: "WYZ0106",
        title: "Use EnsureNotNull instead of a null check inside Bind",
        messageFormat: "This 'Bind' checks the value for null; 'EnsureNotNull' does the same and removes the nullable annotation",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseEnsureNotNullDescription,
        helpLinkUri: HelpLinkBase + "WYZ0106.md");

    /// <summary>WYZ0107: Use BindZip to keep the earlier value.</summary>
    internal static readonly DiagnosticDescriptor UseBindZip = new(
        id: "WYZ0107",
        title: "Use BindZip to keep the earlier value",
        messageFormat: "This 'Bind' and 'Map' keep a value next to the next one in a tuple; 'BindZip' does that directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Bind(a => f(a).Map(b => (a, b))) is BindZip(a => f(a)). The elements of the tuple are named First and Second, like the ones of Zip.",
        helpLinkUri: HelpLinkBase + "WYZ0107.md");

    /// <summary>WYZ0108: Use ConvertFailure to pass a failure on as another type.</summary>
    internal static readonly DiagnosticDescriptor UseConvertFailure = new(
        id: "WYZ0108",
        title: "Use ConvertFailure to pass a failure on as another type",
        messageFormat: "'Failure<T>' of 'GetError()' is 'ConvertFailure<T>()'",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Result.Failure<T>(r.GetError()) is r.ConvertFailure<T>(): the same error as a failed result of another value type.",
        helpLinkUri: HelpLinkBase + "WYZ0108.md");

    /// <summary>WYZ0109: Use SuccessIf or FailureIf instead of a conditional expression.</summary>
    internal static readonly DiagnosticDescriptor UseSuccessIf = new(
        id: "WYZ0109",
        title: "Use SuccessIf or FailureIf instead of a conditional expression",
        messageFormat: "This conditional expression creates a result from a condition; '{0}' says so directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: UseSuccessIfDescription,
        helpLinkUri: HelpLinkBase + "WYZ0109.md");

    /// <summary>WYZ0110: Use MapTry instead of Result.Try inside Bind.</summary>
    internal static readonly DiagnosticDescriptor UseMapTry = new(
        id: "WYZ0110",
        title: "Use MapTry instead of Result.Try inside Bind",
        messageFormat: "This 'Bind' runs a function inside 'Result.Try'; 'MapTry' does that directly",
        category: SuggestionCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Bind(x => Result.Try(() => f(x))) is MapTry(x => f(x)): an exception becomes a failed result in the same way.",
        helpLinkUri: HelpLinkBase + "WYZ0110.md");

    /// <summary>WYZ0111: a value is interpolated into the message of an error.</summary>
    internal static readonly DiagnosticDescriptor InterpolatedMessage = new(
        id: "WYZ0111",
        title: "Do not interpolate values into the message of an error",
        messageFormat: "The message passed to '{0}' is built with string interpolation; make the values details of the error instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: InterpolatedMessageDescription,
        helpLinkUri: HelpLinkBase + "WYZ0111.md");

    private const string Category = "Usage";

    private const string SuggestionCategory = "Design";

    private const string UseBindIfDescription =
        "A Bind whose delegate returns Result.Success(x) for one branch of a condition and another result for the other " +
        "branch is a BindIf. BindIf takes the condition as a function that runs only for a successful result, so the " +
        "behavior is the same.";

    private const string UseMapIfDescription =
        "A Map whose delegate returns the value itself for one branch of a condition and a new value of the same type " +
        "for the other branch is a MapIf. MapIf takes the condition as a function that runs only for a successful " +
        "result, so the behavior is the same.";

    private const string UseEnsureNotNullDescription =
        "A Bind that fails for a null value and succeeds with the value otherwise is EnsureNotNull, which also turns a " +
        "Result<T?> into a Result<T>.";

    private const string UseSuccessIfDescription =
        "cond ? Result.Success() : Result.Failure(e) is Result.SuccessIf(cond, e), and the reverse is " +
        "Result.FailureIf(cond, e). The rule reports it only when the error is a constant, a local or a field, because " +
        "passing the error as a function would allocate a delegate on every call.";

    private const string InterpolatedMessageDescription =
        "An interpolated message is built every time the line runs, also when the result succeeds, and the values become part of the " +
        "text. A message template with {Name} placeholders and the values as details costs nothing until the message is read, keeps " +
        "the template the same for every value (so errors can be grouped), and lets a detail be marked as sensitive so that redacted " +
        "output hides it.";

    private const string UnusedResultDescription =
        "A Result (or a Task or ValueTask of a Result) that is neither awaited and used nor assigned means a failed operation can be " +
        "ignored silently. Handle the result, return it, or write '_ = ...' to show that ignoring it is intended.";

    private const string UncheckedAccessDescription =
        "GetError() and ConvertFailure() throw when the result succeeded and GetValue() throws when it failed. Call them only where the state is known: " +
        "inside an if on IsFailure or IsSuccess, after an early return, or after TryGetError or TryGetValue. Match, ValueOr and the railway " +
        "methods need no check.";

    private const string HelpLinkBase = "https://github.com/WyzelyDk/Functional/blob/main/docs/result/analyzers/";
}
