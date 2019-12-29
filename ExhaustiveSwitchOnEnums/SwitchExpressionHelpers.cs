using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExhaustiveSwitchOnEnums
{
    public static class SwitchExpressionHelpers
    {
        public static IEnumerable<SwitchExpressionArmSyntax> GetDefaultArms(SwitchExpressionSyntax switchStatement)
        {
            var existingDefaultSections = switchStatement.Arms
                .Where(arm => arm.Pattern is DiscardPatternSyntax)
                .ToList();
            return existingDefaultSections;
        }

        public static IEnumerable<SwitchExpressionArmSyntax> GetNonDefaultArms(SwitchExpressionSyntax switchStatement)
        {
            var existingNonDefaultSections = switchStatement.Arms
                .Where(arm => !(arm.Pattern is DiscardPatternSyntax))
                .ToList();
            return existingNonDefaultSections;
        }

        public static IEnumerable<ISymbol> GetMissingEnumMembers(
            TypeInfo switchVariableTypeInfo,
            SwitchExpressionSyntax switchStatement,
            SemanticModel semanticModel)
        {
            var enumMembers = switchVariableTypeInfo.Type.GetMembers()
                .Where(x => x.Kind == SymbolKind.Field)
                .ToArray();

            var caseSwitchLabels = switchStatement.Arms
                .SelectMany(arm => arm.DescendantNodes())
                .OfType<MemberAccessExpressionSyntax>()
                .Select(memberAccess => semanticModel.GetSymbolInfo(memberAccess).Symbol)
                .ToArray();

            return enumMembers
                .Where(x => !caseSwitchLabels.Contains(x))
                .ToList();
        }
    }
}