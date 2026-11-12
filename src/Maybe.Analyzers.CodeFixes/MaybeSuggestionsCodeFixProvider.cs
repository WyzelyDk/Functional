using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.MaybeAnalyzers;

/// <summary>
/// Rewrites the code that the suggestion rules (WYZ0202 to WYZ0208) point at into the method of the library that says what it does,
/// or into the simpler form.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MaybeSuggestionsCodeFixProvider))]
[System.Composition.Shared]
public sealed class MaybeSuggestionsCodeFixProvider : RewriteCodeFixProvider
{
    /// <inheritdoc />
    protected override ImmutableDictionary<string, string> Titles { get; } = new Dictionary<string, string>
    {
        ["WYZ0202"] = "Use Any",
        ["WYZ0203"] = "Use ValueOr",
        ["WYZ0204"] = "Use Maybe.From",
        ["WYZ0205"] = "Use ValueOr",
        ["WYZ0206"] = "Use Map",
        ["WYZ0207"] = "Remove the step",
        ["WYZ0208"] = "Pass a function to ValueOr",
    }.ToImmutableDictionary();

    /// <inheritdoc />
    // The member access that ends in IsSome for WYZ0202, the conditional expression for WYZ0203 and WYZ0204, otherwise the call that the diagnostic is on.
    protected override bool IsTarget(SyntaxNode node, string id)
    {
        if (id == "WYZ0202")
        {
            return node is MemberAccessExpressionSyntax;
        }

        return id is "WYZ0203" or "WYZ0204" ? node is ConditionalExpressionSyntax : node is InvocationExpressionSyntax;
    }

    /// <inheritdoc />
    // A chain of ifs and not a switch on the text: the compiler turns a switch of this size into a hash table,
    // and the equality check behind it has a side (the same hash for another text) that no test can take.
    protected override SyntaxNode? Rewrite(SyntaxNode target, Diagnostic diagnostic)
    {
        var id = diagnostic.Id;

        if (id == "WYZ0202")
        {
            return SuggestionRewrites.Any((MemberAccessExpressionSyntax)target);
        }

        if (id == "WYZ0203")
        {
            return SuggestionRewrites.ConditionalToValueOr((ConditionalExpressionSyntax)target, diagnostic.Properties);
        }

        if (id == "WYZ0204")
        {
            return SuggestionRewrites.From((ConditionalExpressionSyntax)target);
        }

        // Every other rule is about a call, which is what IsTarget looks for.
        return RewriteCallRules((InvocationExpressionSyntax)target, id, diagnostic.Properties);
    }

    // WYZ0208 is the last rule, so it is what is left.
    private static SyntaxNode? RewriteCallRules(InvocationExpressionSyntax call, string id, ImmutableDictionary<string, string?> properties)
    {
        if (id == "WYZ0205")
        {
            return SuggestionRewrites.MatchToValueOr(call, properties);
        }

        if (id == "WYZ0206")
        {
            return SuggestionRewrites.BindToMap(call);
        }

        return id == "WYZ0207" ? RewriteHelpers.RemoveStep(call) : RewriteHelpers.ValueOrFunction(call);
    }
}
