# Wyzely: Functional

[![CI](https://github.com/WyzelyDk/Functional/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WyzelyDk/Functional/actions/workflows/ci.yml?query=branch%3Amain)
[![Line coverage](https://wyzelydk.github.io/Functional/coverage/badge_linecoverage.svg)](https://wyzelydk.github.io/Functional/coverage/)
[![Branch coverage](https://wyzelydk.github.io/Functional/coverage/badge_branchcoverage.svg)](https://wyzelydk.github.io/Functional/coverage/)

[![Wyzely.Functional](https://img.shields.io/nuget/v/Wyzely.Functional?label=Wyzely.Functional)](https://www.nuget.org/packages/Wyzely.Functional)
[![Wyzely.Functional.Result](https://img.shields.io/nuget/v/Wyzely.Functional.Result?label=Wyzely.Functional.Result)](https://www.nuget.org/packages/Wyzely.Functional.Result)
[![Wyzely.Functional.Maybe](https://img.shields.io/nuget/v/Wyzely.Functional.Maybe?label=Wyzely.Functional.Maybe)](https://www.nuget.org/packages/Wyzely.Functional.Maybe)
[![License: MIT](https://img.shields.io/github/license/WyzelyDk/Functional)](LICENSE)

[![Quality gate status](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=coverage)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)

[![Reliability issues](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=software_quality_reliability_issues)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Security issues](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=software_quality_security_issues)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Maintainability issues](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=software_quality_maintainability_issues)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)

[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=WyzelyDk_Functional&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=WyzelyDk_Functional)

**Fast, fluent, dependency-free functional building blocks for .NET: independent, explicit, and honest about their trade-offs.**

```csharp
var message = ParseAge("42")                                  // Result<int>: a number, or a described failure
    .Ensure(age => age >= 18, Error.Validation("Must be an adult."))
    .Map(age => $"Welcome, age {age}.")
    .Match(text => text, error => $"Rejected: {error}");     // "Welcome, age 42."
```

Hello! This is a set of .NET packages for two things that business code keeps needing: an expected failure that is a value instead of an
exception (`Result`), and an absent value that is not `null` (`Maybe`). You take the pieces you need and nothing else: every package
stands on its own, has no dependencies it does not need, and says in plain words what it will and will not do.

## What "functional" means here

Not category theory, and no new vocabulary to learn. It means two ordinary things:

- **Failure is a value.** A missing order, an invalid input or a conflicting update is an *expected* outcome. It is returned in the signature
  (`Result<T>`) instead of thrown, so the caller sees it, has to deal with it, and can chain on it.
- **Absence is a value.** "There may be no customer" is a value too (`Maybe<T>`), not a `null` that is discovered at run time.

Exceptions stay for bugs and the unexpected. These packages are the small amount of machinery that keeps the two worlds from leaking into
each other.

## Fluent, from the first step to the last

The point of these types is code that reads as one chain: you describe the happy path, and the failure (or the absence) skips the rest on
its own. Both types support the whole railway, not a sample of it.

### Result: failure is a value

- **Every step has a fluent form:** `Map`, `Bind`, `Tap`, `TapError`, `MapError`, `Ensure`, `Recover` and `Compensate` continue the
  chain, and `Match` and `Switch` leave it ([the full list](docs/result/the-railway.md)).
- **Synchronous and asynchronous steps mix freely:** each step accepts a plain, `Task` or `ValueTask` result and delegate, so a chain
  needs no `await` in the middle ([async](docs/result/async.md)).
- **Query syntax and combining work too:** LINQ `from ... select` and `Zip` follow the same rules.

### Maybe: absence is a value

- **Every step has a fluent form:** `Map`, `Bind`, `Filter`, `Tap`, `TapNone` and `Or` continue the chain, and `ValueOr`, `Match` and
  `Switch` leave it, with the same names and the same shape as on `Result` ([the full list](docs/maybe/the-railway.md)).
- **Synchronous and asynchronous steps mix freely:** each step accepts a plain, `Task` or `ValueTask` maybe and delegate, so a chain
  needs no `await` in the middle ([async](docs/maybe/async.md)).
- **No `null` in the chain:** a missing value is `None` and skips the rest, so you never check for `null` between steps. With the umbrella
  package `Wyzely.Functional`, `ToResult()` moves from "absent" to "failed" without leaving the chain ([interop](docs/functional/interop.md)).

## Where this code comes from

This is not code written from scratch. These libraries are built on code that has already been **in production use in many codebases**.
For this repository that code was **expanded, improved and documented with Claude Code**: more of the surface, a much larger test suite,
benchmarks, analyzers and code fixes, and the documentation you are reading.

You should know that, and what it does and does not mean:

- **The design decisions are the maintainers'**: what goes in, what is declined, how the API reads. Claude Code did a large share of the
  implementation, the test writing and the documentation.
- **The production history belongs to the code these packages grew from.** The packages as they stand now are reworked and expanded, and
  have not previously been published or run in production *as packages*, so do not read the lineage as a guarantee.
- **What backs the claims is not who wrote the code but what you can check:** the tests (including generated ones for every variant),
  the benchmarks with their limits written next to them, and the written scope. Where a measurement contradicted our expectation we
  changed the code and wrote it down ([examples](docs/result/design-decisions.md#things-we-measured-and-changed)).
- **How we work with the assistant is written down** in [`AGENTS.md`](AGENTS.md), which is also what the assistant reads.

## What we are trying to do

Business code keeps needing the same two small things: a way to say that an operation can fail in an expected way without throwing, and a
way to say that a value may be absent without `null`. Every team rebuilds them, usually slightly differently, and the details (how a failure
is described, what a default value means, which mistakes the compiler can catch) end up being decided by accident.

The goal of this repository is to make those decisions **once, deliberately, and visibly**, so that code built on it is consistent, quick,
and hard to get subtly wrong. It is the foundation for the solutions Wyzely builds, published so that anyone who shares our opinions can use
it. It is deliberately only about these functional types; other building blocks will live in their own repositories.

## The opinions

These libraries are opinionated, and it is better to say so up front than for you to find out in the middle of a project.

- **Explicit beats implicit.** Nothing converts to a result behind your back; a value is a success only if you said so. (`Maybe` has one
  deliberate exception, where a plain value converts to a maybe and `null` means none: [why](docs/maybe/design-decisions.md#an-implicit-conversion-where-null-means-none).)
- **Make the wrong thing hard.** Mistakes such as ignoring a result or reading the error of a success or the value of a none are flagged
  by analyzers, and where possible a code fix is offered.
- **Fast by design, with receipts.** Structs where it matters and nothing allocated on the hot path. A performance claim is a test or a
  benchmark with its limits written next to it, or it is not made.
- **Small and independent.** One package, one job. Taking `Result` never makes you take `Maybe`, and nothing pulls in a dependency graph.
- **The tools come in the box.** Analyzers and code fixes ship inside the package, so you do not have to know they exist.
- **We say no.** Every package has a written scope. A feature that does not fit is declined, because a public API can never be taken back.
- **Honest documentation.** What was measured, what was only compiled, and when you should use something else instead are all stated.

If you disagree with one of these, that is fine. Where it is reasonable, behavior is a setting with a sensible default rather than a hidden
constant; where it is not, there are other good libraries, and we say which in the docs.

## Is it for you?

**Probably yes** if you build business applications on .NET, like explicit code over convenience, and want the compiler to help.

**Probably not (yet)** if you need a large ecosystem of add-ons today, a package with a long public track record, or
`return value;` to compile as a success from a `Result` method. In that case an established library will serve you better, and
here are the comparisons: [Result](docs/result/comparison.md) and [Maybe](docs/maybe/comparison.md).

## Why a library, when writing your own Result and Maybe is easy?

It is easy, and for a small project it can be the right answer. This is a complete, working pair:

```csharp
public readonly record struct SimpleResult<T>(T? Value, string? Error)
{
    public bool IsSuccess => Error is null;

    public static SimpleResult<T> Ok(T value) => new(value, null);

    public static SimpleResult<T> Fail(string error) => new(default, error);
}

public readonly record struct SimpleMaybe<T>(T? Value, bool HasValue) where T : notnull
{
    public static SimpleMaybe<T> Some(T value) => new(value, true);

    public static SimpleMaybe<T> None => default;
}
```

A dozen lines, no dependency, and you control everything. What those dozen lines do not give you is the rest of what business code ends up
needing, and each item below is something this repository has and a hand-written type usually acquires one bug report at a time. Every
number is counted from the source or pinned by a test.

- **An error that is more than a string.** One `Error` type with a message template and structured details, a kind (`NotFound`,
  `Validation`, `Conflict`, `Unauthorized`, `Forbidden`, `Unavailable`, `Timeout`, `Cancelled`, ...), an optional code, a `IsTransient` flag for
  retry decisions, the causing exception, context frames added as the error travels up, aggregation of many errors into one, and
  redaction: a detail marked sensitive is shown by `Message` and hidden by `RedactedMessage` and `ToRedactedString()` ([errors](docs/result/errors.md)).
- **A railway that needs no `await` in the middle.** Every operation exists for a plain, a `Task` and a `ValueTask` receiver, each with a plain, `Task`
  or `ValueTask` delegate: nine variants. They are generated, not hand-written: `src/Result/Generated` holds 215 files (207 for the 10 operation names
  of the railway, 8 for `Zip`) and `src/Maybe/Generated` holds 102 (90 for 9 operations, where `Map` has a second form for nullable value types, and 12 for `Zip`).
  `GeneratedFilesTests` fails if they are out of date. Writing that matrix by hand is the part that is no longer "easy" ([async](docs/result/async.md)).
- **Analyzers and code fixes in the package.** `WYZ0101` (a `Result` is not used, with a code fix), `WYZ0102` (check the state of a `Result`
  before `GetError()` or `GetValue()`) and `WYZ0201` (check that a `Maybe` has a value before `GetValue()`). They are tested in a real Roslyn
  workspace ([rules](docs/analyzers.md)).
- **No hidden third state.** A struct always has a `default`, and here it always has a meaning: `default(Result)` is a success,
  `default(Result<T>)` is a success holding `default(T)`, and `default(Maybe<T>)` is none. Nothing throws because a value was never assigned.
- **Nullability that is tested.** `[NotNullWhen]`, `[MaybeNullWhen]` are pinned by consumer
  tests that compile snippets and assert which warnings appear (`ConsumerNullabilityTests` in both analyzer test projects).
- **Settings instead of constants.** Separators, messages, the redaction text, which exceptions `Result.Try` catches, and whether awaits continue on the
  captured context are options with defaults (`ResultConfiguration`, `MaybeConfiguration`), one per package.
- **Measured size and allocation.** `PerformanceTests` pins that a `Result` is one reference wide and a `Result<string>` two; that a `Maybe<int>` is
  8 bytes and a `Maybe<string>` a reference plus a flag; that a successful synchronous chain allocates 0 bytes for both types; that a chain ending
  in none allocates nothing, also as a `Task` chain over a reference type (one cached completed task per type). Timings are in the
  benchmarks with their caveats ([Result](docs/result/performance.md), [Maybe](docs/maybe/performance.md)); they are indicative, not a verdict.
- **Collection and LINQ helpers.** `Combine` (every error, in order), `Partition`, `SelectInParallel` and `ForEachInParallel` for results;
  `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone` and `Choose` for maybes; query syntax and `Zip` (2 to 5 values) and
  `Flatten` for both.
- **C# 15 unions.** On `net11.0` both `Result<T>` and `Maybe<T>` are unions, so a `switch` can be checked for completeness
  ([Result](docs/result/unions.md), [Maybe](docs/maybe/unions.md)). The feature is new in the language and in .NET 11.
- **The two types connect.** The umbrella package `Wyzely.Functional` adds `ToResult` and `ToMaybe` in all three receiver kinds ([interop](docs/functional/interop.md)).

**The cost of a dependency is real**, and so are the reasons to write your own:

- Your type is exactly what you need, with nothing to learn. Ours has about the API surface of the pages linked above, and one team's opinions
  (explicit conversions, a `default` result that is a success).
- A dependency has to be kept up to date, and these packages are young.
- If you need `Result<T, TError>` with your own error type, value objects or framework integrations today, this repository declines them on purpose
  ([scope](docs/result/scope.md)); use something else.
- If you only ever return a `bool` or a nullable reference, a library type adds a concept for little in return ([Maybe](docs/maybe/why.md#do-you-need-a-maybe-at-all)).

The honest rule of thumb: write your own while one type and a handful of call sites is all you have; reach for a library when you start
writing the second `Map` overload for `ValueTask`, the third error-formatting helper, or the first bug caused by a `default` result.

## Samples

Three runnable console projects, one example file per theme in a small customer and order domain, each with its scenario and the reason in
comments. Each example prints labelled lines, and `test/Samples.Tests` runs every example and checks the lines that matter, so a sample
cannot drift from what its comments claim.

| Project | Demonstrates | Run |
|---|---|---|
| [`samples/Result.Samples`](samples/Result.Samples) | `Result`: basics, the railway, errors, combining, async, parallel work, exceptions, configuration, unions | `dotnet run --project samples/Result.Samples` |
| [`samples/Maybe.Samples`](samples/Maybe.Samples) | `Maybe`: basics, the railway, collections, async, LINQ, equality, nullability, configuration, unions | `dotnet run --project samples/Maybe.Samples` |
| [`samples/Functional.Samples`](samples/Functional.Samples) | the two together: `ToResult` and `ToMaybe`, a full async workflow in one chain, collections and `Combine`/`Zip`, settings across the packages, analyzers, an order checkout | `dotnet run --project samples/Functional.Samples` |

The samples reference the source projects, not the packages, so the analyzers do not run in them.

## The packages

| Package | Premise | Status |
|---|---|---|
| [`Wyzely.Functional.Result`](src/Result/README.md) | An operation can fail in ways you expect, and the failure should be visible, described and hard to ignore. | [![NuGet](https://img.shields.io/nuget/v/Wyzely.Functional.Result)](https://www.nuget.org/packages/Wyzely.Functional.Result) |
| [`Wyzely.Functional.Maybe`](src/Maybe/README.md) | A value may be absent, and "absent" should not be `null`. | [![NuGet](https://img.shields.io/nuget/v/Wyzely.Functional.Maybe)](https://www.nuget.org/packages/Wyzely.Functional.Maybe) |
| [`Wyzely.Functional`](src/Functional/README.md) | You use both and want them to work together. It depends on the two packages above and adds the methods between them (`ToResult()` and `ToMaybe()`). | [![NuGet](https://img.shields.io/nuget/v/Wyzely.Functional)](https://www.nuget.org/packages/Wyzely.Functional) |

### Which one do I install?

- **Only failures:** `Wyzely.Functional.Result`.
- **Only absence:** `Wyzely.Functional.Maybe`.
- **Both, and how they connect:** `Wyzely.Functional`, which depends on the other two and adds the methods between them.

The packages are independent: using one never forces the other on you. Start with the [Result package](src/Result/README.md) or the
[Maybe package](src/Maybe/README.md): each has a five-minute tour, and each answers why it exists next to the libraries that are already out
there ([Result](docs/result/why.md), [Maybe](docs/maybe/why.md)).

## How things are named

- The **root namespace is `Wyzely.Functional`** for every package; the **package ID adds the capability** (`Wyzely.Functional.Result`),
  and the package that includes the others is plain `Wyzely.Functional`. One `using Wyzely.Functional;` gives you everything you installed.
- Packages are **independent**; only `Wyzely.Functional` references the others, to add what connects them.
- All packages are **released together**, with the same version, by one tag, for example `v1.2.0`.

## Contributing

Ideas and bug reports are welcome. Start with [CONTRIBUTING.md](CONTRIBUTING.md); everyone taking part is expected to follow the
[Code of Conduct](CODE_OF_CONDUCT.md). The most useful thing you can do first is read the scope of the package you have in mind
([Result](docs/result/scope.md), [Maybe](docs/maybe/scope.md)): it says what will not be added and why, and how to propose something that fits.

Building, testing and how the generated code works are in [Building and contributing](docs/result/contributing.md) (and for `Maybe`: [Building and contributing](docs/maybe/contributing.md)); how pull requests are checked and how a package is released is in [CI and releasing](docs/releasing.md). In short:

```shell
dotnet build -c Release
```

You need the .NET 11 SDK (a release candidate for now; `global.json` pins it); it builds all target frameworks, `net11.0` (C# 15 unions) included.

```
src/         the packages (Result, Maybe and the umbrella Functional), their analyzers and code fixes, and their code generators
test/        unit, analyzer, code fix and nullability tests
benchmarks/  BenchmarkDotNet comparisons
samples/     console projects with examples
docs/        documentation pages (one folder per package) and the analyzer rule pages
```

## License

[MIT](LICENSE)
