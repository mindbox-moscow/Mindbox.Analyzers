using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers.Rules;

public class TabsShouldOnlyBeUsedForIndentationRule : AnalyzerRule, ISingleLineAnalyzerRule
{
	public TabsShouldOnlyBeUsedForIndentationRule()
		: base(
			ruleId: "Mindbox1008",
			title: "Символ табуляции должен использоваться только для отступов в начале строки или в конце строки",
			messageFormat: "Табуляции используется посреди строки",
			description: "Символ табуляции должен использоваться только для отступов в начале строки или в конце строки")
	{
	}

	public void AnalyzeLine(SyntaxTree tree, TextLine line, string lineString, out ICollection<Diagnostic> foundProblems)
	{
		if (lineString.Trim().Contains("\t"))
		{
			foundProblems = new[]
			{
				CreateDiagnosticForLocation(Location.Create(tree, line.Span))
			};
		}
		else
		{
			foundProblems = null;
		}
	}
}
