<#
.SYNOPSIS
    Checks that the packages work for a project on the long-term-support .NET, with every .NET SDK that such a project can be built with.

.DESCRIPTION
    A project that targets net10.0 can be built with any SDK from 10.0.100 on, and each SDK carries its own compiler. A compiler refuses to load an
    analyzer that references a newer Roslyn than itself (warning CS9057, an error when warnings are errors), so the packages are only as usable as the
    Roslyn that their analyzers are built against allows. This script builds a small consumer project against the packages that were just packed,
    once for every installed SDK of .NET 10 or later (stable releases only), and checks, with warnings as errors:

      1. A program that reads GetValue() of a Result and of a Maybe without a check is reported by WYZ0102 and WYZ0201, and the compiler reports no
         problem with loading an analyzer (CS9057, CS8032, AD0001). That proves that the analyzers loaded and ran in that SDK.
      2. A correct program builds without a warning and runs on the .NET 10 runtime, and prints what it should.

    Run it after the packages were packed. It needs the SDKs to be installed (CI installs the oldest band of .NET 10 next to the others) and the
    .NET 10 runtime. The packages come from the local folder only (package source mapping), so a published package of the same name cannot be used.

.PARAMETER Packages
    The folder with the packed .nupkg files of Wyzely.Functional, Wyzely.Functional.Result and Wyzely.Functional.Maybe.

.PARAMETER Sdk
    The SDK versions to test. The default is every stable SDK of version 10 or later that is installed.

.EXAMPLE
    ./Test-LtsConsumer.ps1 -Packages ./artifacts
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Packages,

    [string[]] $Sdk
)

$ErrorActionPreference = 'Stop'

$packageFolder = (Resolve-Path $Packages).Path
$umbrella = Get-ChildItem -Path $packageFolder -Filter 'Wyzely.Functional.*.nupkg' |
    Where-Object { $_.Name -match '^Wyzely\.Functional\.(?<version>\d[^ ]*)\.nupkg$' } |
    Select-Object -First 1
if (-not $umbrella) {
    throw "No Wyzely.Functional package was found in $packageFolder."
}
$null = $umbrella.Name -match '^Wyzely\.Functional\.(?<version>\d[^ ]*)\.nupkg$'
$version = $Matches['version']
Write-Host "Packages: Wyzely.Functional $version from $packageFolder"

if (-not $Sdk) {
    $Sdk = dotnet --list-sdks |
        ForEach-Object { ($_ -split ' ')[0] } |
        Where-Object { $_ -match '^(\d+)\.' -and [int] $Matches[1] -ge 10 -and $_ -notmatch '-' }
}
if (-not $Sdk) {
    throw 'No stable SDK of .NET 10 or later is installed.'
}

$wrongProgram = @'
using Wyzely.Functional;

Result<int> result = Result.Success(1);
Maybe<int> maybe = Maybe.Some(2);
Console.WriteLine(result.GetValue() + maybe.GetValue());
'@

$rightProgram = @'
using Wyzely.Functional;

var message = Result.Success(42)
    .Ensure(static x => x > 0, Error.Validation("Must be positive."))
    .Map(static x => x * 2)
    .Match(static x => $"ok {x}", static e => $"failed {e}");
var maybe = Maybe.Some<object>(5).OfType<int>().ValueOr(0);
Console.WriteLine($"{message} | {maybe}");
'@

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("lts-consumer-" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$failed = $false

function New-Consumer([string] $folder, [string] $sdk, [string] $program) {
    New-Item -ItemType Directory -Path $folder | Out-Null
    Set-Content -Path (Join-Path $folder 'global.json') -Value "{ `"sdk`": { `"version`": `"$sdk`", `"rollForward`": `"disable`" } }"
    $config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$packageFolder" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="Wyzely.Functional*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@
    Set-Content -Path (Join-Path $folder 'nuget.config') -Value $config
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="Wyzely.Functional" Version="$version" /></ItemGroup>
</Project>
"@
    Set-Content -Path (Join-Path $folder 'consumer.csproj') -Value $project
    Set-Content -Path (Join-Path $folder 'Program.cs') -Value $program
}

try {
    # A package that was restored for one SDK must not hide a problem with the next, so the packages are extracted into a folder of their own.
    $env:NUGET_PACKAGES = Join-Path $work 'nuget-packages'

    foreach ($sdk in $Sdk) {
        $wrong = Join-Path $work "wrong-$sdk"
        New-Consumer $wrong $sdk $wrongProgram
        Push-Location $wrong
        try {
            $output = (dotnet build --configuration Release -warnaserror 2>&1 | Out-String)
        } finally {
            Pop-Location
        }
        $problems = @()
        if ($output -match 'CS9057|CS8032|AD0001') {
            $problems += 'the compiler could not load an analyzer'
        }
        if ($output -notmatch 'WYZ0102') {
            $problems += 'WYZ0102 (Result) was not reported'
        }
        if ($output -notmatch 'WYZ0201') {
            $problems += 'WYZ0201 (Maybe) was not reported'
        }

        $right = Join-Path $work "right-$sdk"
        New-Consumer $right $sdk $rightProgram
        Push-Location $right
        try {
            $built = (dotnet build --configuration Release -warnaserror 2>&1 | Out-String)
            $ran = (dotnet run --configuration Release --no-build 2>&1 | Out-String).Trim()
        } finally {
            Pop-Location
        }
        if ($built -notmatch 'Build succeeded') {
            $lines = $built -split "`n" | Where-Object { $_ -match 'error|warning' } | Select-Object -First 2
            $problems += "the correct program did not build ($($lines -join ' ').Trim())"
        } elseif ($ran -ne 'ok 84 | 5') {
            $problems += "the correct program printed '$ran' instead of 'ok 84 | 5'"
        }

        if ($problems.Count -gt 0) {
            Write-Host "FAIL: SDK ${sdk}: $($problems -join '; ')"
            $failed = $true
        } else {
            Write-Host "OK: SDK ${sdk} loads the analyzers (WYZ0102 and WYZ0201 reported) and the library runs on .NET 10."
        }
    }
} finally {
    Remove-Item -Path $work -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failed) {
    exit 1
}
