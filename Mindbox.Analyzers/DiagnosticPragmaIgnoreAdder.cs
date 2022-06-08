using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MindboxAnalyzers;

public class DiagnosticPragmaIgnoreAdder
{
    private readonly string[] _diagnosticIDsToSuppress;

    private const string PragmaWarningDisablePrefix = "#pragma warning disable Mindbox";
    private const string PragmaWarningRestorePrefix = "#pragma warning restore Mindbox";

    public DiagnosticPragmaIgnoreAdder(string[] diagnosticIDsToSuppress = null)
    {
        _diagnosticIDsToSuppress = diagnosticIDsToSuppress;
    }

    public Dictionary<string, string> AddPragmasToCode(IEnumerable<Diagnostic> diagnostics)
    {
        if (diagnostics is null) return new Dictionary<string, string>();
        
        var changedFiles = new Dictionary<string, string>();
        var insertionShifts = new Dictionary<string, int>(); // we insert new lines, but diagnostics lines stay as if there were no new lines

        // Add ignoring/restoring pragmas to source file. Does not actually modify files, only returns paths and new file contents.
        foreach (var diagnostic in diagnostics)
        {
            
            var descriptor = diagnostic.Descriptor.Id;

            if (_diagnosticIDsToSuppress is not null && _diagnosticIDsToSuppress.Any() && !_diagnosticIDsToSuppress.Contains(descriptor))
            {
                continue;
            }

            var lineStart = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
            var lineEnd = diagnostic.Location.GetLineSpan().EndLinePosition.Line;
            var sourceTree = diagnostic.Location.SourceTree;

            if (sourceTree is null)
            {
                throw new InvalidOperationException(
                    $"sourceTree is null for diagnostic {descriptor} ({diagnostic.Location}). It is required. Please debug to investigate.");
            }

            var sourceCode = sourceTree.GetText().ToString();

            var filename = sourceTree.FilePath;
            var filects = (changedFiles.TryGetValue(filename, out var _filects) ? _filects : sourceCode).Split('\n').ToList();

            if (!insertionShifts.ContainsKey(filename)) insertionShifts[filename] = 0;
            
            filects.Insert(insertionShifts[filename] + lineStart, $"#pragma warning disable {descriptor}");
            filects.Insert(insertionShifts[filename] + lineEnd + 2, $"#pragma warning restore {descriptor}");
            changedFiles[filename] = string.Join("\n", filects);

            insertionShifts[filename] += 2; // pragma disable and pragma restore lines
        }

        // Remove two consecutive disable/restore pragmas for one diagnostic ID
        foreach (var changedFile in changedFiles)
        {
            var filename = changedFile.Key;
            var filects = changedFile.Value.Split('\n').ToList();
            
            for (var i = 0; i < filects.Count; i++)
            {
                if (!filects[i].StartsWith(PragmaWarningRestorePrefix)) continue;
                
                var diagnosticId = filects[i].Substring(PragmaWarningRestorePrefix.Length);
                
                if (filects.Count < i + 1 || filects[i + 1] != PragmaWarningDisablePrefix + diagnosticId) continue;
                    
                filects.RemoveAt(i);
                filects.RemoveAt(i);
                i++;
            }
            
            changedFiles[filename] = string.Join("\n", filects);
        }

        return changedFiles;
    }
}