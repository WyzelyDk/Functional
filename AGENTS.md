# Agent Guidelines

Instructions for AI coding agents (Claude Code, Codex and others) working in this repository.
Claude Code reads this file through `CLAUDE.md`, which only imports it.

This repository is **Wyzely.Functional**: fast, fluent, dependency-free functional building blocks for .NET. (a Result
type, a Maybe type, and the umbrella package that connects them). The readers that matter most are the
**consumers** of the packages, so the two things to optimize for, in this order, are **developer experience** and **performance**, both
backed by evidence. Read `README.md` first, then the package `README.md` and its `docs/` pages (`docs/result/scope.md` in particular).

## Workflow

- **Follow TDD.** Write the failing test first, see it fail, then write the code that makes it pass.
- **Red must be a runtime failure.** Add a stub implementation first (for example a method that throws or returns a default) so the test
  compiles and fails by assertion. A test that fails only because it does not compile has not shown anything.
- **Never add behavior without a test that fails without it.** That includes documentation examples: a README snippet is a test too
  (`ReadmeExamplesTests`).
- **Characterize before you refactor.** Before moving or reshaping existing logic, make sure a test pins its current behavior, then change
  the structure while that test stays green.
- **Plan with test cases.** When you propose a change, list the test cases with it, so that what "done" means is agreed before code exists.
- **Do not commit** unless asked. Never force-push, never skip hooks.

## Developer experience

The library is judged by how it feels to use, not only by what it can do.

- **The API reads like prose.** Functional names (`Map`, `Bind`, `Ensure`, `Match`), the same name for every sync/`Task`/`ValueTask`
  variant (no `Async` suffix), no abbreviations. A chain should read top to bottom without an `await` in the middle.
- **Generic type parameters begin with `T` and say what they are:** `T`, `TResult`, `TSource`, `TException`; never a bare `K`, `U` or `E`.
- **Conversions are named by what they do.** `As...` re-presents the same data under another type without creating a new object and without
  losing information (`AsNullable`, like `AsEnumerable`); `To...` builds something new or drops information (`ToResult`, `ToMaybe`,
  `ToEnumerable`).
- **Explicit over implicit.** No implicit conversions into a result. No property that throws for the wrong state: `GetValue()` and
  `GetError()` are methods, and an analyzer checks that they are guarded. The one deliberate exception is `Maybe<T>`: a plain value
  converts to it implicitly and a `null` reference becomes none, because absence is exactly what `null` means (`ImplicitConversionTests`
  in `Maybe.Tests` pins that this is the only conversion, and that nothing converts out of a maybe).
- **Make the wrong thing hard, and say how to do the right thing.** Prefer an analyzer (with a code fix when a mechanical fix is safe) to
  a documentation warning. Exception and diagnostic messages state what to do next ("Check IsFailure first, or use TryGetError").
- **Nullability is part of the API.** Annotate precisely (`[NotNullWhen]`, `[MaybeNullWhen]`, honest `?`). `ConsumerNullabilityTests`
  compiles consumer snippets and asserts which warnings appear; add a case when you change a signature.
- **IntelliSense is the first documentation.** Every public **and internal** member has an XML doc comment (SA1600 is enforced); the
  summary says what it does, not what it is called.
- **Defaults are sensible and configurable.** Do not hard-code a separator, message or policy that a consumer could reasonably want to
  change; make it a setting in `ResultOptions` with a good default. Equally, do not add a setting that nobody asked for.
- **Keep the surface small.** Before adding public API, apply the admission test in `docs/result/scope.md`. A feature that does not pass is
  declined. Every railway operation costs nine generated methods.
- **Never invent facts in documentation.** Comparisons with other libraries come from inspecting the packages (load the assembly, read the
  nuspec), not from memory. Say what was not verified.

## Performance

- **Measure; do not assume.** A performance claim is a test (allocation, size) or a BenchmarkDotNet result with its limits stated beside it.
  A measurement that contradicts the expectation wins, and the change is reverted (`in` receivers were; the history is in
  `docs/result/design-decisions.md`).
- **Benchmark the way the code is used.** Benchmark a *chain*, not an isolated call: a result passed between calls is a temporary, which
  behaves differently from a field. A microbenchmark that flatters a change proves little.
- **Allocation numbers are exact; timings are not.** Tiered PGO inlines delegate calls inconsistently, so identical code measured from
  1.4 to 8.9 ns. Repeat runs, prefer allocation counts, and do not rank sub-10 ns synchronous chains.
- **The success path allocates nothing.** No LINQ, closures, `params` arrays, boxing or string formatting on it; use `static` lambdas in
  examples and benchmarks. Failures may allocate, but not more than they have to.
- **Structs where it matters, by value.** `Result`/`Result<T>` are readonly structs passed by value. `Error` is a class on purpose: it keeps
  `Result` one pointer wide. Do not make a type a struct, or pass by `in`, without a chain benchmark that shows the gain.
- **A result has two states, and `default` is a success.** `Result` stores one `Error?`: `null` is a success, so `default(Result)` is
  `Result.Success()` and `default(Result<T>)` is `Result.Success(default(T))`. There is no third "uninitialized" state, no marker object and
  nothing to check for or to throw about. Keep it that way: do not add a member that treats a `default` result differently, and build a failure
  only with a non-null error (the value of a failure is always `default`). `Result<T>.Succeeded` and `Failed` are `AggressiveInlining` on purpose:
  without it the JIT stopped inlining the `ValueTask` railway methods into their callers and `Bind` on a `ValueTask` went from 9 to 17 ns. Check a
  change to these types against `ValueTaskReceiver`, not only against the synchronous chains.
- **Async: complete synchronously when possible.** An already completed `Task`/`ValueTask` takes a fast path without a state machine, and
  a synchronous receiver with an asynchronous delegate hands back the delegate's task where it can. A step that leaves the result as it was
  hands back the task it received (`KeepsValue` in the generator catalog; never set it on an operation that can change the value).
- **An error is built in one go.** `Error` is immutable, so every `With...` copies it: a factory that creates an error and then calls `With...`
  allocates two (or three). Use the private constructor (`ErrorAllocationTests` pins this).
- **Argument checks stay** (they cost about 0.5 ns and give an early, clear exception); do not remove them to win a benchmark.

## Structure

- **Layout.** `src/<Project>`, `test/`, `benchmarks/`, `samples/`, `docs/<package>/`. Folders and project files **omit the product prefix**
  (`Result`); `Directory.Build.props` adds `ProductPrefix` (`Wyzely.Functional`) to `AssemblyName` and `RootNamespace`.
  Names follow `Wyzely.{Product}.{...}`: the root namespace is `Wyzely.Functional` for every package (the `Result` project sets its
  `RootNamespace` to the prefix itself), and the package ID adds the capability (`Wyzely.Functional.Result`).
- **The solution file lists every file.** `Wyzely.Functional.slnx` contains the projects and also all files under `.github/`, all files under
  `docs/` and the other files in the repository root. When you add, rename, move or delete such a file, update the solution file in the same
  change.
- **Packages are independent.** A package never references a sibling just for convenience; the one package that connects the others is
  the umbrella `Wyzely.Functional`, which references them and adds only what lies between them.
- **Split a type into partial files by concern**: state, access, conversions, equality, formatting, factories (see `Result.*.cs`,
  `Error.*.cs`, `Maybe{T}.*.cs`). One type per file, the file named after the type; partial parts are named `Type.Concern.cs` and start
  with a `<content>` comment.
- **A namespace must never be named like a public type of its parent.** The type `Wyzely.Functional.Maybe` rules out any namespace that
  starts with `Wyzely.Functional.Maybe.` (the compiler reports a clash between the imported type and the namespace). The projects around
  Maybe therefore keep their folder and assembly names (`Maybe.Tests` is the assembly `Wyzely.Functional.Maybe.Tests`) but set a
  single-word root namespace (`Wyzely.Functional.MaybeTests`, `MaybeCodeGeneration`, `MaybeAnalyzers`, `MaybeBenchmarks`). The same holds for the type
  `Wyzely.Functional.Result`: its projects keep `Result.*` folder and assembly names and use `ResultTests`, `ResultCodeGeneration`,
  `ResultAnalyzers`, `ResultAnalyzersTests`, `ResultBenchmarks` and `ResultSamples`.
- **Shared code is shared as source, never as an assembly.** `src/Shared/Internal` (argument guards, hash combining, the nullable-attribute
  polyfills, the settings holder) is compiled into every library project as `internal` code by `Directory.Build.props`, and
  `src/Shared/CodeGeneration` (the generator engine) into both generator projects. A separate assembly would have to ship in the package or
  expose its types, which would let a consumer reach them. What differs between `Result` and `Maybe` in the generator is one
  `GeneratorProfile`; the number of values `Zip` combines is `GeneratorSettings.MaxZipArity`, defined once. The two analyzer projects share
  `src/Shared/Analyzers/OperationHelpers.cs` and the two code fix projects the base class `RewriteCodeFixProvider.cs` the same way (linked in their project files; they need
  the Roslyn references, so `Shared.csproj` only lists them), and the two analyzer test projects share `test/AnalyzerTesting`. **Duplication is
  a failure of the SonarCloud gate** (new code may not be duplicated by more than a few percent): when a new `Result` rule has a counterpart for
  `Maybe`, put what they have in common in one of these places instead of copying it.
  `src/Shared/Shared.csproj` is the same code (both folders; the generator engine on net8.0 and later) as a real project, for the IDE and for
  `test/Shared.Tests`; nothing references it and it is not packable (`Directory.Build.props` treats the name `Shared` as a tool project). Its
  tests run on net8.0 and net10.0 only, so the `netstandard2.0` branches of the shared code are compiled but not run.
- **The internal helpers have a namespace that matches their folder.** The types in the `Internal` folders (`src/Shared/Internal`,
  `src/Result/Internal`, `src/Maybe/Internal`) are in `Wyzely.Functional.Internal`, which `Directory.Build.props` imports into every library
  (and `Shared.csproj` and `Shared.Tests` import it themselves) with a global using. The polyfills of BCL attributes are the exception: the
  compiler finds them by the namespace of the type they replace, so `.editorconfig` relaxes IDE0130 for `src/Shared/Internal/*Attribute.cs`.
- **Nullable value types and nullable references behave the same.** Wherever a delegate produces the value of a maybe (`Map` in all nine
  variants, `Select`, the `SelectMany` result selector, the `Zip` selectors), there is a second overload constrained to `where TResult : struct`
  whose delegate returns `TResult?`, so an `int?` lambda gives a `Maybe<int>` and `null` becomes none, like a `string?` lambda gives a
  `Maybe<string>`. Add the pair, with tests for both, whenever a new operation takes such a delegate. The one thing C# cannot express is an
  implicit conversion from `int?`; `Maybe.From` covers it.
- **Settings must be respected, and that is tested.** Every property of `ResultOptions` and `MaybeOptions` has a test that proves it takes
  effect; `ContinueOnCapturedContext` is tested through real awaits with a context that counts posts (`ProcessSettingsTests`), and a setting
  made on one thread is read on another. A new setting needs the same.
- **AOT compatibility is run, not claimed.** `samples/Aot.Samples` exercises all three libraries and CI publishes it as a Native AOT app
  without a trim or AOT warning and runs the binary (job `aot`). Do not use reflection, `dynamic` or runtime code generation in a library.
- **Samples are teaching code, and they are tested.** `samples/<Package>.Samples` are console projects (a name ending in `Samples` makes
  a project a tool project: not a library, not packable, documentation rules relaxed in `.editorconfig`). Each example is a public static
  class with `Run(TextWriter output)`, and `test/Samples.Tests` runs every one and asserts its output, so a sample cannot rot unnoticed.
  Samples use the public API only and follow the same no-suppression rule as everything else. Every example may change and restore
  process-wide settings, so `Samples.Tests` disables test parallelization for the whole assembly (`AssemblyInfo.cs`) instead of relying on each
  class to opt in. CI runs on Linux only: the libraries make no operating-system calls and the tests normalise line endings and paths.
- **The umbrella project overrides the naming rule.** `src/Functional` and `test/Functional.Tests` set `AssemblyName` and `RootNamespace`
  themselves, because the product prefix plus the folder name would give `Wyzely.Functional.Functional`. The package is plain
  `Wyzely.Functional`, and holds only what lies between the other packages (`MaybeResultExtensions`).
- **No line is wider than 160 characters.** `LineWidthTests` in `test/Shared.Tests` checks every C# file. Break a long line where a person
  would: one argument per line, a long string as `+`-joined pieces, a long documentation element as an opening tag, wrapped text and a closing
  tag. Do not force the limit where it makes no sense: a method name, for example that of a generated test, stays in one piece (the test
  exempts a method declaration whose only long part is its name), and test names are never shortened to fit. The generators wrap their own
  output (`LineWrapper`, called by `CodeWriter`), so generated files are never wrapped by hand.
- **Generated code is not edited.** The railway operations in `Generated/` and their tests are produced by
  `src/Result.CodeGeneration` from the catalog `Operations.cs`. Change the catalog, regenerate, and let
  `GeneratedFilesTests` confirm the output is current. Use `-- --stubs` to watch the generated tests fail first.
- **Types are `sealed` and `internal` by default**; make a type `public` only when a consumer needs it, and leave it unsealed only when it is
  designed as a base class.
- **Expected failures are returned, not thrown**, in code that uses this library. Exceptions are for bugs and misuse (argument errors,
  reading the value of a failed result).
- **Settings are the only global state** (`ResultConfiguration`, `MaybeConfiguration`; one per package, never shared), read through one
  internal accessor, changed atomically, and reset in tests that touch them (collection `ProcessWideConfiguration`, which disables test
  parallelism).
- **Do not suppress warnings to make the build pass.** There is no `NoWarn` in this repository. Fix the cause. A suppression in code needs a
  one-line reason and is the exception: use `[SuppressMessage("Category", "XXXX", Justification = "why")]` on the narrowest member or type
  that holds the diagnostic, never `#pragma warning disable`. Before you add one, check that the diagnostic really fires without it (the
  rule may not apply, or may not be enabled). Per-folder relaxations live in `.editorconfig`.
- **SonarCloud must pass.** The `sonarcloud` job of `ci.yml` is part of the required check `CI`; it fails when the SonarCloud quality gate
  fails. Details and the one-time setup are in `docs/releasing.md`.
- **The build reports what SonarCloud reports.** A SonarCloud analysis applies the Sonar way profile, so its issues must be fixed (or suppressed
  as above). The SonarAnalyzer package leaves nine of those rules off by default because they take parameters (S107, S110, S1192, S1479, S2342,
  S2436, S3776, S6664, S6669); `.editorconfig` switches them on, and switches off for test projects those that SonarCloud applies to main code
  only (S1192, S6664, S6669). When the `SonarAnalyzer.CSharp` version is updated, check again which Sonar way rules are off by default (the
  rules carry the custom tags `SonarWay`, `MainSourceScope` and `TestSourceScope`). The analyzer cannot report the other parts of the
  quality gate (coverage, duplication, security hotspots).

## Tests

- **xUnit v3** with its built-in `Assert`; use `Assert.Multiple` when checking several things. Substitute nothing that can be a real
  object: results and errors are values.
- **Names** read as `Subject_behavior_when_condition` (`GetError_throws_when_the_result_succeeded`); underscores are allowed only in test
  method names. Test classes are `public sealed`, test methods are `internal`. One test class per subject, `<Subject>Tests`, mirroring
  the production namespace.
- **Arrange, act, assert**, separated by blank lines, without comments when the intent is clear.
- **Test through the public API.** Internal members are reached through `InternalsVisibleTo` only where the public API cannot establish the
  behavior (the generator, the size and allocation checks).
- **Cover the whole matrix.** An operation exists in sync/`Task`/`ValueTask` receiver times delegate variants; the generated matrix tests
  cover them all. Handwritten code that has variants gets tests for each.
- **Allocation and size are tested**, not described (`PerformanceTests`): `GC.GetAllocatedBytesForCurrentThread` for the success path,
  `Unsafe.SizeOf` for the layout.
- **Analyzers and code fixes are tested in a real Roslyn workspace** (`Analyzers.Tests`): snippets that must and must not be reported, and the
  code after the fix.
- **Tests that change process-wide state** belong to the `ProcessWideConfiguration` collection and restore the defaults.

## Commands

Run from the repository root.

```shell
dotnet build Wyzely.Functional.slnx -c Release                     # Release treats warnings as errors
dotnet run --project test/Result.Tests -f net10.0                 # tests are executables (Microsoft.Testing.Platform)
dotnet run --project test/Result.Analyzers.Tests
dotnet run --project src/Result.CodeGeneration                    # regenerate (add -- --stubs for the red phase)
dotnet run --project test/Maybe.Tests -f net10.0                   # Maybe: unit tests
dotnet run --project test/Maybe.Analyzers.Tests                    # Maybe: analyzer tests
dotnet run --project src/Maybe.CodeGeneration                      # Maybe: regenerate (add -- --stubs for the red phase)
dotnet run --project test/Functional.Tests -f net10.0              # the umbrella package: interop tests
dotnet run --project samples/Maybe.Samples                         # a sample (also Result.Samples, Functional.Samples)
dotnet run --project test/Samples.Tests -f net10.0                 # the samples are tests too
dotnet run -c Release --project benchmarks/Result.Benchmarks -- --filter "*SyncSuccess*"
dotnet pack src/Result -c Release
```

- `global.json` pins SDK 11 (the exact version, with `allowPrerelease`, while it is a release candidate). That one SDK builds every target
  framework: `netstandard2.0`, `net8.0`, `net10.0` and `net11.0` (C# 15 unions, for `Result<T>` and `Maybe<T>`). `net11.0` is a target
  framework like the others, not an opt-in; the union code sits behind `#if NET11_0_OR_GREATER`. The tests run on the .NET 8, 10 and 11
  runtimes, so all three must be installed (the SDK 11 brings the last). After the general availability of .NET 11, pin the stable 11.0.x.
- Line endings follow `.gitattributes` (`text=auto`); the generator writes CRLF and the tests normalize, so do not fight it.
- Before saying a task is done: Release build clean, the tests of **every** target framework green, the generated files current, and the
  documentation and examples updated where behavior changed.

## Documentation

- **Audience: consumers.** READMEs start with what the thing is for and a short example, then why, how to use it, and where to go next.
  Contributor material (building, the generator) lives at the bottom or in `docs/<package>/contributing.md`.
- **The package README is the NuGet page**, so its links are absolute and its examples are self-contained (no types that are never
  defined). Long material goes into pages under `docs/<package>/`.
- **Be honest about trade-offs**, including when to use a different library, and about what was not verified.
- **Say where the code comes from.** The root README states that this code is built on code with production history and was expanded,
  improved and documented with Claude Code. Do not remove or soften that; keep new claims in line with it.

## Pull requests and releases

- **Trunk-based.** `main` is always releasable; work happens in short-lived feature branches and reaches `main` only through a pull request.
- **A pull request must build without errors or warnings and pass every test** on every target framework. CI (`.github/workflows/ci.yml`)
  enforces this; make it pass locally first (see Commands). Do not weaken a workflow, a rule or a test to make a check pass.
- **The packages are released together, with one version, by a tag on `main`** named `v<version>` (`v1.2.3`); MinVer gives every packable project that
  version. Never create, move or delete a release tag unless asked, and never publish by hand. Details: `docs/releasing.md`.
- **A packable project must not set its own `MinVerTagPrefix`:** all packages share the prefix `v` from `Directory.Build.props`; CI validates this.
- **Documentation-only changes skip the build.** CI always starts, but `.github/scripts/Test-CodeChanged.ps1` decides whether anything other than
  documentation changed; when not, only the link check runs. A file that is not on that list counts as code. Markdown files that are build
  inputs (`AnalyzerReleases.*.md`) are code. If you add a kind of file that cannot affect the build, add it to the list and to its test.
