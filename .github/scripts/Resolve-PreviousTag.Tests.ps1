<#
.SYNOPSIS
    Tests Resolve-PreviousTag.ps1 against a throw-away repository. Exits with a non-zero code when any case fails.

.DESCRIPTION
    The notes of a release list the pull requests since the previous release. For a stable release that is the previous stable release, so that the
    notes of 1.0.0 cover everything since 0.3.1 and not only what came after its release candidates; for a prerelease it is the tag before it.
    Git's default version sort puts v1.0.0-rc.1 above v1.0.0, so the order has to be told that a suffix after a hyphen comes before the release.
#>
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Resolve-PreviousTag.ps1'
$repo = Join-Path ([System.IO.Path]::GetTempPath()) ('previous-tag-' + [guid]::NewGuid().ToString('N'))
$failures = 0

New-Item -ItemType Directory -Path $repo | Out-Null
Push-Location $repo

try {
    git init -q -b main
    git config user.email 'test@example.com'
    git config user.name 'Test'
    git commit -q --allow-empty -m 'baseline'

    # Created out of order on purpose: the answer must come from the versions, not from when a tag was made. 'nightly' and 'v-notes' are not releases.
    foreach ($tag in 'v1.1.0', 'v0.3.1', 'v1.0.0-rc.2', 'v0.1.0', 'v1.0.0', 'v1.0.1', 'nightly', 'v1.0.0-rc.1', 'v1.1.0-preview.1', 'v-notes') {
        git tag $tag
    }

    # Runs the script in its own process and returns the exit code and the value that it wrote to GITHUB_OUTPUT.
    function Invoke-Resolve([string] $tag) {
        $outputFile = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N') + '.out')
        $env:GITHUB_OUTPUT = $outputFile

        try {
            & pwsh -NoProfile -File $script -Tag $tag *> $null
            $code = $LASTEXITCODE
        }
        finally {
            Remove-Item Env:GITHUB_OUTPUT
        }

        $previous = $null

        if (Test-Path $outputFile) {
            $line = Get-Content $outputFile | Where-Object { $_ -like 'previous=*' } | Select-Object -First 1

            if ($line) {
                $previous = $line.Substring('previous='.Length)
            }

            Remove-Item $outputFile
        }

        return @{ Code = $code; Previous = $previous }
    }

    function Test-Case([string] $name, [string] $tag, [string] $expected) {
        $result = Invoke-Resolve $tag

        if ($result.Code -eq 0 -and $result.Previous -ceq $expected) {
            Write-Host "ok      $name"
        }
        else {
            Write-Host "FAILED  $name (expected '$expected', got '$($result.Previous)', exit code $($result.Code))"
            $script:failures++
        }
    }

    Test-Case 'the first release candidate starts from the last stable release' 'v1.0.0-rc.1' 'v0.3.1'
    Test-Case 'a later release candidate starts from the one before it' 'v1.0.0-rc.2' 'v1.0.0-rc.1'
    Test-Case 'a stable release starts from the previous stable release, not from its release candidates' 'v1.0.0' 'v0.3.1'
    Test-Case 'a patch release starts from the release that it patches' 'v1.0.1' 'v1.0.0'
    Test-Case 'a preview starts from the last release before it' 'v1.1.0-preview.1' 'v1.0.1'
    Test-Case 'a stable release starts from the previous stable release, not from its preview' 'v1.1.0' 'v1.0.1'
    Test-Case 'the first release has no previous release' 'v0.1.0' ''

    function Test-Rejected([string] $name, [string] $tag) {
        $result = Invoke-Resolve $tag

        if ($result.Code -ne 0) {
            Write-Host "ok      $name"
        }
        else {
            Write-Host "FAILED  $name (the script accepted it and answered '$($result.Previous)')"
            $script:failures++
        }
    }

    Test-Rejected 'a tag that is not a release tag is rejected' 'nightly'
    Test-Rejected 'a release tag that does not exist is rejected' 'v9.9.9'

    Remove-Item Env:GITHUB_OUTPUT -ErrorAction SilentlyContinue
}
finally {
    Pop-Location
    Remove-Item $repo -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures -gt 0) {
    Write-Host "$failures case(s) failed."
    exit 1
}
