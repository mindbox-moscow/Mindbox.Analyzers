using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using MindboxAnalyzers;

namespace Mindbox.Analyzers.ConsoleApplication
{
    internal class Program
    {
        private const string SolutionPath = @"/Users/tsyrulnikov/Mindbox/DirectCRM/DirectCrmTrunk.sln";
        
        public static void Main(string[] args)
        {
            Console.WriteLine($"Before solution opened: {DateTime.Now}");

            MSBuildLocator.RegisterDefaults();

            var workspace = MSBuildWorkspace.Create();

            workspace.LoadMetadataForReferencedProjects = true;

            workspace.WorkspaceFailed += (sender, eventArgs) =>
            {
                Console.Error.WriteLine($"{eventArgs.Diagnostic.Kind}: {eventArgs.Diagnostic.Message}");
                Console.Error.WriteLine();
            };

            var originalSolution = workspace.OpenSolutionAsync(SolutionPath).Result;

            Console.WriteLine($"Before compilation {DateTime.Now}");

            var diagnostics = GetSortedDiagnosticsFromDocuments(new MindboxAnalyzer(), originalSolution);

            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine(diagnostic.GetMessage());
            }

            Console.WriteLine($"FINISHED {DateTime.Now}");
            Console.ReadKey();
        }

        private static List<Diagnostic> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Solution solution)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var compilation = project.GetCompilationAsync().Result;
                if (compilation == null) continue;
                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        var documents = project.Documents.ToArray();
                        for (var i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }
            
            return diagnostics;
        }
    }
}