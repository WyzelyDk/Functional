<#
.SYNOPSIS
    Decides whether a change touches anything other than documentation, so that CI can skip the build and the tests for a pure
    documentation change.

.DESCRIPTION
    Writes `code=true` or `code=false` (to the file named by GITHUB_OUTPUT when it is set, and to the console).

    A file is "documentation" when it cannot change what is built or tested. EVERYTHING ELSE COUNTS AS CODE, so a new kind of file
    makes CI run until someone decides it is safe to ignore. Two kinds of file look like documentation but are not:
      - AnalyzerReleases.Shipped.md and AnalyzerReleases.Unshipped.md are fed to the analyzer as AdditionalFiles, and a wrong edit
        fails the build;
      - everything under .github/workflows and .github/scripts is the pipeline itself.

    If the base commit is unknown (a first push, a new branch), or the diff cannot be computed, the answer is `code=true`.

.PARAMETER Base
    The commit to compare with (the state before the change).

.PARAMETER Head
    The commit that contains the change.

.EXAMPLE
    ./Test-CodeChanged.ps1 -Base origin/main -Head HEAD
#>
[CmdletBinding()]
param(
    [string] $Base,

    [Parameter(Mandatory)]
    [string] $Head
)

$ErrorActionPreference = 'Stop'

# Files that cannot change what is built or tested.
$documentation = @(
    '\.md$',
    '^docs/',
    '^LICENSE$',
    '^\.github/PULL_REQUEST_TEMPLATE\.md$',
    '^\.github/ISSUE_TEMPLATE/',
    '^\.github/CODEOWNERS$',
    '^\.github/FUNDING\.yml$',
    '^\.github/dependabot\.yml$',
    '^\.github/rulesets/',
    '^mkdocs\.yml$',
    '^\.github/mkdocs/'
)

# Markdown files that ARE inputs to the build.
$buildInputs = @(
    '(^|/)AnalyzerReleases\.(Shipped|Unshipped)\.md$'
)

function Write-Answer([bool] $code, [string] $reason) {
    Write-Host "code=$($code.ToString().ToLowerInvariant())  ($reason)"

    if ($env:GITHUB_OUTPUT) {
        "code=$($code.ToString().ToLowerInvariant())" | Add-Content -Path $env:GITHUB_OUTPUT
    }
}

function Test-IsDocumentation([string] $path) {
    foreach ($pattern in $buildInputs) {
        if ($path -match $pattern) {
            return $false
        }
    }

    foreach ($pattern in $documentation) {
        if ($path -match $pattern) {
            return $true
        }
    }

    return $false
}

if ([string]::IsNullOrWhiteSpace($Base) -or $Base -match '^0+$') {
    Write-Answer $true 'there is no base commit to compare with'

    return
}

git cat-file -e "$Base^{commit}" 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Answer $true "the base commit '$Base' is not available"

    return
}

# --no-renames lists both the old and the new path of a moved file, so a code file renamed into docs/ still counts as code.
$files = @(git diff --name-only --no-renames $Base $Head)

if ($LASTEXITCODE -ne 0) {
    Write-Answer $true 'the changed files could not be listed'

    return
}

foreach ($file in $files) {
    if (-not (Test-IsDocumentation $file)) {
        Write-Answer $true "'$file' is not documentation"

        return
    }
}

Write-Answer $false "$($files.Count) changed file(s), all documentation"
