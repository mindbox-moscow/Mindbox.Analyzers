using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NameOfInColumnAttributesRequiredRule : AnalyzerRule, ITreeAnalyzerRule
{
	public NameOfInColumnAttributesRequiredRule()
		: base(
			ruleId: "Mindbox2002",
			title: "Вместо указания имени поля строкой должен использоваться оператор nameof",
			messageFormat: "Название поля строкой вместо использования nameof",
			description: "Вместо указания имени поля строкой должен использоваться оператор nameof")
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = tree
			.GetRoot()
			.DescendantNodes(node => !node.IsKind(SyntaxKind.MethodDeclaration))
			.OfType<PropertyDeclarationSyntax>()
			.SelectMany(property => property.AttributeLists
				.SelectMany(list => list.Attributes
					.Where(attr => attr.Name.ToString() is "Column" or "Association")
					.Where(attr => attr.ArgumentList != null)))
			.SelectMany(columnAttribute => columnAttribute
				.ArgumentList
				.Arguments
				.Where(argument => argument.NameEquals.Name.Identifier.Text is "Storage"
											or "ThisKey"))
			.Where(storageArgument => storageArgument != null)
			.Where(storageArgument => storageArgument.Expression.IsKind(SyntaxKind.StringLiteralExpression))
			.Select(storageArgument => CreateDiagnosticForLocation(storageArgument.Expression.GetLocation()))
			.ToList();
	}
}
