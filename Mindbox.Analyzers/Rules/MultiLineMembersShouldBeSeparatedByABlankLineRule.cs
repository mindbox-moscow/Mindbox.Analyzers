using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class MultiLineMembersShouldBeSeparatedByABlankLineRule : AnalyzerRule, ITreeAnalyzerRule
{
	public MultiLineMembersShouldBeSeparatedByABlankLineRule()
		: base(
			ruleId: "Mindbox1004",
			title: "Multiline methods, properties, inner classes, structs, enums and interfaces " +
				   "should be separated from other members with at least one empty line",
			messageFormat: "Multiline class member should be separated with at least one empty line",
			description: "Makes sure that multiline methods, properties, inner classes, structs, enums and " +
						 "interfaces are separated from other members with at least one empty line")
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		foundProblems = new List<Diagnostic>();

		var typeDeclarations = tree
			.GetRoot()
			.DescendantNodes(node => !node.IsKind(SyntaxKind.MethodDeclaration))
			.OfType<TypeDeclarationSyntax>();

		foreach (var typeDeclaration in typeDeclarations)
		{
			var declarationMembers = typeDeclaration
				.Members
				.Select(x => x);

			MemberDeclarationSyntax previous = null;
			foreach (var current in declarationMembers)
			{
				if (previous != null && ShouldMemberBeSeparated(current))
				{
					var combinedTrivia = previous.GetTrailingTrivia()
						.Concat(current.GetLeadingTrivia())
						.ToList();

					var linebreaksCount = combinedTrivia
						.Count(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));

					if (linebreaksCount < 2)
						foundProblems.Add(CreateDiagnosticForLocation(current.GetLocation()));
				}

				previous = current;
			}
		}
	}

	private bool ShouldMemberBeSeparated(MemberDeclarationSyntax current)
	{
		return current.IsKind(SyntaxKind.ClassDeclaration) ||
				current.IsKind(SyntaxKind.StructDeclaration) ||
				current.IsKind(SyntaxKind.InterfaceDeclaration) ||
				current.IsKind(SyntaxKind.EnumDeclaration) ||
				(current.IsKind(SyntaxKind.MethodDeclaration) &&
					(current.DescendantTrivia().Count(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia))
					> current.GetTrailingTrivia().Count(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)))) ||
				(current.IsKind(SyntaxKind.PropertyDeclaration) &&
					(current.DescendantTrivia().Count(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia))
					> current.GetTrailingTrivia().Count(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia))));
	}
}