# Building and contributing

[Back to the Result overview](../../src/Result/README.md)

This page is for people who want to change the library. If you only want to use it, you do not need anything here.

## Before you propose a change

Read [Scope](scope.md). Every new public member has to pass its admission test, and every operation on the railway costs nine generated
methods plus tests and documentation. Say which of the questions your change answers and show the few lines someone would write today
without it; that is the fastest way to a decision.

## Building

Run these from the repository root. They need the .NET 11 SDK (a release candidate for now; `global.json` pins the exact version) and the .NET 8
and 10 runtimes. The library targets `netstandard2.0`, `net8.0`, `net10.0` and `net11.0` (which adds C# 15 union support); the tests run on
`net8.0`, `net10.0` and `net11.0`.

```shell
dotnet build Wyzely.Functional.slnx -c Release
dotnet run --project test/Result.Tests -f net10.0       # the tests are executables (xUnit v3 / Microsoft.Testing.Platform)
dotnet run --project test/Result.Analyzers.Tests
dotnet pack src/Result -c Release
```

Release builds treat warnings as errors, and every public and internal member has XML documentation (see the `stylecop.*` settings in `.editorconfig`).

## How the work is done here

- **Tests first.** A behavior change starts with a failing test (failing at run time, with a stub so that it compiles), then the code.
- **Benchmarks before performance claims.** A change that is meant to be faster is measured, and a measurement that contradicts the
  expectation wins (see [Design decisions](design-decisions.md)).
- **README examples are tests.** If you change a snippet, change the test that holds it.

## The generated async variants

The `Map`/`Bind`/`Tap`/... methods and their tests are generated: one file per combination of a synchronous, `Task` or `ValueTask`
receiver and delegate (for example `Generated/Bind/BindFromResultOfTToResultOfT.Task.ValueTask.g.cs`). The generator is
`src/Result.CodeGeneration` and the catalog of operations is `Operations.cs`. To add or change an
operation, edit the catalog and run:

```shell
dotnet run --project src/Result.CodeGeneration
```

A test (`GeneratedFilesTests`) fails if the generated files are out of date. `-- --stubs` writes methods that do nothing useful, so you
can watch the generated tests fail first.

## Repository layout

```
src/Result                      the library
src/Result.Analyzers            the Roslyn analyzers (packed into the library package)
src/Result.Analyzers.CodeFixes  the code fixes (packed into the library package)
src/Result.CodeGeneration       the generator for the async variants and their tests
test/                                                  unit, analyzer, code fix and nullability tests
benchmarks/                                            BenchmarkDotNet comparisons
docs/result/                                          the documentation pages and analyzer rule pages
```

## Pull requests and releases

Every pull request must build without errors or warnings and pass all tests; CI checks both. How CI works, how the packages are released with one
tag, and the one-time GitHub and nuget.org setup are in [CI and releasing](../releasing.md).
