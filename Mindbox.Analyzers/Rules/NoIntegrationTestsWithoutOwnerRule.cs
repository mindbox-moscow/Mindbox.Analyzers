using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NoIntegrationTestsWithoutOwnerRule : AnalyzerRule, ITreeAnalyzerRule
{
    public const string TestMethodAttributeName = "TestMethod";
    public const string OwnerAttributeName = "Owner";
    
    public NoIntegrationTestsWithoutOwnerRule()
            : base(
                ruleId: "Mindbox1016",
                title: "Integration test method must have \"Owner\" attribute.",
                messageFormat: "Test method does not have \"Owner\" attribute.",
                description: "Checks if a method with attribute \"TestMethod\" has an \"Owner\" attribute.",
                severity: DiagnosticSeverity.Hidden)
        {
        }

        public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
        {
            foundProblems = tree
                .GetRoot()
                .DescendantNodes()
                .AsParallel()
                .Where(node => node.IsKind(SyntaxKind.MethodDeclaration))
                .Where(node =>
                {
                    var containsTestMethodAttr = (node as MethodDeclarationSyntax)?
                        .AttributeLists
                        .Any(al =>
                        {
                            return
                                al.Attributes.Any(
                                    a => a.Name.ToString().StartsWith(TestMethodAttributeName));
                        })
                        ?? false;
                    var containsOwnerAttr = (node as MethodDeclarationSyntax)?
                        .AttributeLists
                        .Any(al =>
                        {
                            return
                                al.Attributes.Any(
                                    a => a.Name.ToString().StartsWith(OwnerAttributeName));
                        })
                        ?? false;
                    return containsTestMethodAttr && !containsOwnerAttr;
                })
                .Select(node => CreateDiagnosticForLocation(Location.Create(tree, node.FullSpan)))
                .ToList();
        }
}