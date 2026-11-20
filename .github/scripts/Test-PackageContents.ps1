<#
.SYNOPSIS
    Checks what the packed packages contain: the assets, the dependencies and the analyzers.

.DESCRIPTION
    The libraries are for modern .NET only: every package has an asset for the long-term-support .NET and one for the newest, and nothing else
    (no netstandard asset, no target-framework group with another package behind it). The analyzers are packed into the folder of the Roslyn that
    they were built against, so that an older compiler gets nothing (see docs/analyzers.md). The umbrella package depends on the other two and on
    nothing else; the others depend on nothing.

.PARAMETER Packages
    The folder with the packed .nupkg files.

.EXAMPLE
    ./Test-PackageContents.ps1 -Packages ./artifacts
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Packages
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$expectedFrameworks = @('net10.0', 'net11.0')
$expectedAnalyzerFolder = 'analyzers/dotnet/roslyn5.0/cs'
$files = Get-ChildItem -Path (Resolve-Path $Packages).Path -Filter '*.nupkg'
if (-not $files) {
    throw "No .nupkg file was found in $Packages."
}

$failed = $false
foreach ($file in $files) {
    $problems = @()
    $zip = [System.IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $entries = $zip.Entries | ForEach-Object { $_.FullName }

        $frameworks = $entries |
            Where-Object { $_ -match '^lib/([^/]+)/[^/]+\.dll$' } |
            ForEach-Object { ($_ -split '/')[1] } |
            Sort-Object -Unique
        if (($frameworks -join ',') -ne ($expectedFrameworks -join ',')) {
            $problems += "lib has [$($frameworks -join ', ')] instead of [$($expectedFrameworks -join ', ')]"
        }

        $analyzers = $entries | Where-Object { $_ -match '^analyzers/.+\.dll$' }
        $isUmbrella = $file.Name -match '^Wyzely\.Functional\.\d'
        $misplaced = $analyzers | Where-Object { -not $_.StartsWith($expectedAnalyzerFolder + '/') }
        if ($misplaced) {
            $problems += "analyzers outside $($expectedAnalyzerFolder): $($misplaced -join ', ')"
        }
        if (-not $isUmbrella -and -not $analyzers) {
            $problems += 'no analyzers'
        }
        if ($isUmbrella -and $analyzers) {
            $problems += 'the umbrella package must not contain analyzers of its own'
        }

        foreach ($required in @('README.md', 'icon.png')) {
            if ($entries -notcontains $required) {
                $problems += "$required is missing"
            }
        }

        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') } | Select-Object -First 1
        $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
        try {
            [xml] $nuspec = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
        $groups = @($nuspec.package.metadata.dependencies.group)
        $groupFrameworks = $groups | Where-Object { $_ } | ForEach-Object { $_.targetFramework } | Sort-Object
        if (($groupFrameworks -join ',') -ne ($expectedFrameworks -join ',')) {
            $problems += "dependency groups [$($groupFrameworks -join ', ')] instead of [$($expectedFrameworks -join ', ')]"
        }
        $dependencies = $groups | ForEach-Object { $_.dependency } | Where-Object { $_ } | ForEach-Object { $_.id } | Sort-Object -Unique
        $expectedDependencies = if ($isUmbrella) { @('Wyzely.Functional.Maybe', 'Wyzely.Functional.Result') } else { @() }
        if (($dependencies -join ',') -ne ($expectedDependencies -join ',')) {
            $problems += "depends on [$($dependencies -join ', ')] instead of [$($expectedDependencies -join ', ')]"
        }
    } finally {
        $zip.Dispose()
    }

    if ($problems.Count -gt 0) {
        Write-Host "FAIL: $($file.Name): $($problems -join '; ')"
        $failed = $true
    } else {
        Write-Host "OK: $($file.Name)"
    }
}

if ($failed) {
    exit 1
}
