using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MindboxAnalyzers.Rules;

public class NoAdjacentWhitespaceAllowedRule : AnalyzerRule, ITreeAnalyzerRule
{
	public NoAdjacentWhitespaceAllowedRule()
		: base(
			ruleId: "Mindbox1009",
			title: "There should be no two or more consecutive whitespace characters",
			messageFormat: "Two or more consecutive whitespace characters. Please remove the extra",
			description: "Makes sure that there should be no two or more consecutive whitespace characters")
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = tree
			.GetRoot()
			.DescendantNodes(descendIntoTrivia: true)
			.Where(node => node.IsKind(SyntaxKind.WhitespaceTrivia))
			.Where(node => node.Span.Length > 1)
			.Select(node => CreateDiagnosticForLocation(node.GetLocation()))
			.ToList();
	}
}
