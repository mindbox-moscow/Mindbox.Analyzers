﻿using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules
{
	public class NoRegionsInsideMethodRule : AnalyzerRule, ITreeAnalyzerRule
	{
		public NoRegionsInsideMethodRule()
			: base(
				ruleId: "Mindbox2000",
				title: "Regions shouldn't be used inside a method",
				messageFormat: "Regions shouldn't be used inside a method",
				description: "Makes sure that #region directives aren't used inside a method")
		{
		}

		public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
		{
			var regionUsages = tree.GetCompilationUnitRoot()
				.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Where(method => method.Body != null)
				.SelectMany(method => method.Body
					.DescendantNodes(descendIntoTrivia: true)
					.Where(x => x.IsStructuredTrivia)
					.Where(x => x.IsKind(SyntaxKind.RegionDirectiveTrivia))
					.Select(region => new
					{
						ParentClass = method.Parent as ClassDeclarationSyntax,
						Region = region
					}))
				.Where(region => region.ParentClass == null ||
								!region.ParentClass.AttributeLists
									.Any(list => list.Attributes.Any(x => x.Name.ToString() == "TestClass")));

			foundProblems = regionUsages
				.Select(regionUsage => CreateDiagnosticForLocation(Location.Create(tree, regionUsage.Region.FullSpan)))
				.ToList();
		}
	}
}
