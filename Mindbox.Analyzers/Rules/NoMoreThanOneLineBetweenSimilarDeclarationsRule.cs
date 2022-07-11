using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NoMoreThanOneLineBetweenSimilarDeclarationsRule : AnalyzerRule, ITreeAnalyzerRule
{
	public NoMoreThanOneLineBetweenSimilarDeclarationsRule()
		: base(
			ruleId: "Mindbox1003",
			title: "There should not be more than one line between members of one kind and visibility " +
			       "that match by static/instance",
			messageFormat: "More than one line between members of one kind and visibility",
			description: "Makes sure that there should not be more than one line between members of one kind " +
			             "and visibility that match by static/instance")
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
				if (previous?.RawKind == current.RawKind)
				{
					SyntaxTokenList previousModifiers;
					SyntaxTokenList currentModifiers;

					switch (current.Kind())
					{
						case SyntaxKind.PropertyDeclaration:
							previousModifiers = ((PropertyDeclarationSyntax)previous).Modifiers;
							currentModifiers = ((PropertyDeclarationSyntax)current).Modifiers;
							break;
						case SyntaxKind.FieldDeclaration:
							previousModifiers = ((FieldDeclarationSyntax)previous).Modifiers;
							currentModifiers = ((FieldDeclarationSyntax)current).Modifiers;
							break;
						default:
							continue;
					}

					if (previousModifiers.Count == currentModifiers.Count &&
						previousModifiers.All(mprev => currentModifiers.Any(mcurr => mcurr.RawKind == mprev.RawKind)))
					{
						var combinedTrivia = previous.GetTrailingTrivia()
							.Concat(current.GetLeadingTrivia())
							.ToList();

						var linebreaksCount = combinedTrivia
							.Count(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));

						if (linebreaksCount > 2 && combinedTrivia
							.All(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)
								|| trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
						{
							foundProblems.Add(CreateDiagnosticForLocation(current.GetLocation()));
						}
					}
				}

				previous = current;
			}
		}
	}
}
