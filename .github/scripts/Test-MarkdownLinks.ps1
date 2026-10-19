<#
.SYNOPSIS
    Checks that every relative link in the Markdown files of the repository points at a file or folder that exists.

.DESCRIPTION
    Absolute links (http, https, mailto) and links to an anchor in the same page are not checked, and neither are links inside fenced
    code blocks. A link with an anchor (`page.md#section`) is checked for the page only. Exits with a non-zero code when a link is broken.

.PARAMETER Root
    The folder to check. Defaults to the repository root.
#>
[CmdletBinding()]
param(
    [string] $Root = (Join-Path $PSScriptRoot '..' '..')
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path $Root).Path
$broken = 0
$checked = 0

$files = Get-ChildItem -Path $root -Recurse -Filter '*.md' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules|\.git|\.venv|\.sonarqube|site|BenchmarkDotNet\.Artifacts)[\\/]' }

foreach ($file in $files) {
    $inFence = $false
    $number = 0

    foreach ($line in Get-Content -Path $file.FullName) {
        $number++

        if ($line -match '^\s*(```|~~~)') {
            $inFence = -not $inFence

            continue
        }

        if ($inFence) {
            continue
        }

        foreach ($match in [regex]::Matches($line, '\]\(([^)\s]+)(?:\s+"[^"]*")?\)')) {
            $target = $match.Groups[1].Value

            if ($target -match '^(https?:|mailto:|#)') {
                continue
            }

            $target = ($target -split '#')[0]

            if ([string]::IsNullOrWhiteSpace($target)) {
                continue
            }

            $checked++
            $base = if ($target.StartsWith('/')) { $root } else { $file.DirectoryName }
            $path = Join-Path $base ([uri]::UnescapeDataString($target).TrimStart('/'))

            if (-not (Test-Path -LiteralPath $path)) {
                $relative = [System.IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
                Write-Host "::error file=$relative,line=$number::Broken link: $($match.Groups[1].Value)"
                $broken++
            }
        }
    }
}

Write-Host "Checked $checked relative link(s) in $($files.Count) Markdown file(s): $broken broken."

if ($broken -gt 0) {
    exit 1
}
