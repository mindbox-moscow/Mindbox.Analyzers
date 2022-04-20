param($installPath, $toolsPath, $package, $project)

$analyzersPath = Join-Path (Join-Path (Split-Path -Path $toolsPath -Parent) "lib" ) * -Resolve

# Install the language agnostic analyzers.
if (Test-Path $analyzersPath)
{
    foreach ($analyzerFilePath in Get-ChildItem $analyzersPath -Filter *.dll)
    {
        if($project.Object.AnalyzerReferences)
        {
            $project.Object.AnalyzerReferences.Add($analyzerFilePath.FullName)
        }
    }
}
