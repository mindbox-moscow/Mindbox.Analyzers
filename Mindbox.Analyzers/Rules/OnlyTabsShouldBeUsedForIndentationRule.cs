using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers.Rules;

public class OnlyTabsShouldBeUsedForIndentationRule : AnalyzerRule, ISingleLineAnalyzerRule
{
	public OnlyTabsShouldBeUsedForIndentationRule()
		: base(
			ruleId: "Mindbox1002",
			title: "Отступы должны формироваться только с помощью табуляции",
			messageFormat: "Для отступа используются пробелы",
			description: "Проверяет, что отступы формируются только с помощью табуляции")
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
