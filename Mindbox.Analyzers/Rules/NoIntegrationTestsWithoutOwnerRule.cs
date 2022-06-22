using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class NoIntegrationTestsWithoutOwnerRule : AnalyzerRule, ITreeAnalyzerRule
{
    public const string IntegrationTestAttributeName = "IntegrationTest";
    public const string TestMethodAttributeName = "TestMethod";
    public const string OwnerAttributeName = "Owner";
    
    public NoIntegrationTestsWithoutOwnerRule()
            : base(
                ruleId: "Mindbox1016",
                title: "Integration test method must have Owner attribute.",
                messageFormat: "Method does not have Owner attribute",
                description: "Checks if a method with attribute \"TestMethod\" " +
                             "in a class with \"IntegrationTest\" attribute, has an \"Owner\" attribute.")
        {
        }

        public void AnalyzeTree(SyntaxTree tree, out ICollection<Diagnostic> foundProblems)
        {
            var methods = tree
                .GetRoot()
                .DescendantNodes()
                .AsParallel()
                .Where(node => node.IsKind(SyntaxKind.ClassDeclaration))
                .Where(node =>
                {
                    return (node as ClassDeclarationSyntax)
                        .AttributeLists
                        .Any(al => al.Attributes.Any(
                            a => a.Name.ToString() == IntegrationTestAttributeName));
                })
                .Select(node =>
                    node.DescendantNodes()
                        .AsParallel()
                        .Where(node => node.IsKind(SyntaxKind.MethodDeclaration))
                        .Where(node =>
                        {
                            var containsTestMethodAttr = (node as MethodDeclarationSyntax)
                                .AttributeLists
                                .Any(al =>
                                {
                                    return
                                        al.Attributes.Any(
                                            a => a.Name.ToString() == TestMethodAttributeName);
                                });
                            var containsOwnerAttr = (node as MethodDeclarationSyntax)
                                .AttributeLists
                                .Any(al =>
                                {
                                    return
                                        al.Attributes.Any(
                                            a => a.Name.ToString() == OwnerAttributeName);
                                });
                            return containsTestMethodAttr && !containsOwnerAttr;
                        }));

            foundProblems= new List<Diagnostic>();
            foreach (var subList in methods)
            {
                (foundProblems as List<Diagnostic>).AddRange(
                    subList
                        .Select(node => CreateDiagnosticForLocation(Location.Create(tree, node.FullSpan)))
                        .ToList());
            }
        }
}