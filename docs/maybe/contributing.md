# Building and contributing

[Back to the Maybe overview](../../src/Maybe/README.md)

This page is for people who want to change the library. If you only want to use it, you do not need anything here.

## Before you propose a change

Read [Scope](scope.md). Every new public member has to pass its admission test, and every operation on the railway costs nine generated
methods plus tests and documentation. Say which of the questions your change answers and show the few lines someone would write today
without it; that is the fastest way to a decision.

## Building

Run these from the repository root. They need the .NET 11 SDK (a release candidate for now; `global.json` pins the exact version) and the .NET 8
and 10 runtimes. The library targets `netstandard2.0`, `net8.0`, `net10.0` and `net11.0` (which adds C# 15 union support, see
[C# 15 unions](unions.md)); the tests run on `net8.0`, `net10.0` and `net11.0`.

```shell
dotnet build Wyzely.Functional.slnx -c Release
dotnet run --project test/Maybe.Tests -f net10.0          # the tests are executables (xUnit v3 / Microsoft.Testing.Platform)
dotnet run --project test/Maybe.Analyzers.Tests
dotnet run --project test/Functional.Tests -f net10.0      # the umbrella package: Maybe and Result together
dotnet pack src/Maybe -c Release
```

Release builds treat warnings as errors, and every public and internal member has XML documentation (see the `stylecop.*` settings in `.editorconfig`).

## How the work is done here

- **Tests first.** A behavior change starts with a failing test (failing at run time, with a stub so that it compiles), then the code.
- **Benchmarks before performance claims.** A change that is meant to be faster is measured, and a measurement that contradicts the
  expectation wins. The claims on [Performance](performance.md) are tests.
- **README examples are tests.** If you change a snippet in the README or a page, change the test that holds it
  (`test/Maybe.Tests/ReadmeExamplesTests.cs`, and `test/Functional.Tests/ReadmeExamplesTests.cs` for the umbrella package). The samples in
  `samples/Maybe.Samples` are covered by `test/Samples.Tests/MaybeSamplesTests.cs`.
- **Code shared with the other packages** (guards, the hash helper, nullable-attribute polyfills, the options holder) lives in `src/Shared/Internal` and is
  compiled into each library as internal source; the generator engine in `src/Shared/CodeGeneration` is shared the same way. The largest `Zip` arity is
  `GeneratorSettings.MaxZipArity` there.
- **No namespace beginning with `Wyzely.Functional.Maybe.`.** It would clash with the type `Wyzely.Functional.Maybe`; see
  [Design decisions](design-decisions.md#single-word-namespaces-for-the-tooling).

## The generated async variants

The `Map`/`Bind`/`Filter`/`Tap`/`TapNone`/`Or`/`ValueOr`/`Match`/`Switch` methods and their tests are generated: one file per combination of a
synchronous, `Task` or `ValueTask` receiver and delegate (for example `Generated/Map/Map.Task.ValueTask.g.cs`). The generator is
`src/Maybe.CodeGeneration` and the catalog of operations is `Operations.cs`. To add or change an operation, edit the catalog and run:

```shell
dotnet run --project src/Maybe.CodeGeneration
```

A test (`GeneratedFilesTests`) fails if the generated files are out of date. `-- --stubs` writes methods that do nothing useful, so you
can watch the generated tests fail first.

## Repository layout

```
src/Maybe                      the library
src/Maybe.Analyzers            the Roslyn analyzer (packed into the library package)
src/Maybe.Analyzers.CodeFixes  the code fix project (WYZ0201 has no code fix)
src/Maybe.CodeGeneration       the generator for the async variants and their tests
src/Functional                 the umbrella package: Result, Maybe and the methods between them
test/                          unit, analyzer and nullability tests (Maybe.Tests, Maybe.Analyzers.Tests, Functional.Tests)
benchmarks/Maybe.Benchmarks    BenchmarkDotNet scenarios
docs/maybe/                    the documentation pages and analyzer rule pages
docs/functional/               the documentation of the umbrella package
```

## Pull requests and releases

Every pull request must build without errors or warnings and pass all tests; CI checks both. All packages are released together, with the same version, by one tag: `v` and the version (for example `v1.2.0`). How CI works, how the packages are released with a tag, and the
one-time GitHub and nuget.org setup are in [CI and releasing](../releasing.md).
