using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExhaustiveSwitchOnEnums
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExhaustiveSwitchOnEnumsCodeFixProvider)), Shared]
    public class ExhaustiveSwitchOnEnumsCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Make exhaustive";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            ExhaustiveSwitchOnEnumsAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            RegisterCodeFixForSwitch(context, root, diagnostic);
            RegisterCodeFixForSwitchExpression(context, root, diagnostic);
        }

        private static void RegisterCodeFixForSwitch(CodeFixContext context, SyntaxNode root, Diagnostic diagnostic)
        {
            var declaration = root
                .FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<SwitchStatementSyntax>()
                .FirstOrDefault();

            if (declaration == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => MakeExhaustiveAsync(context, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> MakeExhaustiveAsync(
            CodeFixContext context,
            SwitchStatementSyntax switchStatement,
            CancellationToken cancellationToken)
        {
            var existingNonDefaultSections = SwitchHelpers.GetNonDefaultSections(switchStatement);
            var existingDefaultSections = SwitchHelpers.GetDefaultSections(switchStatement);

            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return context.Document;
            }

            var switchVariable = switchStatement.Expression as IdentifierNameSyntax;
            if (switchVariable == null)
            {
                return context.Document;
            }

            var switchVariableTypeInfo = semanticModel.GetTypeInfo(switchVariable);
            var missingCases = SwitchHelpers.GetMissingEnumMembers(
                switchVariableTypeInfo,
                switchStatement,
                semanticModel);

            var missingSections = missingCases
                .Select(missingMember => SyntaxFactory.SwitchSection(
                    SyntaxFactory.List<SwitchLabelSyntax>(new[]
                    {
                        SyntaxFactory.CaseSwitchLabel(
                            SyntaxFactory.Token(SyntaxKind.CaseKeyword),
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(switchVariableTypeInfo.Type.Name),
                                SyntaxFactory.IdentifierName(missingMember.Name)
                            ),
                            SyntaxFactory.Token(SyntaxKind.ColonToken)
                        )
                    }),
                    SyntaxFactory.List<StatementSyntax>().Add(
                        SyntaxFactory.ThrowStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.ParseTypeName(nameof(NotImplementedException)),
                                SyntaxFactory.ArgumentList(),
                                null
                            )
                        )
                    )
                ))
                .ToList();

            var exhaustingSwitchStatement = SyntaxFactory
                .SwitchStatement(
                    SyntaxFactory.IdentifierName(switchVariable.Identifier)
                        .WithLeadingTrivia(switchVariable.GetLeadingTrivia())
                        .WithTrailingTrivia(switchVariable.GetTrailingTrivia())
                )
                .WithSections(
                    new SyntaxList<SwitchSectionSyntax>()
                        .AddRange(existingNonDefaultSections)
                        .AddRange(missingSections)
                        .AddRange(existingDefaultSections)
                )
                .WithLeadingTrivia(switchStatement.GetLeadingTrivia())
                .WithTrailingTrivia(switchStatement.GetTrailingTrivia());

            var oldRoot = await context.Document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(switchStatement, exhaustingSwitchStatement);

            return context.Document.WithSyntaxRoot(newRoot);
        }

        private static void RegisterCodeFixForSwitchExpression(CodeFixContext context, SyntaxNode root,
            Diagnostic diagnostic)
        {
            var declaration = root
                .FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<SwitchExpressionSyntax>()
                .FirstOrDefault();

            if (declaration == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => MakeExhaustiveAsync(context, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }


        private static async Task<Document> MakeExhaustiveAsync(
            CodeFixContext context,
            SwitchExpressionSyntax switchStatement,
            CancellationToken cancellationToken)
        {
            var existingNonDefaultSections = SwitchExpressionHelpers.GetNonDefaultArms(switchStatement);
            var existingDefaultSections = SwitchExpressionHelpers.GetDefaultArms(switchStatement);

            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return context.Document;
            }

            var switchVariable = switchStatement.GoverningExpression as IdentifierNameSyntax;
            if (switchVariable == null)
            {
                return context.Document;
            }

            var switchVariableTypeInfo = semanticModel.GetTypeInfo(switchVariable);
            var missingCases = SwitchExpressionHelpers.GetMissingEnumMembers(
                switchVariableTypeInfo,
                switchStatement,
                semanticModel);

            var missingSections = missingCases
                .Select(missingMember => SyntaxFactory.SwitchExpressionArm(
                    SyntaxFactory.ConstantPattern(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(switchVariableTypeInfo.Type.Name),
                            SyntaxFactory.IdentifierName(missingMember.Name)
                        )
                    ),
                    null,
                    SyntaxFactory.ThrowExpression(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName(nameof(NotImplementedException)),
                            SyntaxFactory.ArgumentList(),
                            null
                        )
                    )
                ))
                .ToList();

            var exhaustingSwitchStatement = SyntaxFactory
                .SwitchExpression(
                    SyntaxFactory.IdentifierName(switchVariable.Identifier)
                        .WithLeadingTrivia(switchVariable.GetLeadingTrivia())
                        .WithTrailingTrivia(switchVariable.GetTrailingTrivia())
                )
                .WithArms(
                    new SeparatedSyntaxList<SwitchExpressionArmSyntax>()
                        .AddRange(existingNonDefaultSections)
                        .AddRange(missingSections)
                        .AddRange(existingDefaultSections)
                )
                .WithLeadingTrivia(switchStatement.GetLeadingTrivia())
                .WithTrailingTrivia(switchStatement.GetTrailingTrivia());

            var oldRoot = await context.Document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(switchStatement, exhaustingSwitchStatement);

            return context.Document.WithSyntaxRoot(newRoot);
        }
    }
}