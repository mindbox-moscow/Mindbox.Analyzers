using System.Collections.Immutable;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using MindboxAnalyzers;

namespace Mindbox.Analyzers.ConsoleApplication
{
    internal class Program
    {
        private const string SolutionPath = @"/Users/savelijsivkov/RiderProjects/ConsoleApp1/ConsoleApp1.sln";
        
        private static readonly string[] DiagnosticIDsToSuppress = { "Mindbox1025", "Mindbox1026" };

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

            Console.WriteLine($"Found {diagnostics.Count} diagnostic messages{(diagnostics.Count > 0 ? ':' : '.')}");
            foreach (var diagnostic in diagnostics)
            {
                var descriptor = diagnostic.Descriptor.Id;

                if (DiagnosticIDsToSuppress.Any() && !DiagnosticIDsToSuppress.Contains(descriptor))
                {
                    Console.WriteLine($"Ignoring diagnostic {descriptor} as it is not in the list of Diagnostic IDs to suppress");
                    continue;
                }
                
                var message = diagnostic.GetMessage();
                var lineStart = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
                var lineEnd = diagnostic.Location.GetLineSpan().EndLinePosition.Line;
                var sourceTree = diagnostic.Location.SourceTree;

                if (sourceTree is null)
                {
                    throw new InvalidOperationException($"sourceTree is null for diagnostic {descriptor} ({diagnostic.Location}). It is required. Please debug to investigate.");
                }
                
                var codeInQuestion = sourceTree.GetText().ToString().Split('\n')
                    .Select(x => x.Trim()).Skip(lineStart).Take(lineEnd - lineStart + 1);
                Console.WriteLine($"  - {descriptor} \"{message}\" on lin{(lineStart == lineEnd ? $"e {lineStart}" : $"es {lineStart} to {lineEnd} inclusive")}");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Join("\n", codeInQuestion.Select(x => "    " + x)));
                Console.ResetColor();
            }

            var changedFiles = new DiagnosticPragmaIgnoreAdder(DiagnosticIDsToSuppress).AddPragmasToCode(diagnostics);
            foreach (var (filename, newFileContents) in changedFiles)
            {
                File.WriteAllText(filename, newFileContents);
            }

            Console.WriteLine($"FINISHED {DateTime.Now}. Updated {changedFiles.Count} files.");
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