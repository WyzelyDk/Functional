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

        if (diagnostic.Id == "WYZ0109")
        {
            return SuggestionRewrites.SuccessIf((ConditionalExpressionSyntax)target, properties);
        }

        // Every other rule is about a call, which is what IsTarget looks for.
        var call = (InvocationExpressionSyntax)target;

        // A chain of ifs and not a switch on the text: the compiler turns a switch of this size into a hash table, and the equality check behind it has a
        // side (the same hash for another text) that no test can take.
        if (diagnostic.Id == "WYZ0103")
        {
            return SuggestionRewrites.BindIf(call, properties);
        }

        if (diagnostic.Id == "WYZ0104")
        {
            return SuggestionRewrites.MapIf(call, properties);
        }

        if (diagnostic.Id == "WYZ0105")
        {
            return SuggestionRewrites.EnsureNot(call);
        }

        if (diagnostic.Id == "WYZ0106")
        {
            return SuggestionRewrites.EnsureNotNull(call, properties);
        }

        if (diagnostic.Id == "WYZ0107")
        {
            return SuggestionRewrites.BindZip(call);
        }

        if (diagnostic.Id == "WYZ0108")
        {
            return SuggestionRewrites.ConvertFailure(call);
        }

        if (diagnostic.Id == "WYZ0110")
        {
            return SuggestionRewrites.MapTry(call);
        }

        if (diagnostic.Id == "WYZ0112")
        {
            return SuggestionRewrites.BindToMap(call);
        }

        if (diagnostic.Id == "WYZ0113")
        {
            return SuggestionRewrites.MapToTap(call);
        }

        if (diagnostic.Id == "WYZ0114")
        {
            return SuggestionRewrites.MatchToValueOr(call);
        }

        if (diagnostic.Id == "WYZ0115")
        {
            return SuggestionRewrites.RemoveStep(call);
        }

        if (diagnostic.Id == "WYZ0116")
        {
            return SuggestionRewrites.FailureWithMessage(call);
        }

        return SuggestionRewrites.Flatten(call);
    }
}
