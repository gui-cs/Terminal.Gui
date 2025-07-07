using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Gui.Analyzers;

[DiagnosticAnalyzer (LanguageNames.CSharp)]
public class HandledEventArgsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TGUI001";
    private static readonly LocalizableString Title = "Accepting event handler should set Handled = true";
    private static readonly LocalizableString MessageFormat = "Accepting event handler does not set Handled = true";
    private static readonly LocalizableString Description = "Handlers for Accepting should mark the CommandEventArgs as handled by setting Handled = true otherwise subsequent Accepting event handlers may also fire (e.g. default buttons).";
    private static readonly string Url = "https://github.com/tznind/gui.cs/blob/analyzer-no-handled/Terminal.Gui.Analyzers/TGUI001.md";
    private const string Category = nameof(DiagnosticCategory.Reliability);

    private static readonly DiagnosticDescriptor _rule = new (
                                                              DiagnosticId,
                                                              Title,
                                                              MessageFormat,
                                                              Category,
                                                              DiagnosticSeverity.Warning,
                                                              true,
                                                              Description,
                                                              helpLinkUri: Url);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize (AnalysisContext context)
    {
        context.EnableConcurrentExecution ();

        // Only analyze non-generated code
        context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);

        // Register for b.Accepting += (s,e)=>{...};
        context.RegisterSyntaxNodeAction (
                                          AnalyzeLambdaOrAnonymous,
                                          SyntaxKind.ParenthesizedLambdaExpression,
                                          SyntaxKind.SimpleLambdaExpression,
                                          SyntaxKind.AnonymousMethodExpression);

        // Register for b.Accepting += MyMethod;
        context.RegisterSyntaxNodeAction (
                                          AnalyzeEventSubscriptionWithMethodGroup,
                                          SyntaxKind.AddAssignmentExpression);
    }

    private static void AnalyzeLambdaOrAnonymous (SyntaxNodeAnalysisContext context)
    {
        var lambda = (AnonymousFunctionExpressionSyntax)context.Node;

        // Check if this lambda is assigned to the Accepting event
        if (!IsAssignedToAcceptingEvent (lambda.Parent, context))
        {
            return;
        }

        // Look for any parameter of type CommandEventArgs (regardless of name)
        IParameterSymbol? eParam = GetCommandEventArgsParameter (lambda, context.SemanticModel);

        if (eParam == null)
        {
            return;
        }

        // Analyze lambda body for e.Handled = true assignment
        if (lambda.Body is BlockSyntax block)
        {
            bool setsHandled = block.Statements
                                    .SelectMany (s => s.DescendantNodes ().OfType<AssignmentExpressionSyntax> ())
                                    .Any (a => IsHandledAssignment (a, eParam, context));

            if (!setsHandled)
            {
                var diag = Diagnostic.Create (_rule, lambda.GetLocation ());
                context.ReportDiagnostic (diag);
            }
        }
        else if (lambda.Body is ExpressionSyntax)
        {
            // Expression-bodied lambdas unlikely for event handlers — skip
        }
    }

    /// <summary>
    ///     Finds the first parameter of type CommandEventArgs in any parameter list (method or lambda).
    /// </summary>
    /// <param name="paramOwner"></param>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    private static IParameterSymbol? GetCommandEventArgsParameter (SyntaxNode paramOwner, SemanticModel semanticModel)
    {
        SeparatedSyntaxList<ParameterSyntax>? parameters = paramOwner switch
                                                           {
                                                               AnonymousFunctionExpressionSyntax lambda => GetParameters (lambda),
                                                               MethodDeclarationSyntax method => method.ParameterList.Parameters,
                                                               _ => null
                                                           };

        if (parameters == null || parameters.Value.Count == 0)
        {
            return null;
        }

        foreach (ParameterSyntax param in parameters.Value)
        {
            IParameterSymbol? symbol = semanticModel.GetDeclaredSymbol (param);

            if (symbol != null && IsCommandEventArgsType (symbol.Type))
            {
                return symbol;
            }
        }

        return null;
    }

    private static bool IsAssignedToAcceptingEvent (SyntaxNode? node, SyntaxNodeAnalysisContext context)
    {
        if (node is AssignmentExpressionSyntax assignment && IsAcceptingEvent (assignment.Left, context))
        {
            return true;
        }

        if (node?.Parent is AssignmentExpressionSyntax parentAssignment && IsAcceptingEvent (parentAssignment.Left, context))
        {
            return true;
        }

        return false;
    }

    private static bool IsCommandEventArgsType (ITypeSymbol? type) { return type != null && type.Name == "CommandEventArgs"; }

    private static void AnalyzeEventSubscriptionWithMethodGroup (SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        // Check event name: b.Accepting += ...
        if (!IsAcceptingEvent (assignment.Left, context))
        {
            return;
        }

        // Right side: should be method group (IdentifierNameSyntax)
        if (assignment.Right is IdentifierNameSyntax methodGroup)
        {
            // Resolve symbol of method group
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo (methodGroup);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                // Find method declaration in syntax tree
                ImmutableArray<SyntaxReference> declRefs = methodSymbol.DeclaringSyntaxReferences;

                foreach (SyntaxReference declRef in declRefs)
                {
                    var methodDecl = declRef.GetSyntax () as MethodDeclarationSyntax;

                    if (methodDecl != null)
                    {
                        AnalyzeHandlerMethodBody (context, methodDecl, methodSymbol);
                    }
                }
            }
        }
    }

    private static void AnalyzeHandlerMethodBody (SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDecl, IMethodSymbol methodSymbol)
    {
        // Look for any parameter of type CommandEventArgs
        IParameterSymbol? eParam = GetCommandEventArgsParameter (methodDecl, context.SemanticModel);

        if (eParam == null)
        {
            return;
        }

        // Analyze method body
        if (methodDecl.Body != null)
        {
            bool setsHandled = methodDecl.Body.Statements
                                         .SelectMany (s => s.DescendantNodes ().OfType<AssignmentExpressionSyntax> ())
                                         .Any (a => IsHandledAssignment (a, eParam, context));

            if (!setsHandled)
            {
                var diag = Diagnostic.Create (_rule, methodDecl.Identifier.GetLocation ());
                context.ReportDiagnostic (diag);
            }
        }
    }

    private static SeparatedSyntaxList<ParameterSyntax> GetParameters (AnonymousFunctionExpressionSyntax lambda)
    {
        switch (lambda)
        {
            case ParenthesizedLambdaExpressionSyntax p:
                return p.ParameterList.Parameters;
            case SimpleLambdaExpressionSyntax s:
                // Simple lambda has a single parameter, wrap it in a list
                return SyntaxFactory.SeparatedList (new [] { s.Parameter });
            case AnonymousMethodExpressionSyntax a:
                return a.ParameterList?.Parameters ?? default (SeparatedSyntaxList<ParameterSyntax>);
            default:
                return default (SeparatedSyntaxList<ParameterSyntax>);
        }
    }

    private static bool IsAcceptingEvent (ExpressionSyntax expr, SyntaxNodeAnalysisContext context)
    {
        // Check if expr is b.Accepting or similar

        // Get symbol info
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo (expr);
        ISymbol? symbol = symbolInfo.Symbol;

        if (symbol == null)
        {
            return false;
        }

        // Accepting event symbol should be an event named "Accepting"
        if (symbol.Kind == SymbolKind.Event && symbol.Name == "Accepting")
        {
            return true;
        }

        return false;
    }

    private static bool IsHandledAssignment (AssignmentExpressionSyntax assignment, IParameterSymbol eParamSymbol, SyntaxNodeAnalysisContext context)
    {
        // Check if left side is "e.Handled" and right side is "true"
        // Left side should be MemberAccessExpression: e.Handled

        if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
        {
            // Check that member access expression is "e.Handled"
            ISymbol? exprSymbol = context.SemanticModel.GetSymbolInfo (memberAccess.Expression).Symbol;

            if (exprSymbol == null)
            {
                return false;
            }

            if (!SymbolEqualityComparer.Default.Equals (exprSymbol, eParamSymbol))
            {
                return false;
            }

            if (memberAccess.Name.Identifier.Text != "Handled")
            {
                return false;
            }

            // Check right side is true literal
            if (assignment.Right is LiteralExpressionSyntax literal && literal.IsKind (SyntaxKind.TrueLiteralExpression))
            {
                return true;
            }
        }

        return false;
    }
}
