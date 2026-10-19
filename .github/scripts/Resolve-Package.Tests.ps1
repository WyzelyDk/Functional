<#
.SYNOPSIS
    Tests Resolve-Package.ps1 against throw-away repositories and against this one. Exits with a non-zero code when any case fails.

.DESCRIPTION
    All packages are released together by one tag, `v<version>`. The script turns that tag into the list of packages to publish (in the
    order in which they must be pushed: a package after the packages it references) and checks that no package has a tag prefix of its own.
#>
$ErrorActionPreference = 'Stop'

$realRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$work = Join-Path ([System.IO.Path]::GetTempPath()) ('resolve-package-' + [guid]::NewGuid().ToString('N'))
$failures = 0

New-Item -ItemType Directory -Path $work | Out-Null

# Builds a repository with the script in .github/scripts and one project per entry under src/. A project has an Id, whether it is packable,
# the Ids of the projects it references, and optionally a tag prefix of its own.
function New-Fixture([string] $name, [hashtable[]] $projects) {
    $root = Join-Path $work $name
    New-Item -ItemType Directory -Path (Join-Path $root '.github' 'scripts') | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'Resolve-Package.ps1') (Join-Path $root '.github' 'scripts')
    Set-Content -Path (Join-Path $root 'Directory.Build.props') -Value '<Project><PropertyGroup><MinVerTagPrefix>v</MinVerTagPrefix><IsPackable>false</IsPackable></PropertyGroup></Project>'

    foreach ($project in $projects) {
        $directory = Join-Path $root 'src' $project.Id
        New-Item -ItemType Directory -Path $directory | Out-Null
        $prefix = if ($project.Prefix) { "<MinVerTagPrefix>$($project.Prefix)</MinVerTagPrefix>" } else { '' }
        $references = ($project.References | ForEach-Object { "<ProjectReference Include=`"..\$_\$_.csproj`" />" }) -join ''
        $packable = if ($project.Packable) { 'true' } else { 'false' }
        Set-Content -Path (Join-Path $directory "$($project.Id).csproj") -Value "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><IsPackable>$packable</IsPackable><PackageId>$($project.Id)</PackageId>$prefix</PropertyGroup><ItemGroup>$references</ItemGroup></Project>"
    }

    return $root
}

# Runs the script of a repository in its own process, so that a thrown error shows up as the exit code, and returns the exit code and the
# values that the script wrote to GITHUB_OUTPUT.
function Invoke-Resolve([string] $root, [string[]] $arguments) {
    $outputFile = Join-Path $work ([guid]::NewGuid().ToString('N') + '.out')
    $env:GITHUB_OUTPUT = $outputFile

    try {
        & pwsh -NoProfile -File (Join-Path $root '.github' 'scripts' 'Resolve-Package.ps1') @arguments *> $null
        $code = $LASTEXITCODE
    }
    finally {
        Remove-Item Env:GITHUB_OUTPUT
    }

    $values = @{}

    if (Test-Path $outputFile) {
        foreach ($line in Get-Content $outputFile) {
            $separator = $line.IndexOf('=')

            if ($separator -gt 0) {
                $values[$line.Substring(0, $separator)] = $line.Substring($separator + 1)
            }
        }
    }

    return @{ Code = $code; Values = $values }
}

function Assert-Case([string] $name, [bool] $condition, [string] $detail) {
    if ($condition) {
        Write-Host "ok      $name"
    }
    else {
        Write-Host "FAILED  $name ($detail)"
        $script:failures++
    }
}

try {
    $good = New-Fixture 'good' @(
        @{ Id = 'Alpha.Umbrella'; Packable = $true; References = @('Beta.Core', 'Gamma.Extras') },
        @{ Id = 'Beta.Core'; Packable = $true },
        @{ Id = 'Gamma.Extras'; Packable = $true },
        @{ Id = 'Delta.Tool'; Packable = $false; Prefix = 'tool-v' })

    $release = Invoke-Resolve $good @('-Tag', 'v0.1.0')
    Assert-Case 'a release tag resolves' ($release.Code -eq 0) "exit code $($release.Code)"
    Assert-Case 'the version is the tag without the prefix' ($release.Values['version'] -eq '0.1.0') "got '$($release.Values['version'])'"
    Assert-Case 'a version without a hyphen is not a prerelease' ($release.Values['prerelease'] -eq 'false') "got '$($release.Values['prerelease'])'"
    Assert-Case 'the tag is passed on' ($release.Values['tag'] -eq 'v0.1.0') "got '$($release.Values['tag'])'"
    Assert-Case 'every packable project is released, a package after the packages it references' ($release.Values['packageIds'] -eq 'Beta.Core,Gamma.Extras,Alpha.Umbrella') "got '$($release.Values['packageIds'])'"
    Assert-Case 'the projects come in the same order' ($release.Values['projects'] -eq 'src/Beta.Core/Beta.Core.csproj,src/Gamma.Extras/Gamma.Extras.csproj,src/Alpha.Umbrella/Alpha.Umbrella.csproj') "got '$($release.Values['projects'])'"

    $preview = Invoke-Resolve $good @('-Tag', 'v1.2.3-preview.1')
    Assert-Case 'a version with a hyphen is a prerelease' ($preview.Code -eq 0 -and $preview.Values['prerelease'] -eq 'true' -and $preview.Values['version'] -eq '1.2.3-preview.1') "exit code $($preview.Code), got '$($preview.Values['version'])' / '$($preview.Values['prerelease'])'"

    foreach ($tag in 'result-v0.1.0', 'v0.1', 'v1.2.3.4', '0.1.0', 'V0.1.0', 'v', 'vx.y.z') {
        $rejected = Invoke-Resolve $good @('-Tag', $tag)
        Assert-Case "the tag '$tag' is rejected" ($rejected.Code -ne 0) 'the script accepted it'
    }

    $valid = Invoke-Resolve $good @('-Validate')
    Assert-Case 'validation passes when no package has a prefix of its own' ($valid.Code -eq 0) "exit code $($valid.Code)"

    $own = New-Fixture 'own-prefix' @(
        @{ Id = 'Beta.Core'; Packable = $true },
        @{ Id = 'Gamma.Extras'; Packable = $true; Prefix = 'extras-v' })

    $ownValidation = Invoke-Resolve $own @('-Validate')
    Assert-Case 'validation fails when a package has a tag prefix of its own' ($ownValidation.Code -ne 0) 'the script accepted it'

    $ownRelease = Invoke-Resolve $own @('-Tag', 'v0.1.0')
    Assert-Case 'a release is refused when a package has a tag prefix of its own' ($ownRelease.Code -ne 0) 'the script accepted it'

    $none = New-Fixture 'none' @(@{ Id = 'Delta.Tool'; Packable = $false })
    $noneValidation = Invoke-Resolve $none @('-Validate')
    Assert-Case 'validation fails when there is nothing to release' ($noneValidation.Code -ne 0) 'the script accepted it'

    $real = Invoke-Resolve $realRoot @('-Tag', 'v0.1.0')
    $realIds = @(($real.Values['packageIds'] -split ',') | Where-Object { $_ })
    Assert-Case 'this repository releases its three packages with one tag' ($real.Code -eq 0 -and $realIds.Count -eq 3) "exit code $($real.Code), packages: $($real.Values['packageIds'])"
    Assert-Case 'the umbrella package is pushed last' ($realIds.Count -gt 0 -and $realIds[-1] -eq 'Wyzely.Functional') "packages: $($real.Values['packageIds'])"

    $realValidation = Invoke-Resolve $realRoot @('-Validate')
    Assert-Case 'this repository passes the validation' ($realValidation.Code -eq 0) "exit code $($realValidation.Code)"
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures -gt 0) {
    Write-Host "$failures case(s) failed."
    exit 1
}

Write-Host 'All cases passed.'
