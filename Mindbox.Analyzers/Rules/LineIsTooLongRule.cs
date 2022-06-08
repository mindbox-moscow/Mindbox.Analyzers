using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers.Rules
{
	public class LineIsTooLongRule : AnalyzerRule, ISingleLineAnalyzerRule
	{
		public const int MaxLineLength = 130;
		public const int SymbolsPerTab = 4;

		public LineIsTooLongRule()
			: base(
				ruleId: "Mindbox1000",
				title: "C# code lines shouldn't be longer than " + MaxLineLength + " symbols",
				messageFormat: "Line is longer than " + MaxLineLength + " symbols",
				description: "Makes sure that lines of C# core are shorter than " + MaxLineLength + " symbols " +
					"(tabs count as " + SymbolsPerTab + " spaces, trailing spaces are not considered)")
		{
		}
		
		public void AnalyzeLine(SyntaxTree tree, TextLine line, string lineString, out ICollection<Diagnostic> foundProblems)
		{
			if (lineString.TrimEnd().Replace("\t", new string(' ', SymbolsPerTab)).Length > MaxLineLength)
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
