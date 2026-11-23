<#
.SYNOPSIS
    Finds the release that the notes of a release start from.

.DESCRIPTION
    The notes of a release list the pull requests since the previous release. For a stable release that is the previous stable release, so that the
    notes of 1.0.0 cover everything since 0.3.1 and not only what came after its release candidates (and the notes of 1.0.1 start from 1.0.0, not from
    a release candidate of 1.0.0). For a prerelease it is the tag before it, whether that is stable or a prerelease.

    Git's default version sort puts v1.0.0-rc.1 above v1.0.0, so the tags are sorted with the suffix after a hyphen coming before the release
    (versionsort.suffix). The previous release is then the next tag below this one in that order, so the answer does not depend on which tags were
    made later. It is empty when there is none, which is the case for the first release.

    The script runs in the repository that holds the tags. When the GITHUB_OUTPUT environment variable is set (in GitHub Actions), the result is also
    written to it as `previous`.

.PARAMETER Tag
    The release tag, `v` and a version, for example v1.0.0 or v1.0.0-rc.1. It must exist in the repository.

.EXAMPLE
    ./Resolve-PreviousTag.ps1 -Tag v1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Tag
)

$ErrorActionPreference = 'Stop'

$versionPattern = '^v\d+\.\d+\.\d+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$'

if ($Tag -notmatch $versionPattern) {
    throw "'$Tag' is not a release tag: it must be 'v' and a version, such as v1.2.3 or v1.2.3-preview.1."
}

$tags = @(git -c versionsort.suffix=- tag --list 'v[0-9]*' --sort=-v:refname | Where-Object { $_ -match $versionPattern })

if ($LASTEXITCODE -ne 0) {
    throw 'Could not list the tags.'
}

$position = [array]::IndexOf($tags, $Tag)

if ($position -lt 0) {
    throw "The tag '$Tag' does not exist in this repository."
}

$isPrerelease = $Tag.Contains('-')
$below = @($tags | Select-Object -Skip ($position + 1))

if (-not $isPrerelease) {
    $below = @($below | Where-Object { -not $_.Contains('-') })
}

$previous = if ($below.Count -gt 0) { $below[0] } else { '' }

if ($env:GITHUB_OUTPUT) {
    "previous=$previous" | Add-Content -Path $env:GITHUB_OUTPUT
}

Write-Host "previous=$previous"
