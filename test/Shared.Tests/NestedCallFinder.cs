using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wyzely.Functional.SharedTests;

// Finds the calls that break the rule of AGENTS.md: a call, an object creation with arguments or an await is the argument of another call.
// An argument is a name, a literal, a member access, nameof, a lambda or a method group; anything else that calls goes in a local first.
// Fluent chains are fine (the call is the receiver), and so is a place that cannot hold a local: a field or property initializer, an attribute,
// a default value, a constructor initializer or the base call of a primary constructor.
internal static class NestedCallFinder
{
    // The source is read twice, once for the newest target and once for netstandard2.0, so that the code behind #if of either is checked.
    private static readonly string[][] SymbolSets =
    [
        ["NET11_0_OR_GREATER", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "NET"],
        ["NETSTANDARD2_0", "NETSTANDARD"],
    ];

    internal static IReadOnlyList<NestedCall> Find(string source)
    {
        var found = new SortedDictionary<int, NestedCall>();

        foreach (var symbols in SymbolSets)
        {
            var options = CSharpParseOptions.Default.WithPreprocessorSymbols(symbols);
            var root = CSharpSyntaxTree.ParseText(source, options).GetRoot();

            foreach (var call in FindIn(root))
            {
                found.TryAdd(call.Line, call);
            }
        }

        return [.. found.Values];
    }

    private static IEnumerable<NestedCall> FindIn(SyntaxNode root)
    {
        foreach (var node in root.DescendantNodes().Where(IsCall))
        {
            if (IsInsideArgument(node) || CannotHoldLocal(node) || !HasNestedArgument(node))
            {
                continue;
            }

            var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var text = string.Join(' ', node.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(static part => part.Trim()));

            yield return new NestedCall(line, text);
        }
    }

    private static bool IsCall(SyntaxNode node) =>
        (node is InvocationExpressionSyntax invocation && !IsNameof(invocation)) || node is BaseObjectCreationExpressionSyntax;

    private static bool IsNameof(InvocationExpressionSyntax invocation) => invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" };

    private static SeparatedSyntaxList<ArgumentSyntax> ArgumentsOf(SyntaxNode call) => call switch
    {
        InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments,
        BaseObjectCreationExpressionSyntax creation => creation.ArgumentList?.Arguments ?? default,
        _ => default,
    };

    private static bool HasNestedArgument(SyntaxNode call) => ArgumentsOf(call).Any(static argument => Nests(argument.Expression));

    // An argument nests when a call that has arguments (or a call at all), or an await, is part of it outside of a lambda.
    private static bool Nests(SyntaxNode expression)
    {
        var pending = new Stack<SyntaxNode>([expression]);

        while (pending.Count > 0)
        {
            var node = pending.Pop();

            if (node is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                continue;
            }

            if (node is AwaitExpressionSyntax || (IsCall(node) && !IsCreationWithoutArguments(node)))
            {
                return true;
            }

            foreach (var child in node.ChildNodes())
            {
                pending.Push(child);
            }
        }

        return false;
    }

    private static bool IsCreationWithoutArguments(SyntaxNode node) =>
        node is BaseObjectCreationExpressionSyntax { ArgumentList: null or { Arguments.Count: 0 } };

    // The outermost call of a nesting is the one to report: a call that is itself part of an argument of another call is skipped.
    private static bool IsInsideArgument(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or LocalFunctionStatementSyntax or BlockSyntax or StatementSyntax)
            {
                return false;
            }

            if (ancestor is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: { } owner } } && IsCall(owner))
            {
                return true;
            }
        }

        return false;
    }

    // A place without statements, where a local cannot be declared.
    private static bool CannotHoldLocal(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or StatementSyntax or ArrowExpressionClauseSyntax)
            {
                return false;
            }

            if (ancestor is FieldDeclarationSyntax or PropertyDeclarationSyntax or EnumMemberDeclarationSyntax or AttributeSyntax or ParameterSyntax
                or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax)
            {
                return true;
            }
        }

        return false;
    }

    // A call that breaks the rule: the line it starts on and its text on one line.
    internal sealed record NestedCall(int Line, string Text);
}
