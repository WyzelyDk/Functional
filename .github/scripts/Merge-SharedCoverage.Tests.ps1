<#
.SYNOPSIS
    Tests Merge-SharedCoverage.ps1 against throw-away Cobertura files. Exits with a non-zero code when any case fails.
#>
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Merge-SharedCoverage.ps1'
$directory = Join-Path ([System.IO.Path]::GetTempPath()) ("shared-coverage-" + [guid]::NewGuid().ToString('N'))
$failures = 0

function Write-Report([string] $name, [string] $package, [string] $class, [string] $filename) {
    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<coverage>
  <packages>
    <package name="$package">
      <classes>
        <class name="$class" filename="$filename"><lines><line number="1" hits="1" /></lines></class>
        <class name="Own.Class" filename="C:\work\src\Result\Own.cs"><lines><line number="1" hits="1" /></lines></class>
      </classes>
    </package>
  </packages>
</coverage>
"@
    Set-Content -Path (Join-Path $directory "$name.cobertura.xml") -Value $content
}

function Test-Case([string] $name, [bool] $condition) {
    if ($condition) {
        Write-Host "ok      $name"
    }
    else {
        Write-Host "FAILED  $name"
        $script:failures++
    }
}

function Get-Package([string] $name) {
    $document = [xml](Get-Content (Join-Path $directory "$name.cobertura.xml") -Raw)
    return $document.SelectNodes('//package')
}

New-Item -ItemType Directory -Path $directory | Out-Null

try {
    Write-Report 'windows' 'Wyzely.Functional.Result' 'Wyzely.Functional.Internal.Guard' 'C:\work\src\Shared\Internal\Guard.cs'
    Write-Report 'linux' 'Wyzely.Functional.Maybe' 'Wyzely.Functional.Internal.Guard' '/work/src/Shared/Internal/Guard.cs'
    Write-Report 'unrelated' 'Wyzely.Functional.Maybe' 'Wyzely.Functional.Maybe.Thing' '/work/src/Maybe/Thing.cs'

    & $script -Path $directory

    $windows = Get-Package 'windows'
    $linux = Get-Package 'linux'
    $unrelated = Get-Package 'unrelated'

    Test-Case 'a shared class with a Windows path moves to the shared package' (@($windows | Where-Object name -eq 'Wyzely.Functional.Shared' | ForEach-Object { $_.classes.class.name }) -contains 'Wyzely.Functional.Internal.Guard')
    Test-Case 'a shared class with a Linux path moves to the shared package' (@($linux | Where-Object name -eq 'Wyzely.Functional.Shared' | ForEach-Object { $_.classes.class.name }) -contains 'Wyzely.Functional.Internal.Guard')
    Test-Case 'a class of the assembly itself stays where it was' (@($windows | Where-Object name -eq 'Wyzely.Functional.Result' | ForEach-Object { $_.classes.class.name }) -contains 'Own.Class')
    Test-Case 'a file without a shared class is not given a shared package' (@($unrelated | Where-Object name -eq 'Wyzely.Functional.Shared').Count -eq 0)

    & $script -Path $directory

    Test-Case 'running it again changes nothing' (@(Get-Package 'windows' | Where-Object name -eq 'Wyzely.Functional.Shared').Count -eq 1)
}
finally {
    Remove-Item -Recurse -Force $directory -ErrorAction SilentlyContinue
}

if ($failures -gt 0) {
    Write-Host "$failures case(s) failed."
    exit 1
}

Write-Host 'All cases passed.'
