using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers.Rules;

public class No3AdjacentEmptyLinesRule : AnalyzerRule, ITextAnalyzerRule
{
	public No3AdjacentEmptyLinesRule()
		: base(
			ruleId: "Mindbox1001",
			title: "There should never be three or more consecutive empty lines",
			messageFormat: "Three or more consecutive empty lines. Please remove the extra.",
			description: "Makes sure that there's no three or more consecutive empty lines")
	{
	}

	public void AnalyzeText(SyntaxTree tree, SourceText text, out ICollection<Diagnostic> foundProblems)
	{
		var result = new List<Diagnostic>();

		var emptyLineCount = 0;
		var emptyLineStart = default(int);
		foreach (var line in text.Lines)
		{
			if (string.IsNullOrWhiteSpace(line.ToString()))
			{
				if (emptyLineCount == 0)
					emptyLineStart = line.Start;
				emptyLineCount++;
				if (emptyLineCount == 3)
					result.Add(CreateDiagnosticForLocation(
						Location.Create(tree, TextSpan.FromBounds(emptyLineStart, line.End))));
			}
			else
			{
				emptyLineCount = 0;
			}
		}

		foundProblems = result.Any()
			? result
			: null;
	}
}
