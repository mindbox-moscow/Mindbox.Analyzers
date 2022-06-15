using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers.Rules
{
	public class TabsShouldOnlyBeUsedForIndentationRule : AnalyzerRule, ISingleLineAnalyzerRule
	{
		public TabsShouldOnlyBeUsedForIndentationRule()
			: base(
				ruleId: "Mindbox1008",
				title: "Tabs should only be used in the beginning or the end of a line",
				messageFormat: "Tabs are used in the middle of a line",
				description: "Makes sure that tabs are only used in the beginning or the end of a line")
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
}
