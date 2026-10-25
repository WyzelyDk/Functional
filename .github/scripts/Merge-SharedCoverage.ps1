<#
.SYNOPSIS
    Moves the coverage of the shared source files into one package, so that a file counts once however many assemblies compile it.

.DESCRIPTION
    The files under src/Shared are compiled as source into several assemblies (the helpers into every library, the generator engine into both
    generators and into the Shared project). Each test run measures only the copy in the assembly that it exercises, so in the report a class
    of such a file looks half covered in an assembly whose tests take only some of its branches, and the report lists it three times.

    This script rewrites every Cobertura file under -Path: a class whose file is under src/Shared is moved into the package
    `Wyzely.Functional.Shared`. ReportGenerator then merges the copies of a class (same package, same name) and the line or branch is covered
    when any run covered it, which is what the coverage of a source file means.

.EXAMPLE
    ./Merge-SharedCoverage.ps1 -Path .
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'

$sharedPackage = 'Wyzely.Functional.Shared'
$sharedPath = '[/\\]src[/\\]Shared[/\\]'

foreach ($file in Get-ChildItem -Path $Path -Recurse -Filter '*.cobertura.xml') {
    $document = [xml]::new()
    $document.Load($file.FullName)

    $packages = $document.SelectSingleNode('//packages')
    $target = $packages.SelectSingleNode("package[@name='$sharedPackage']")
    $moved = 0

    foreach ($class in @($document.SelectNodes('//package/classes/class'))) {
        if ($class.GetAttribute('filename') -notmatch $sharedPath -or $class.ParentNode.ParentNode.GetAttribute('name') -eq $sharedPackage) {
            continue
        }

        if ($null -eq $target) {
            $target = $document.CreateElement('package')
            $target.SetAttribute('name', $sharedPackage)
            $target.AppendChild($document.CreateElement('classes')) | Out-Null
            $packages.AppendChild($target) | Out-Null
        }

        $target.SelectSingleNode('classes').AppendChild($class) | Out-Null
        $moved++
    }

    if ($moved -gt 0) {
        $document.Save($file.FullName)
    }
}
