using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers.Rules;

public class OnlyTabsShouldBeUsedForIndentationRule : AnalyzerRule, ISingleLineAnalyzerRule
{
	public OnlyTabsShouldBeUsedForIndentationRule()
		: base(
			ruleId: "Mindbox1002",
			title: "Code should be indented with tabs only",
			messageFormat: "Indentation with spaces is prohibited",
			description: "Makes sure that code is indented with tabs only")
	{
	}

	public void AnalyzeLine(SyntaxTree tree, TextLine line, string lineString, out ICollection<Diagnostic> foundProblems)
	{
		var spacesAtTheStartEncountered = false;

		foreach (var character in lineString)
		{
			if (character != '\t' && char.IsWhiteSpace(character))
				spacesAtTheStartEncountered = true;
			else
				break;
		}

		if (spacesAtTheStartEncountered)
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
