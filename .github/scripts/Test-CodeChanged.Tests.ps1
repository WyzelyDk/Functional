<#
.SYNOPSIS
    Tests Test-CodeChanged.ps1 against a throw-away git repository. Exits with a non-zero code when any case fails.
#>
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Test-CodeChanged.ps1'
$repo = Join-Path ([System.IO.Path]::GetTempPath()) ("code-changed-" + [guid]::NewGuid().ToString('N'))
$output = Join-Path ([System.IO.Path]::GetTempPath()) ("code-changed-output-" + [guid]::NewGuid().ToString('N'))
$failures = 0

New-Item -ItemType Directory -Path $repo | Out-Null
Push-Location $repo

try {
    git init -q -b main
    git config user.email 'test@example.com'
    git config user.name 'Test'
    git config core.autocrlf false

    function Add-File([string] $path, [string] $content = 'x') {
        $directory = Split-Path $path -Parent

        if ($directory) {
            New-Item -ItemType Directory -Force -Path $directory | Out-Null
        }

        Set-Content -Path $path -Value $content
    }

    foreach ($path in 'README.md', 'LICENSE', 'docs/guide.md', 'src/Library/Class.cs', 'src/Analyzers/AnalyzerReleases.Unshipped.md', 'global.json', '.github/workflows/ci.yml') {
        Add-File $path
    }

    git add -A
    git commit -q -m 'baseline'
    $baseline = (git rev-parse HEAD).Trim()

    function Test-Case([string] $name, [scriptblock] $change, [string] $expected) {
        git reset -q --hard $baseline
        git clean -q -fd
        & $change
        git add -A
        git commit -q -m $name
        Remove-Item $output -ErrorAction SilentlyContinue
        $env:GITHUB_OUTPUT = $output
        & $script -Base $baseline -Head HEAD | Out-Null
        $actual = (Get-Content $output -Raw).Trim()

        if ($actual -eq "code=$expected") {
            Write-Host "ok      $name"
        }
        else {
            Write-Host "FAILED  $name (expected code=$expected, got $actual)"
            $script:failures++
        }
    }

    Test-Case 'a README edit is documentation' { Add-File 'README.md' 'changed' } 'false'
    Test-Case 'a new page under docs is documentation' { Add-File 'docs/new/page.md' } 'false'
    Test-Case 'a picture under docs is documentation' { Add-File 'docs/picture.png' } 'false'
    Test-Case 'the license is documentation' { Add-File 'LICENSE' 'changed' } 'false'
    Test-Case 'the ruleset files are not built' { Add-File '.github/rulesets/main.json' '{}' } 'false'
    Test-Case 'the documentation site configuration is documentation' { Add-File 'mkdocs.yml' } 'false'
    Test-Case 'the documentation site tools are documentation' { Add-File '.github/mkdocs/hooks.py' } 'false'
    Test-Case 'the PR template is documentation' { Add-File '.github/PULL_REQUEST_TEMPLATE.md' } 'false'
    Test-Case 'a source file is code' { Add-File 'src/Library/Class.cs' 'changed' } 'true'
    Test-Case 'the analyzer release notes are a build input' { Add-File 'src/Analyzers/AnalyzerReleases.Unshipped.md' 'changed' } 'true'
    Test-Case 'a workflow is code' { Add-File '.github/workflows/ci.yml' 'changed' } 'true'
    Test-Case 'a script of the pipeline is code' { Add-File '.github/scripts/Something.ps1' } 'true'
    Test-Case 'global.json is code' { Add-File 'global.json' 'changed' } 'true'
    Test-Case 'an unknown kind of file is code' { Add-File 'something.unknown' } 'true'
    Test-Case 'documentation and code together are code' { Add-File 'README.md' 'changed'; Add-File 'src/Library/Class.cs' 'changed' } 'true'
    Test-Case 'a code file renamed into docs is still code' { New-Item -ItemType Directory -Force docs | Out-Null; git mv src/Library/Class.cs docs/Class.md } 'true'
    Test-Case 'a deleted source file is code' { git rm -q src/Library/Class.cs } 'true'

    # No base commit: always run everything.
    foreach ($base in $null, '', '0000000000000000000000000000000000000000', 'does-not-exist') {
        Remove-Item $output -ErrorAction SilentlyContinue
        $env:GITHUB_OUTPUT = $output
        & $script -Base $base -Head HEAD | Out-Null
        $actual = (Get-Content $output -Raw).Trim()

        if ($actual -eq 'code=true') {
            Write-Host "ok      an unusable base ('$base') runs everything"
        }
        else {
            Write-Host "FAILED  an unusable base ('$base') must run everything (got $actual)"
            $failures++
        }
    }
}
finally {
    Pop-Location
    Remove-Item $repo -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $output -ErrorAction SilentlyContinue
    Remove-Item Env:GITHUB_OUTPUT -ErrorAction SilentlyContinue
}

if ($failures -gt 0) {
    Write-Host "$failures case(s) failed."
    exit 1
}

Write-Host 'All cases passed.'

# The cases above run failing git commands on purpose; without this the last of those exit codes would become the script's.
exit 0
