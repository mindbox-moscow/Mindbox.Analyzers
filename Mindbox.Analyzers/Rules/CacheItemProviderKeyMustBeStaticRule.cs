using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class CacheItemProviderKeyMustBeStaticRule : AnalyzerRule, ITreeAnalyzerRule
{
	public CacheItemProviderKeyMustBeStaticRule()
		: base(
			ruleId: "Mindbox1015",
			title: "Properties and fields of type *CacheItemProviderKey should be static",
			messageFormat: "Properties and fields of type *CacheItemProviderKey should be static",
			description: "Checks that properties and fields of type *CacheItemProviderKey should be static in order to avoid memory leaks."
		)
	{
	}

	public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
	{
		if (!tree.GetText().ToString().Contains("CacheItemProviderKey"))
		{
			foundProblems = Array.Empty<Diagnostic>();
			return;
		}

		foundProblems = tree
			.GetRoot()
			.DescendantNodes()
			.Where(node => node.IsKind(SyntaxKind.PropertyDeclaration) || node.IsKind(SyntaxKind.FieldDeclaration))
			.OfType<MemberDeclarationSyntax>()
			.Where(member => member.Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.StaticKeyword)))
			.Select(
				member => member switch
				{
					PropertyDeclarationSyntax property => property.Type,
					FieldDeclarationSyntax field => field.Declaration.Type,
					_ => null
				}
			)
			.Where(type => type?.Parent != null)
			.Where(type => type is IdentifierNameSyntax identifier && identifier.Identifier.Text.EndsWith("CacheItemProviderKey"))
			.Select(type => CreateDiagnosticForLocation(type.Parent!.GetLocation()))
			.ToList();
	}
}