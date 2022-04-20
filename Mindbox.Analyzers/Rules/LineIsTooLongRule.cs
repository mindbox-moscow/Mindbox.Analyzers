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
				title: "Строки C# кода не должны быть длинее " + MaxLineLength + " символов",
				messageFormat: "Строка длинее " + MaxLineLength + " символов",
				description: "Проверяет, что строки C# кода не длинее " + MaxLineLength + " символов " +
					"(табуляция считается за " + SymbolsPerTab + " символа, пробельные символы в конце строки игнорируются)")
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
