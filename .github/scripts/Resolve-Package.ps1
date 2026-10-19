<#
.SYNOPSIS
    Turns a release tag into the list of NuGet packages to publish, or checks that every package can be released with one tag.

.DESCRIPTION
    All packages in this repository are released together, with the same version, by one tag: `v1.2.3` releases every packable project under
    src/ as version 1.2.3. The projects therefore share the tag prefix `v` (set once in Directory.Build.props) and must not set one of their own.

    -Tag       Checks that the tag is a valid release tag and returns the version and the packages, in the order in which they must be
               pushed: a package comes after the packages that it references, so that a consumer never sees a package whose dependencies
               are missing.
    -Validate  Checks that every packable project uses the shared tag prefix (run in CI, so that a new package cannot be added in a way that
               would be released separately or not at all).

    When the GITHUB_OUTPUT environment variable is set (in GitHub Actions), the result of -Tag is also written to it. The packages and the
    projects are comma-separated lists.

.EXAMPLE
    ./Resolve-Package.ps1 -Tag v1.2.3-preview.1

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
$releasePrefix = 'v'
$versionPattern = '^\d+\.\d+\.\d+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$'

function Get-PackableProject {
    foreach ($file in Get-ChildItem -Path (Join-Path $root 'src') -Recurse -Filter '*.csproj') {
        $evaluated = dotnet msbuild $file.FullName -getProperty:IsPackable -getProperty:PackageId -getProperty:MinVerTagPrefix -getItem:ProjectReference | Out-String | ConvertFrom-Json

        if ($LASTEXITCODE -ne 0) {
            throw "Could not evaluate $($file.FullName)."
        }

        if ($evaluated.Properties.IsPackable -eq 'true') {
            $references = @()

            if ($evaluated.Items.PSObject.Properties.Name -contains 'ProjectReference') {
                $references = @($evaluated.Items.ProjectReference | ForEach-Object { [System.IO.Path]::GetFullPath($_.FullPath) })
            }

            [pscustomobject]@{
                Path       = [System.IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
                FullPath   = [System.IO.Path]::GetFullPath($file.FullName)
                PackageId  = $evaluated.Properties.PackageId
                Prefix     = $evaluated.Properties.MinVerTagPrefix
                References = $references
            }
        }
    }
}

function Assert-SharedPrefix($projects) {
    $problems = @()

    foreach ($project in $projects | Where-Object { $_.Prefix -ne $releasePrefix }) {
        $problems += "$($project.PackageId): the MinVerTagPrefix is '$($project.Prefix)', but all packages are released together with the tag prefix '$releasePrefix'. Remove the MinVerTagPrefix from the project (Directory.Build.props sets it)."
    }

    if ($problems.Count -gt 0) {
        $problems | ForEach-Object { Write-Error $_ -ErrorAction Continue }

        throw 'The packable projects do not share one tag prefix.'
    }
}

# A package after the packages it references; packages that do not depend on each other come in alphabetical order.
function Get-PushOrder($projects) {
    $remaining = [System.Collections.Generic.List[object]]::new()
    $projects | Sort-Object { $_.PackageId } | ForEach-Object { $remaining.Add($_) }
    $ordered = [System.Collections.Generic.List[object]]::new()

    while ($remaining.Count -gt 0) {
        $next = $remaining | Where-Object {
            $project = $_
            -not ($remaining | Where-Object { $_.FullPath -ne $project.FullPath -and $project.References -contains $_.FullPath })
        } | Select-Object -First 1

        if ($null -eq $next) {
            throw "The packable projects reference each other in a cycle: $(($remaining | ForEach-Object { $_.PackageId }) -join ', ')."
        }

        $ordered.Add($next)
        [void]$remaining.Remove($next)
    }

    return $ordered
}

$projects = @(Get-PackableProject)

if ($projects.Count -eq 0) {
    throw 'No packable project was found under src/.'
}

Assert-SharedPrefix $projects

if ($Validate) {
    Write-Host "All packages are released together by a tag that looks like '$($releasePrefix)<version>': $(($projects | ForEach-Object { $_.PackageId } | Sort-Object) -join ', ')"

    return
}

if (-not $Tag.StartsWith($releasePrefix)) {
    throw "The tag '$Tag' must start with '$releasePrefix' (all packages are released together by one tag)."
}

$version = $Tag.Substring($releasePrefix.Length)

if ($version -notmatch $versionPattern) {
    throw "The tag '$Tag' must be '$releasePrefix' followed by a version such as 1.2.3 or 1.2.3-preview.1, not '$version'."
}

$ordered = @(Get-PushOrder $projects)

$result = [ordered]@{
    tag        = $Tag
    version    = $version
    prerelease = ($version.Contains('-')).ToString().ToLowerInvariant()
    packageIds = ($ordered | ForEach-Object { $_.PackageId }) -join ','
    projects   = ($ordered | ForEach-Object { $_.Path }) -join ','
}

$result.GetEnumerator() | ForEach-Object { Write-Host "$($_.Key)=$($_.Value)" }

if ($env:GITHUB_OUTPUT) {
    $result.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" | Add-Content -Path $env:GITHUB_OUTPUT }
}
