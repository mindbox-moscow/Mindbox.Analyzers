using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NamedObjectModelConfigurationRegisterProhibitedRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	public NamedObjectModelConfigurationRegisterProhibitedRule()
		: base(
			ruleId: "Mindbox1026",
			title: "Forbids to register named objects through INamedObjectModelConfiguration.Register",
			messageFormat: "Cannot use INamedObjectModelConfiguration.Register. Use IServiceCollection.AddXXX in Startup.cs.",
			description: "Forbids to register named objects through INamedObjectModelConfiguration.Register"
		)
	{
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		var apcNames = new[] { "INamedObjectModelConfiguration", "NamedObjectModelConfiguration" };

		foundProblems = model.SyntaxTree
			.GetRoot()
			.DescendantNodes()
			.OfType<GenericNameSyntax>()
			.Where(node => node.Identifier.Value!.ToString() == "Register")
			.Select(node => node.Parent!.ChildNodes().ElementAt(0))
			.Where(node => apcNames.Contains((model.GetTypeInfo(node).Type as INamedTypeSymbol)?.Name))
			.Select(node => CreateDiagnosticForLocation(node.Parent!.GetLocation()))
			.ToList();
	}
}