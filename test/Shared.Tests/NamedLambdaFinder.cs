using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.SharedTests;

// Finds the lambdas that break the rule of AGENTS.md. A lambda has no block body (a block, or delegate { }, is a private method or a local function
// that is passed as a method group), and a body that calls two or more times is a chain with one indented line per dot, or it is named.
// A body that is one expression is fine:
// a name, a member access, one call, a comparison, and also a conditional, a switch or a pattern.
internal static class NamedLambdaFinder
{
    // The source is read twice, once for the newest target (net11.0) and once for the older one (net10.0), so that the code behind #if of either is checked.
    private static readonly string[][] SymbolSets =
    [
        ["NET11_0_OR_GREATER", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET"],
        ["NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET"],
    ];

    internal static IReadOnlyList<LambdaProblem> Find(string source)
    {
        var found = new SortedDictionary<int, LambdaProblem>();

        foreach (var symbols in SymbolSets)
        {
            var options = CSharpParseOptions.Default.WithPreprocessorSymbols(symbols);
            var root = CSharpSyntaxTree.ParseText(source, options).GetRoot();

            foreach (var problem in FindIn(root))
            {
                found.TryAdd(problem.Line, problem);
            }
        }

        return [.. found.Values];
    }

    private static IEnumerable<LambdaProblem> FindIn(SyntaxNode root)
    {
        foreach (var lambda in root.DescendantNodes().Where(static node => node is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax))
        {
            var reason = ReasonFor(lambda);

            if (reason is null)
            {
                continue;
            }

            var line = lambda.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var text = string.Join(' ', lambda.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(static part => part.Trim()));

            yield return new LambdaProblem(line, reason, text);
        }
    }

    private static string? ReasonFor(SyntaxNode lambda)
    {
        var body = lambda switch
        {
            LambdaExpressionSyntax { Block: not null } => null,
            LambdaExpressionSyntax { ExpressionBody: { } expression } => expression,
            _ => null,
        };

        if (body is null)
        {
            return "has a block body; make the block a private method or a local function";
        }

        return IsAllowed(body) ? null : "calls two or more times; break the chain into one line per dot, or name it";
    }

    private static bool IsAllowed(ExpressionSyntax body)
    {
        if (body is ConditionalExpressionSyntax or SwitchExpressionSyntax || ContainsPattern(body) || IsList(body))
        {
            return true;
        }

        var calls = CallsOf(body);

        return calls.Count < 2 || IsChainWithALinePerDot(body, calls);
    }

    private static bool ContainsPattern(ExpressionSyntax body) => body.DescendantNodesAndSelf().Any(static node => node is IsPatternExpressionSyntax);

    // A list of parts, each of them one expression: a collection, an array, a tuple or an interpolated string.
    // The catalogs of the generators are lists of lines.
    private static bool IsList(ExpressionSyntax body) =>
        body is CollectionExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or TupleExpressionSyntax
            or InterpolatedStringExpressionSyntax;

    private static List<InvocationExpressionSyntax> CallsOf(SyntaxNode body) =>
    [
        .. body.DescendantNodesAndSelf(static node => node is not LambdaExpressionSyntax and not AnonymousMethodExpressionSyntax)
            .OfType<InvocationExpressionSyntax>()
            .Where(static call => call.Expression is not IdentifierNameSyntax { Identifier.Text: "nameof" }),
    ];

    // A chain is one call after another on the result of the one before, without a call inside the arguments. It reads well when the dot of every call
    // after the first one starts a line.
    private static bool IsChainWithALinePerDot(ExpressionSyntax body, List<InvocationExpressionSyntax> calls)
    {
        var chain = new List<InvocationExpressionSyntax>();
        SyntaxNode? current = body;

        while (current is not null)
        {
            switch (current)
            {
                case InvocationExpressionSyntax call:
                    chain.Add(call);
                    current = call.Expression is MemberAccessExpressionSyntax access ? access.Expression : null;
                    break;
                case MemberAccessExpressionSyntax property:
                    current = property.Expression;
                    break;
                default:
                    current = null;
                    break;
            }
        }

        chain.Reverse();

        return chain.Count == calls.Count && chain.Skip(1).All(StartsALine);
    }

    private static bool StartsALine(InvocationExpressionSyntax call)
    {
        if (call.Expression is not MemberAccessExpressionSyntax access)
        {
            return false;
        }

        var dot = access.OperatorToken;
        var before = dot.GetPreviousToken().GetLocation().GetLineSpan().EndLinePosition.Line;

        return dot.GetLocation().GetLineSpan().StartLinePosition.Line > before;
    }

    // A lambda that breaks the rule: the line it starts on, what is wrong and its text on one line.
    internal sealed record LambdaProblem(int Line, string Reason, string Text);
}
