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
        private const string SolutionPath = @"/Users/savelijsivkov/RiderProjects/ConsoleApp1/ConsoleApp1.sln";
        
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

            int insertionShift = 0; // we insert new lines, but diagnostics lines stay as if there were no new lines

            Console.WriteLine($"Found {diagnostics.Count} diagnostic messages{(diagnostics.Count > 0 ? ':' : '.')}");
            foreach (var diagnostic in diagnostics)
            {
                var descriptor = diagnostic.Descriptor.Id;
                var message = diagnostic.GetMessage();
                var lineStart = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
                var lineEnd = diagnostic.Location.GetLineSpan().EndLinePosition.Line;
                var codeInQuestion = diagnostic.Location.SourceTree.GetText().ToString().Split('\n')
                    .Select(x => x.Trim()).Skip(lineStart).Take(lineEnd - lineStart + 1);
                Console.WriteLine($"  - {descriptor} \"{message}\" on lin{(lineStart == lineEnd ? $"e {lineStart}" : $"es {lineStart} to {lineEnd} inclusive")}");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Join("\n", codeInQuestion.Select(x => "    " + x)));
                Console.ResetColor();

                var filename = diagnostic.Location.SourceTree.FilePath;
                var filects = new List<string>(File.ReadAllLines(filename));
                filects.Insert(insertionShift + lineStart, $"#pragma warning disable {descriptor}");
                filects.Insert(insertionShift + lineEnd + 2, $"#pragma warning restore {descriptor}");
                File.WriteAllLines(filename, filects);

                insertionShift += 2; // pragma disable and pragma restore lines
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