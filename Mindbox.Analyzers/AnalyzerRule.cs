using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MindboxAnalyzers;

public interface IAnalyzerRule
{
	DiagnosticDescriptor DiagnosticDescriptor { get; }
}

public interface ITreeAnalyzerRule : IAnalyzerRule
{
	void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems);
}

public interface ITextAnalyzerRule : IAnalyzerRule
{
	void AnalyzeText(SyntaxTree tree, SourceText text, out ICollection<Diagnostic> foundProblems);
}

public interface ISingleLineAnalyzerRule : IAnalyzerRule
{
	/// <summary>
	/// Проанлазировать строку
	/// </summary>
	/// <param name="tree">Синатксическое дерево</param>
	/// <param name="line">Строка файла</param>
	/// <param name="lineString">Строковое представление строки файла</param>
	/// <param name="foundProblems">Коллекция найденных несоответствий правилу. <c>null</c>, если всё хорошо.</param>
	void AnalyzeLine(SyntaxTree tree, TextLine line, string lineString, out ICollection<Diagnostic> foundProblems);
}

public abstract class AnalyzerRule : IAnalyzerRule
{
	protected const string DefaultCategory = "Mindbox";
	protected const DiagnosticSeverity DefaultSeverity = DiagnosticSeverity.Warning;

	public DiagnosticDescriptor DiagnosticDescriptor { get; }

	protected AnalyzerRule(
		string ruleId,
		string title,
		string messageFormat,
		string description,
		string category = DefaultCategory,
		DiagnosticSeverity severity = DefaultSeverity,
		bool isEnabledByDefault = true)
	{
		DiagnosticDescriptor = new DiagnosticDescriptor(
			ruleId,
			title,
			messageFormat,
			category,
			severity,
			isEnabledByDefault,
			description);
	}

	protected Diagnostic CreateDiagnosticForLocation(Location location)
	{
		return Diagnostic.Create(DiagnosticDescriptor, location);
	}
}