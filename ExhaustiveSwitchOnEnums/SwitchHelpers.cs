using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExhaustiveSwitchOnEnums
{
    public static class SwitchHelpers
    {
        public static IEnumerable<SwitchSectionSyntax> GetDefaultSections(SwitchStatementSyntax switchStatement)
        {
            var existingDefaultSections = switchStatement.Sections
                .Where(section => section.DescendantNodes().Any(node => node is DefaultSwitchLabelSyntax))
                .ToList();
            return existingDefaultSections;
        }

        public static IEnumerable<SwitchSectionSyntax> GetNonDefaultSections(SwitchStatementSyntax switchStatement)
        {
            var existingNonDefaultSections = switchStatement.Sections
                .Where(section => !section.DescendantNodes().Any(node => node is DefaultSwitchLabelSyntax))
                .ToList();
            return existingNonDefaultSections;
        }

        public static IEnumerable<ISymbol> GetMissingEnumMembers(
            TypeInfo switchVariableTypeInfo,
            SwitchStatementSyntax switchStatement,
            SemanticModel semanticModel)
        {
            var enumMembers = switchVariableTypeInfo.Type.GetMembers()
                .Where(x => x.Kind == SymbolKind.Field)
                .ToArray();

            var caseSwitchLabels = switchStatement.Sections
                .SelectMany(section => section.Labels)
                .Select(label => label.DescendantNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault())
                .Where(memberAccess => memberAccess != null)
                .Select(memberAccess => semanticModel.GetSymbolInfo(memberAccess).Symbol)
                .ToArray();

            return enumMembers
                .Where(x => !caseSwitchLabels.Contains(x))
                .ToList();
        }
    }
}