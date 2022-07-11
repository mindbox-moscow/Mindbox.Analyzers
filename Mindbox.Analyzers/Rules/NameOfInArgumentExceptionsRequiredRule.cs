using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NameOfInArgumentExceptionsRequiredRule : AnalyzerRule, ITreeAnalyzerRule
{
	public NameOfInArgumentExceptionsRequiredRule()
		: base(
			ruleId: "Mindbox2001",
			title: "Instead of specifying the argument name as a string nameof operator should be used",
			messageFormat: "Use nameof instead of parameter name as string",
			description: "Instead of specifying the argument name as a string nameof operator should be used")
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		var methodsAndConstructors = tree
			.GetRoot()
			.DescendantNodes()
			.Where(node => node.IsKind(SyntaxKind.MethodDeclaration) || node.IsKind(SyntaxKind.ConstructorDeclaration));

		foundProblems = methodsAndConstructors
			.SelectMany(m => m.DescendantNodes()
				.Where(x => x.IsKind(SyntaxKind.ObjectCreationExpression))
				.OfType<ObjectCreationExpressionSyntax>()
				.Where(x => x.Type.IsKind(SyntaxKind.IdentifierName))
				.Select(x => new
				{
					Node = x,
					Type = x.Type as IdentifierNameSyntax
				}))
			.Select(x =>
			{
				ArgumentSyntax result = null;
				if (x.Type.ToString() == "ArgumentNullException")
				{
					if (x.Node.ArgumentList.Arguments.Count >= 1
						&& x.Node.ArgumentList.Arguments[0].Expression
							.IsKind(SyntaxKind.StringLiteralExpression))
					{
						result = x.Node.ArgumentList.Arguments[0];
					}
				}
				else if (x.Type.ToString() == "ArgumentException")
				{
					if (x.Node.ArgumentList.Arguments.Count >= 2
						&& x.Node.ArgumentList.Arguments[1].Expression
							.IsKind(SyntaxKind.StringLiteralExpression))
					{
						result = x.Node.ArgumentList.Arguments[1];
					}
				}

				return result;
			})
			.Where(x => x != null)
			.Select(argumentNode => CreateDiagnosticForLocation(argumentNode.GetLocation()))
			.ToList();
	}
}