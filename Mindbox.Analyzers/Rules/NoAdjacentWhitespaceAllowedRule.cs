using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MindboxAnalyzers.Rules
{
	public class NoAdjacentWhitespaceAllowedRule : AnalyzerRule, ITreeAnalyzerRule
	{
		public NoAdjacentWhitespaceAllowedRule()
			: base(
				ruleId: "Mindbox1009",
				title: "В форматировании кода не должно быть двух пробельных символов подряд",
				messageFormat: "Два пробельных символа подряд",
				description: "Проверяет, что в форматировании кода нет двух пробельных символов подряд")
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
}
