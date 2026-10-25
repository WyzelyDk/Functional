using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Wyzely.Functional.ResultAnalyzers;

/// <summary>
/// Offers a guard clause for <c>GetValue()</c> (WYZ0102) in a method that returns a result: the failure is returned before the statement that reads
/// the value. There is no fix for <c>GetError()</c> and <c>ConvertFailure()</c>, which need a failed result, because nothing says what the code
/// should do for a success, and none where the early return could change what else the statement does.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UncheckedAccessCodeFixProvider))]
[System.Composition.Shared]
public sealed class UncheckedAccessCodeFixProvider : CodeFixProvider
{
    private const string Title = "Return the failure when the result failed";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["WYZ0102"];

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        // A C# document always has a syntax root and a semantic model.
        var call = root!.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();

        if (call is null || Plan(call, model!) is not { } plan)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(Title, token => InsertGuardAsync(context.Document, plan, token), equivalenceKey: "WYZ0102"),
            diagnostic);
    }

    private static async Task<Document> InsertGuardAsync(Document document, GuardPlan plan, CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var statement = plan.Statement;
        var indentation = statement.GetLeadingTrivia().LastOrDefault(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));

        var guard = SyntaxFactory.IfStatement(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    plan.Receiver.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("IsFailure")),
                SyntaxFactory.Block(SyntaxFactory.ReturnStatement(plan.Returned)))
            .WithLeadingTrivia(indentation)
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var block = (BlockSyntax)statement.Parent!;
        var changed = block.WithStatements(block.Statements.Insert(block.Statements.IndexOf(statement), guard));

        return document.WithSyntaxRoot(root!.ReplaceNode(block, changed));
    }

    // The statement to put the guard before, and what the guard returns, or null when a guard clause could change what the code does.
    private static GuardPlan? Plan(InvocationExpressionSyntax call, SemanticModel model)
    {
        if (call.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetValue" } access
            || call.ArgumentList.Arguments.Count != 0
            || !IsPlainReceiver(access.Expression)
            || StatementOf(call) is not { Parent: BlockSyntax } statement
            || statement.Ancestors().FirstOrDefault(IsFunction) is not { } function
            || function is not (MethodDeclarationSyntax or LocalFunctionStatementSyntax))
        {
            return null;
        }

        var returnType = ReturnTypeOf(function, model);
        var receiverType = model.GetTypeInfo(access.Expression).Type;

        if (returnType is not INamedTypeSymbol { Name: "Result" } result || !IsLibraryType(result) || receiverType is null)
        {
            return null;
        }

        ExpressionSyntax returned;

        if (!result.IsGenericType)
        {
            returned = SyntaxFactory.ParseExpression("Result.Failure(" + access.Expression.WithoutTrivia().ToString() + ".GetError())");
        }
        else if (SymbolEqualityComparer.Default.Equals(receiverType, result))
        {
            returned = access.Expression.WithoutTrivia();
        }
        else
        {
            var name = result.TypeArguments[0].ToMinimalDisplayString(model, statement.SpanStart);
            returned = SyntaxFactory.ParseExpression(access.Expression.WithoutTrivia().ToString() + ".ConvertFailure<" + name + ">()");
        }

        return new GuardPlan(statement, access.Expression, returned);
    }

    private static bool IsFunction(SyntaxNode node) =>
        node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax or AccessorDeclarationSyntax;

    // The type that a return statement of the function has to return: the result, also inside an async method that returns a task of it.
    private static ITypeSymbol? ReturnTypeOf(SyntaxNode function, SemanticModel model)
    {
        // The function is a method or a local function, so its symbol is a method.
        var method = (IMethodSymbol)model.GetDeclaredSymbol(function)!;

        if (!method.IsAsync)
        {
            return method.ReturnType;
        }

        return method.ReturnType is INamedTypeSymbol { IsGenericType: true, Name: "Task" or "ValueTask" } task ? task.TypeArguments[0] : null;
    }

    private static bool IsLibraryType(INamedTypeSymbol type) => type.ContainingNamespace.ToDisplayString() == "Wyzely.Functional";

    // A name, or names joined by dots: a receiver whose evaluation has no effect, so that the guard and the call read the same value.
    private static bool IsPlainReceiver(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax => true,
        ThisExpressionSyntax => true,
        MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression) => IsPlainReceiver(access.Expression),
        _ => false,
    };

    // The statement that holds the call, when the call is evaluated first and always: a declaration, an assignment to a name, an expression or a
    // return, with the call at the start of the expression. Anything evaluated before it, or only sometimes, makes an early return a change.
    private static StatementSyntax? StatementOf(SyntaxNode node) => node.Parent switch
    {
        LocalDeclarationStatementSyntax { IsConst: false, UsingKeyword.RawKind: 0 } or ExpressionStatementSyntax or ReturnStatementSyntax
            => (StatementSyntax)node.Parent,
        MemberAccessExpressionSyntax access when access.Expression == node => StatementOf(access),
        InvocationExpressionSyntax invocation when invocation.Expression == node => StatementOf(invocation),
        ElementAccessExpressionSyntax element when element.Expression == node => StatementOf(element),
        BinaryExpressionSyntax binary when binary.Left == node && !IsShortCircuit(binary) => StatementOf(binary),
        AssignmentExpressionSyntax { Left: IdentifierNameSyntax } assignment when assignment.Right == node => StatementOf(assignment),
        ParenthesizedExpressionSyntax or CastExpressionSyntax or EqualsValueClauseSyntax or VariableDeclaratorSyntax or VariableDeclarationSyntax
            => StatementOf(node.Parent),
        _ => null,
    };

    private static bool IsShortCircuit(BinaryExpressionSyntax binary) =>
        binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression) || binary.IsKind(SyntaxKind.CoalesceExpression);

    private sealed class GuardPlan(StatementSyntax statement, ExpressionSyntax receiver, ExpressionSyntax returned)
    {
        internal StatementSyntax Statement { get; } = statement;

        internal ExpressionSyntax Receiver { get; } = receiver;

        internal ExpressionSyntax Returned { get; } = returned;
    }
}
