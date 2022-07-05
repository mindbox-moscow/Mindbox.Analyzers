using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class CustomUtcDateTimeConvertingRequiredRule : AnalyzerRule, ITreeAnalyzerRule
{
	public CustomUtcDateTimeConvertingRequiredRule()
		: base(
			ruleId: "Mindbox1010",
			title: "Для конвертации даты и времени должна использоваться TimeZoneHistory",
			messageFormat: "Использование ToLocalTime и ToUniversalTime запрещено, используйте TimeZoneHistory",
			description: "Использование ToLocalTime и ToUniversalTime запрещено, используйте TimeZoneHistory")
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = tree
			.GetRoot()
			.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(x => x.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
			.Where(x => ((MemberAccessExpressionSyntax)x.Expression).Name.Identifier.ToString() is "ToUniversalTime"
						or "ToLocalTime")
			.Select(node => CreateDiagnosticForLocation(Location.Create(tree, node.FullSpan)))
			.ToList();
	}
}
