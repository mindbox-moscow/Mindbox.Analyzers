using Microsoft.CodeAnalysis;

namespace MindboxAnalyzers;

public class DiagnosticPragmaIgnoreAdder
{
    private readonly string[]? _diagnosticIDsToSuppress;

    private const string PragmaWarningDisablePrefix = "#pragma warning disable Mindbox";
    private const string PragmaWarningRestorePrefix = "#pragma warning restore Mindbox";

    public DiagnosticPragmaIgnoreAdder(string[]? diagnosticIDsToSuppress = null)
    {
        _diagnosticIDsToSuppress = diagnosticIDsToSuppress;
    }

    public Dictionary<string, string> AddPragmasToCode(IEnumerable<Diagnostic>? diagnostics)
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
            var fileContents = (changedFiles.TryGetValue(filename, out var changedFileContents) ? changedFileContents : sourceCode)
                .Split('\n').ToList();

            if (!insertionShifts.ContainsKey(filename)) insertionShifts[filename] = 0;
            
            fileContents.Insert(insertionShifts[filename] + lineStart, $"#pragma warning disable {descriptor}");
            fileContents.Insert(insertionShifts[filename] + lineEnd + 2, $"#pragma warning restore {descriptor}");
            changedFiles[filename] = string.Join("\n", fileContents);

            insertionShifts[filename] += 2; // pragma disable and pragma restore lines
        }

        // Remove two consecutive disable/restore pragmas for one diagnostic ID
        foreach (var changedFile in changedFiles)
        {
            var filename = changedFile.Key;
            var fileContents = changedFile.Value.Split('\n').ToList();
            
            for (var i = 0; i < fileContents.Count; i++)
            {
                if (!fileContents[i].StartsWith(PragmaWarningRestorePrefix)) continue;
                
                var diagnosticId = fileContents[i].Substring(PragmaWarningRestorePrefix.Length);
                
                if (fileContents.Count < i + 1 || fileContents[i + 1] != PragmaWarningDisablePrefix + diagnosticId) continue;
                    
                fileContents.RemoveAt(i);
                fileContents.RemoveAt(i);
                i++;
            }
            
            changedFiles[filename] = string.Join("\n", fileContents);
        }

        return changedFiles;
    }
}