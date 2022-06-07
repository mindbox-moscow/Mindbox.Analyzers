using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules
{
	public class ModelApplicationHostControllerServiceLocatorProhibitedRule : AnalyzerRule, ISemanticModelAnalyzerRule
	{
		public ModelApplicationHostControllerServiceLocatorProhibitedRule()
			: base(
				ruleId: "Mindbox1025",
				title: "Запрет на использование MAHC как сервис-локатора",
				messageFormat: "Нельзя использовать Get у ModelApplicationHostController'a. Используйте DI через конструктор.",
				description: "Запрещает вызывать Get<> у ModelApplicationHostController"
			)
		{
		}

		public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
		{
			var apcNames = new[] {"ModelApplicationHostController", "ApplicationHostController"};
			
			foundProblems = model.SyntaxTree
				.GetRoot()
				.DescendantNodes()
				.OfType<GenericNameSyntax>()
				.Where(node => node.Identifier.Value!.ToString() == "Get")
				.Select(node => node.Parent!.ChildNodes().ElementAt(0))
				.Where(node => apcNames.Contains((model.GetTypeInfo(node).Type as INamedTypeSymbol)?.Name))
				.Select(node => CreateDiagnosticForLocation(node.Parent!.GetLocation()))
				.ToList();
		}
	}
}
