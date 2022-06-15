using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules
{
	public class CustomizationsCrossReferenceProhibitedRule : AnalyzerRule, ITreeAnalyzerRule
	{
		private const string ruleId = "Mindbox1014";
		
		private const string title =
			"In Customizations project cross-links between different projects are forbidden.";
		
		private const string invalidNamespaceLinkMessage =
			"Code from one project's namespace references code from another project's namespace";
		
		private const string description =
			"Code from one Customizations project shouldn't reference code from another Customizations project. " +
			"Code in the project should only contain declarations from correct namespaces that correspond to the project";

		private const string invalidFilePathMessage =
			"Class file does not contain to Dev\\Customizations folder. " +
			"The probable cause is the rule is enabled not in the Customizations project";

		private const string invalidNamespaceMessage =
			"Defined namespace with invalid name. " +
			"For Customizations projects the correct namespace is itc.{ProjectName}. " +
			"Moreover, all project files should be located in Dev\\Customizations\\...\\{ProjectName}\\";

		private DiagnosticDescriptor InvalidClassFolderDescriptor { get; }
		private DiagnosticDescriptor InvalidNamespaceDescriptor { get; }


		public CustomizationsCrossReferenceProhibitedRule()
			: base(
				ruleId: ruleId,
				title: title,
				messageFormat: invalidNamespaceLinkMessage,
				description: description,
				isEnabledByDefault: false)
		{
			InvalidClassFolderDescriptor = new DiagnosticDescriptor(
				ruleId,
				title,
				invalidFilePathMessage,
				DefaultCategory,
				DefaultSeverity,
				false,
				description);

			InvalidNamespaceDescriptor = new DiagnosticDescriptor(
				ruleId,
				title,
				invalidNamespaceMessage,
				DefaultCategory,
				DefaultSeverity,
				false,
				description);
		}

		public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
		{
			if (!tree.FilePath.ToLower().Contains(Path.Combine("dev","customizations")))
			{
				foundProblems = new List<Diagnostic> {
					CreateDiagnosticForInvalidClassFolderMessage(tree)
				};

				return;
			}

			var filePath = tree.FilePath
				.ToLower()
				.Split(new[] { Path.Combine("dev","customizations") + Path.DirectorySeparatorChar }, StringSplitOptions.None)[1];

			if (!filePath.Contains(Path.DirectorySeparatorChar))
			{
				foundProblems = new List<Diagnostic>();
				return;
			}

			filePath = filePath.Split(new[] { Path.DirectorySeparatorChar }, 2)[1];

			if (!filePath.Contains(Path.DirectorySeparatorChar))
			{
				foundProblems = new List<Diagnostic>();
				return;
			}

			var projectName = filePath.Split(Path.DirectorySeparatorChar)[0];

			var qualifiedNamesInNamespaceItc = tree.GetRoot().DescendantNodes()
				.OfType<QualifiedNameSyntax>()
				.Select(GetTopQualifiedName)
				.Where(n => !(n.Parent is NamespaceDeclarationSyntax))
				.Where(CheckIfTopLevelNamespaceIsItc)
				.ToList();

			var crossLinkProblems = qualifiedNamesInNamespaceItc
				.Where(n =>
					!CheckIfSecondLevelNamespaceIsAllowed(n, projectName))
				.Select(name => CreateDiagnosticForLocation(Location.Create(tree, name.FullSpan)));

			var nonQualifiedNamespaceProblems = tree.GetRoot().DescendantNodes()
				.OfType<NamespaceDeclarationSyntax>()
				.Select(n => n.Name)
				.Where(n => !(n is QualifiedNameSyntax))
				.Select(name => CreateDiagnosticForInvalidNamespaceMessage(Location.Create(tree, name.FullSpan)));

			var invalidNamespaceProblems = tree.GetRoot().DescendantNodes()
				.OfType<NamespaceDeclarationSyntax>()
				.Select(n => n.Name)
				.OfType<QualifiedNameSyntax>()
				.Where(name =>
					!CheckIfTopLevelNamespaceIsItc(name) ||
					!AreEqual(GetSecondLevelNamespace(name), projectName))
				.Select(name => CreateDiagnosticForInvalidNamespaceMessage(Location.Create(tree, name.FullSpan)));

			foundProblems = crossLinkProblems
					.Union(nonQualifiedNamespaceProblems)
					.Union(invalidNamespaceProblems)
					.ToList();
		}

		private Diagnostic CreateDiagnosticForInvalidClassFolderMessage(SyntaxTree tree)
		{
			return Diagnostic.Create(InvalidClassFolderDescriptor,
						Location.Create(tree, tree.GetRoot().FullSpan));
		}

		private Diagnostic CreateDiagnosticForInvalidNamespaceMessage(Location location)
		{
			return Diagnostic.Create(InvalidNamespaceDescriptor, location);
		}

		private bool CheckIfSecondLevelNamespaceIsAllowed(QualifiedNameSyntax name, string projectName)
		{
			return new List<string> { "Commons", "Administration", "DirectCrm", "Api", projectName }
				.Any(allowedName => AreEqual(GetSecondLevelNamespace(name), allowedName));
		}

		private bool CheckIfTopLevelNamespaceIsItc(QualifiedNameSyntax name)
		{
			return AreEqual(TryGetTopLevelNamespace(name), "itc");
		}

		private QualifiedNameSyntax GetTopQualifiedName(QualifiedNameSyntax name)
		{
			if (!(name.Parent is QualifiedNameSyntax))
				return name;

			return GetTopQualifiedName((QualifiedNameSyntax)name.Parent);
		}

		private IdentifierNameSyntax TryGetTopLevelNamespace(QualifiedNameSyntax name)
		{
			return name.DescendantNodesAndSelf()
				.OfType<QualifiedNameSyntax>()
				.Select(n => n.Left)
				.OfType<IdentifierNameSyntax>()
				.SingleOrDefault();
		}

		private IdentifierNameSyntax GetSecondLevelNamespace(QualifiedNameSyntax name)
		{
			return (IdentifierNameSyntax)name.DescendantNodesAndSelf()
				.OfType<QualifiedNameSyntax>()
				.Single(n => (n.Left is IdentifierNameSyntax))
				.Right;
		}

		private bool AreEqual(IdentifierNameSyntax name, string value)
		{
			if (name == null)
				return false;

			return string.Equals(name.ToString(), value, StringComparison.CurrentCultureIgnoreCase);
		}
	}
}
