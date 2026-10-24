using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// Rewrites the code that the suggestion rules (WYZ0103 to WYZ0110 and WYZ0112 to WYZ0117) point at into the method of the library that says what
/// it does, or into the simpler form.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SuggestionsCodeFixProvider))]
[System.Composition.Shared]
public sealed class SuggestionsCodeFixProvider : RewriteCodeFixProvider
{
    /// <inheritdoc />
    protected override ImmutableDictionary<string, string> Titles { get; } = new Dictionary<string, string>
    {
        ["WYZ0103"] = "Use BindIf",
        ["WYZ0104"] = "Use MapIf",
        ["WYZ0105"] = "Use EnsureNot",
        ["WYZ0106"] = "Use EnsureNotNull",
        ["WYZ0107"] = "Use BindZip",
        ["WYZ0108"] = "Use ConvertFailure",
        ["WYZ0109"] = "Use SuccessIf or FailureIf",
        ["WYZ0110"] = "Use MapTry",
        ["WYZ0112"] = "Use Map",
        ["WYZ0113"] = "Use Tap",
        ["WYZ0114"] = "Use ValueOr",
        ["WYZ0115"] = "Remove the step",
        ["WYZ0116"] = "Pass the message to Failure",
        ["WYZ0117"] = "Use Flatten",
    }.ToImmutableDictionary();

    /// <inheritdoc />
    // The conditional expression for WYZ0109, otherwise the call that the diagnostic is reported on.
    protected override bool IsTarget(SyntaxNode node, string id) => id == "WYZ0109" ? node is ConditionalExpressionSyntax : node is InvocationExpressionSyntax;

    /// <inheritdoc />
    protected override SyntaxNode? Rewrite(SyntaxNode target, Diagnostic diagnostic)
    {
        var properties = diagnostic.Properties;

        return (target, diagnostic.Id) switch
        {
            (InvocationExpressionSyntax call, "WYZ0103") => SuggestionRewrites.BindIf(call, properties),
            (InvocationExpressionSyntax call, "WYZ0104") => SuggestionRewrites.MapIf(call, properties),
            (InvocationExpressionSyntax call, "WYZ0105") => SuggestionRewrites.EnsureNot(call),
            (InvocationExpressionSyntax call, "WYZ0106") => SuggestionRewrites.EnsureNotNull(call, properties),
            (InvocationExpressionSyntax call, "WYZ0107") => SuggestionRewrites.BindZip(call),
            (InvocationExpressionSyntax call, "WYZ0108") => SuggestionRewrites.ConvertFailure(call),
            (ConditionalExpressionSyntax conditional, "WYZ0109") => SuggestionRewrites.SuccessIf(conditional, properties),
            (InvocationExpressionSyntax call, "WYZ0110") => SuggestionRewrites.MapTry(call),
            (InvocationExpressionSyntax call, "WYZ0112") => SuggestionRewrites.BindToMap(call),
            (InvocationExpressionSyntax call, "WYZ0113") => SuggestionRewrites.MapToTap(call),
            (InvocationExpressionSyntax call, "WYZ0114") => SuggestionRewrites.MatchToValueOr(call),
            (InvocationExpressionSyntax call, "WYZ0115") => SuggestionRewrites.RemoveStep(call),
            (InvocationExpressionSyntax call, "WYZ0116") => SuggestionRewrites.FailureWithMessage(call),
            (InvocationExpressionSyntax call, "WYZ0117") => SuggestionRewrites.Flatten(call),
            _ => null,
        };
    }
}
