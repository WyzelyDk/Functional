<#
.SYNOPSIS
    Finds the NuGet package that a release tag belongs to, or checks that every package can be released unambiguously.

.DESCRIPTION
    Every package in this repository is released by its own tag, for example `result-v1.2.3`. The prefix of that tag (`result-v`) is the
    MinVerTagPrefix of the package's project, so the project file is the single source of truth: nothing has to be kept in sync by hand.

    -Tag       Resolves a tag to its project, package id and version, and checks that the tag is a valid release tag.
    -Validate  Checks that every packable project has its own, well-formed tag prefix (run in CI, so a new package cannot be added in a
               way that makes a release ambiguous).

    When the GITHUB_OUTPUT environment variable is set (in GitHub Actions), the result of -Tag is also written to it.

.EXAMPLE
    ./Resolve-Package.ps1 -Tag result-v1.2.3-preview.1

.EXAMPLE
    ./Resolve-Package.ps1 -Validate
#>
[CmdletBinding(DefaultParameterSetName = 'Tag')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Tag')]
    [string] $Tag,

    [Parameter(Mandatory, ParameterSetName = 'Validate')]
    [switch] $Validate
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$prefixPattern = '^[a-z0-9]+(-[a-z0-9]+)*-v$'
$versionPattern = '^\d+\.\d+\.\d+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$'

function Get-PackableProject {
    foreach ($file in Get-ChildItem -Path (Join-Path $root 'src') -Recurse -Filter '*.csproj') {
        $properties = (dotnet msbuild $file.FullName -getProperty:IsPackable -getProperty:PackageId -getProperty:MinVerTagPrefix | Out-String | ConvertFrom-Json).Properties

        if ($LASTEXITCODE -ne 0) {
            throw "Could not evaluate $($file.FullName)."
        }

        if ($properties.IsPackable -eq 'true') {
            [pscustomobject]@{
                Path      = [System.IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
                PackageId = $properties.PackageId
                Prefix    = $properties.MinVerTagPrefix
            }
        }
    }
}

function Assert-ValidPrefix($projects) {
    $problems = @()

    foreach ($project in $projects) {
        if ($project.Prefix -notmatch $prefixPattern) {
            $problems += "$($project.PackageId): the MinVerTagPrefix '$($project.Prefix)' must look like 'name-v' (lower case, ending in '-v')."
        }
    }

    foreach ($group in $projects | Group-Object Prefix | Where-Object Count -gt 1) {
        $problems += "The tag prefix '$($group.Name)' is used by more than one package: $(($group.Group.PackageId) -join ', ')."
    }

    foreach ($a in $projects) {
        foreach ($b in $projects | Where-Object { $_.PackageId -ne $a.PackageId }) {
            if ($b.Prefix.StartsWith($a.Prefix)) {
                $problems += "The tag prefix '$($a.Prefix)' ($($a.PackageId)) is the start of '$($b.Prefix)' ($($b.PackageId)); a tag could belong to either."
            }
        }
    }

    if ($problems.Count -gt 0) {
        $problems | ForEach-Object { Write-Error $_ -ErrorAction Continue }

        throw 'The packable projects do not have unambiguous tag prefixes.'
    }
}

$projects = @(Get-PackableProject)

if ($projects.Count -eq 0) {
    throw 'No packable project was found under src/.'
}

Assert-ValidPrefix $projects

if ($Validate) {
    $projects | ForEach-Object { Write-Host "$($_.PackageId): tags look like '$($_.Prefix)<version>'" }

    return
}

$candidates = @($projects | Where-Object { $Tag.StartsWith($_.Prefix) })

if ($candidates.Count -eq 0) {
    throw "The tag '$Tag' does not start with the tag prefix of any package: $(($projects | ForEach-Object { $_.Prefix }) -join ', ')."
}

$project = $candidates[0]
$version = $Tag.Substring($project.Prefix.Length)

if ($version -notmatch $versionPattern) {
    throw "The tag '$Tag' must be '$($project.Prefix)' followed by a version such as 1.2.3 or 1.2.3-preview.1, not '$version'."
}

$result = [ordered]@{
    tag        = $Tag
    prefix     = $project.Prefix
    project    = $project.Path
    packageId  = $project.PackageId
    version    = $version
    prerelease = ($version.Contains('-')).ToString().ToLowerInvariant()
}

$result.GetEnumerator() | ForEach-Object { Write-Host "$($_.Key)=$($_.Value)" }

if ($env:GITHUB_OUTPUT) {
    $result.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" | Add-Content -Path $env:GITHUB_OUTPUT }
}
