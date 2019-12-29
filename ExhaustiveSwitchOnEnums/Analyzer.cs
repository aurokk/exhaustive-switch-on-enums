using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExhaustiveSwitchOnEnums
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExhaustiveSwitchOnEnumsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ExhaustiveSwitchOnEnums";

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.AnalyzerTitle),
            Resources.ResourceManager,
            typeof(Resources));

        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(Resources.AnalyzerMessageFormat),
            Resources.ResourceManager,
            typeof(Resources));

        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(Resources.AnalyzerDescription),
            Resources.ResourceManager,
            typeof(Resources));

        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(
                GeneratedCodeAnalysisFlags.Analyze |
                GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSwitch, SyntaxKind.SwitchStatement);
            context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
        }

        private void AnalyzeSwitch(SyntaxNodeAnalysisContext context)
        {
            var typedSwitchDeclaration = (SwitchStatementSyntax) context.Node;

            var switchVariableTypeInfo = context.SemanticModel.GetTypeInfo(typedSwitchDeclaration.Expression);
            if (switchVariableTypeInfo.Type.TypeKind != TypeKind.Enum)
            {
                // switch variable is not an enum
                return;
            }

            var defaultSection = SwitchHelpers.GetDefaultSections(typedSwitchDeclaration);
            var throwStatement = defaultSection.Single()
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(x => x.Expression)
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(x => context.SemanticModel.GetTypeInfo(x).Type.Name == nameof(ArgumentOutOfRangeException));

            if (!throwStatement.Any())
            {
                // doesn't contain a throw expression of ArgumentOutOfRangeException
                return;
            }

            var missingMembers = SwitchHelpers.GetMissingEnumMembers(
                switchVariableTypeInfo,
                typedSwitchDeclaration,
                context.SemanticModel);
            if (!missingMembers.Any())
            {
                // every case is handled
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }

        private void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
        {
            var typedSwitchDeclaration = (SwitchExpressionSyntax) context.Node;

            var switchVariableTypeInfo = context.SemanticModel.GetTypeInfo(typedSwitchDeclaration.GoverningExpression);
            if (switchVariableTypeInfo.Type.TypeKind != TypeKind.Enum)
            {
                // switch variable is not an enum
                return;
            }

            var defaultArm = SwitchExpressionHelpers.GetDefaultArms(typedSwitchDeclaration);
            var throwStatement = defaultArm.Single()
                .DescendantNodes()
                .OfType<ThrowExpressionSyntax>()
                .Select(x => x.Expression)
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(x => context.SemanticModel.GetTypeInfo(x).Type.Name == nameof(ArgumentOutOfRangeException));

            if (!throwStatement.Any())
            {
                // doesn't contain a throw expression of ArgumentOutOfRangeException
                return;
            }

            var missingMembers = SwitchExpressionHelpers.GetMissingEnumMembers(
                switchVariableTypeInfo,
                typedSwitchDeclaration,
                context.SemanticModel);
            if (!missingMembers.Any())
            {
                // every case is handled
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}