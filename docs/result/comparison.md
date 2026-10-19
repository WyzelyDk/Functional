# How it compares

[Back to the Result overview](../../src/Result/README.md)

This page compares `Wyzely.Functional.Result` with eight other libraries that can carry a success or a failure: FluentResults,
CSharpFunctionalExtensions, ErrorOr, Ardalis.Result, LanguageExt, OneOf, Funcky and Optional. Some are result libraries, some are general
functional or union libraries that people use as a result type, and the table says which is which.

How the facts were found, on 2026-10-18:

- **Packages:** the latest stable version of each package was downloaded from nuget.org. Target frameworks, dependencies and contents come
  from the package itself, and public types and extension methods from loading the assemblies.
- **Repositories:** stars, last commit and releases come from the GitHub API for the repository each package names.
- **Behavior:** `default`, `null` handling and failed-value access were checked by running small programs against the packages.
- **Speed and allocation:** see [Performance](performance.md), where the same libraries are benchmarked.

Nothing here is read from marketing pages, and the absence of a feature in a package is not a statement about its roadmap. Versions move:
check the current version before relying on a cell. The `Maybe` types of these libraries are compared in the
[Maybe comparison](../maybe/comparison.md).

## The libraries

| Library (version, released) | What it offers as a result | Targets | Repository, on 2026-10-18 |
|---|---|---|---|
| **Wyzely.Functional.Result** (this repository, not published yet) | `Result`, `Result<T>`, one aggregating `Error` | `netstandard2.0`, `net8.0`, `net10.0`, `net11.0` | not published yet |
| [FluentResults](https://github.com/altmann/FluentResults) 4.0.0 (2025-06-29) | `Result`, `Result<T>`, a list of reasons (errors and successes) | `net8.0`, `net9.0`, `netstandard2.0`, `netstandard2.1` | 2,647 stars; last commit 2025-07-10, the release of 4.0 |
| [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) 3.7.0 (2026-03-02) | `Result`, `Result<T>`, `Result<T, E>`, `UnitResult<E>`; also `Maybe`, value objects and entities | `net6.0`, `net8.0`, `netstandard2.0` | 2,833 stars; last commit 2026-03-02, the release of 3.7.0; 101 open issues |
| [ErrorOr](https://github.com/error-or/error-or) 2.1.1 (2026-05-15) | `ErrorOr<T>`, a value or a list of errors | `net8.0`, `net10.0`, `netstandard2.0` | 2,095 stars; last commit 2026-10-08 |
| [Ardalis.Result](https://github.com/ardalis/result) 10.1.0 (2024-10-28) | `Result`, `Result<T>` with a status that mirrors HTTP outcomes | `net6.0`, `net7.0`, `net8.0`, `netstandard2.0` | 1,045 stars; last commit 2026-07-03, after the last release |
| [LanguageExt](https://github.com/louthy/language-ext) `LanguageExt.Core` 4.4.9 (2024-06-26) | `Fin<A>`, inside a complete functional base library | `netstandard2.0` | 7,083 stars; last commit 2026-10-09; version 5 is in beta (`5.0.0-beta-78`, 2026-10-09) |
| [OneOf](https://github.com/mcintyre321/OneOf) 3.0.271 (2024-05-16) | no result type: general unions, `OneOf<T0, T1, ...>`, used here as `OneOf<T, Error<string>>` | `netstandard2.0`, `netstandard1.3`, `net45`, `net35` | 4,058 stars; no commit on the default branch since 2024-05-16 |
| [Funcky](https://github.com/polyadic/funcky) 3.6.1 (2026-10-16) | `Result<T>` (the error is an `Exception`), `Option<T>`, `Either<L, R>` and more | `net5.0` to `net10.0`, `netcoreapp3.1`, `netstandard2.0`, `netstandard2.1` | 55 stars; last commit 2026-10-16 |
| [Optional](https://github.com/nlkl/Optional) 4.0.0 (2018-02-01) | `Option<T, TException>`, next to `Option<T>` | `netstandard2.0`, `netstandard1.0`, `net45`, `net35` | 921 stars; last commit 2018-02-01 |

Things to take from the table:

- **Activity differs a lot.** ErrorOr, Funcky, LanguageExt and CSharpFunctionalExtensions have been changed this year. FluentResults has had
  no commit since July 2025. Ardalis.Result has commits on GitHub after its last package (October 2024); we did not look at what they change.
  OneOf and Optional have had no commit on their default branch for years, and Optional has had no release since 2018 (its `5.0.0-alpha`
  of 2019 never became stable).
- **Newer .NET targets.** Only ErrorOr and Funcky ship a `net10.0` build, and only FluentResults and Funcky a `net9.0` build. The others run
  on current .NET through their `net8.0` or `netstandard2.0` build, which normally works, but they are not compiled against it.
- **LanguageExt 4 is the stable line, 5 is a rewrite in beta.** Everything on this page, including the benchmarks, is about 4.4.9. Version 5
  changes the API, so do not assume the numbers carry over.
- **Stars are not quality.** They are shown because they say something about how many people have looked at a library.

## Type design

| | Result type | Is a struct | Size, `T` = `int` / `string` | Error model |
|---|---|---|---|---|
| **Wyzely.Functional.Result** | `Result<T>` | yes | 16 B / 16 B (`Result` without a value: 8 B) | one immutable `Error`, which can aggregate others: message template, details, kind, code, cause, context frames |
| FluentResults | `Result<T>` | no (class) | a heap object per result | a list of reasons (`IError`, `ISuccess`) with metadata and nested causes |
| CSharpFunctionalExtensions | `Result<T>`, `Result<T, E>`, `UnitResult<E>` | yes | 16 B / 24 B | a string, or your own type with `Result<T, E>` |
| ErrorOr | `ErrorOr<T>` | yes | 16 B / 16 B | a list of `Error`: code, description, type (`Failure`, `Unexpected`, `Validation`, `Conflict`, `NotFound`, `Unauthorized`, `Forbidden`), metadata |
| Ardalis.Result | `Result<T>` | no (class) | a heap object per result | a `ResultStatus` (`Ok`, `Created`, `Error`, `Forbidden`, `Unauthorized`, `Invalid`, `NotFound`, `NoContent`, `Conflict`, `CriticalError`, `Unavailable`), a list of strings and a list of validation errors |
| LanguageExt | `Fin<A>` | yes | 16 B / 24 B | `LanguageExt.Common.Error`, which can wrap an exception |
| OneOf | `OneOf<T, Error<string>>` (you define it) | yes | 16 B / 24 B | whatever type you put in the union |
| Funcky | `Result<T>` | yes | 16 B / 24 B | an `Exception` |
| Optional | `Option<T, TException>` | yes | 16 B / 24 B | any type `TException`, which does not have to be an exception |

A class-based library allocates for every result, success or not: creating one measured 160 B for FluentResults and 64 B (72 B for a
reference payload) for Ardalis.Result. The struct libraries pay in size instead, and a result that carries a reference is one pointer
larger in most of them than in this one. A larger result is copied more often as it travels through a chain.

## What happens in the corners

These were run, not read. The cells say what the code did.

| | `default(...)` | A success holding `null` | The value of a failed result |
|---|---|---|---|
| **Wyzely.Functional.Result** | **a success**; a `default` `Result<T>` holds `default(T)` | allowed | `GetValue()` throws `ResultFailedException` |
| FluentResults | `null`, as it is a class | allowed | `.Value` throws `InvalidOperationException` |
| CSharpFunctionalExtensions | **a success** (`IsSuccess` is `true`) | allowed | `.Value` throws `ResultFailureException` |
| ErrorOr | `IsError` is `false` and `Value` is `0` | an implicit conversion from a null string gives `IsError` = `false` | `.Value` returned `0`, no exception |
| Ardalis.Result | `null`, as it is a class | allowed | `.Value` returned `0`, no exception |
| LanguageExt `Fin<A>` | **a failure**, with an error that says the value was not initialized | `FinSucc(null)` throws `ValueIsNullException` | no `Value` property: use `Match` or `IfFail` |
| OneOf | **the first type with its default value**: a `default` `OneOf<int, Error<string>>` holds a `0` as its first case | not probed | `AsT0` on the other case throws (documented, not probed here) |
| Funcky | `Match` throws `NotSupportedException` | not probed | no `Value` property: use `Match`, `GetOrElse` or `GetOrThrow` |
| Optional | none, with a `default` error (`null` for a string) | not probed | not applicable: it is an option |

Why the first column matters: `default` is what an unassigned field, an array element or a missing deserialized property gives you, so
what it means decides whether a forgotten assignment is noticed. This library, CSharpFunctionalExtensions, ErrorOr and OneOf behave as if a `default`
were a success, LanguageExt as a failure, Funcky throws when it is used, and the class-based libraries give `null`. Only LanguageExt, Funcky
and the class-based libraries make a forgotten assignment visible; here it counts as a success.

## API surface

| | Extension methods in the assembly | Of those, mentioning `Task` / `ValueTask` | `Try` wrapper for throwing code | Settings | Analyzers in the package | Dependencies, per nuspec |
|---|---|---|---|---|---|---|
| **Wyzely.Functional.Result** | 223 | 117 / 115 | yes (`Result.Try`) | `ResultConfiguration`, 9 settings | yes, 2 rules and 1 code fix | none on `net8.0` and `net10.0` |
| FluentResults | 30 | 14 / 12 | yes (`Result.Try`) | `Result.Setup`: logger, factories, default `Try` handler | no | `Microsoft.Extensions.Logging.Abstractions`, `System.Threading.Tasks.Extensions` |
| CSharpFunctionalExtensions | 1,664 | 751 / 634 | yes, several forms | `Result.Configuration`: error separator, default `ConfigureAwait`, default `Try` handler | no | none |
| ErrorOr | 43 | 39 / 0 | no | none found | no | `Microsoft.Bcl.HashCode`, `System.Memory` (`netstandard2.0` only) |
| Ardalis.Result | 32 | 15 / 0 | no | none found | no | `System.Text.Json` (`netstandard2.0` only) |
| LanguageExt | 5,782 | 840 / 140 | yes (the `Try<A>` family) | not examined | no | ten, on its only target (`netstandard2.0`) |
| OneOf | 0 | 0 / 0 | no | none found | no | none |
| Funcky | 781 | 10 / 139 | none found | none found | yes (built-in analyzers and code fixes; the number of rules was not counted) | none on `net5.0` and later; `System.Text.Json` and others on `netcoreapp3.1` and the `netstandard` builds |
| Optional | 47 | 0 / 0 | no | none found | no | none |

How to read the numbers: they count public extension methods in the whole assembly, so for LanguageExt, Funcky and
CSharpFunctionalExtensions they include everything the library does, not only its result type. A method counts as mentioning `Task` or
`ValueTask` when a parameter or the return type does. They show how much of an asynchronous surface exists; they do not measure quality,
and a big surface is a strength or a burden depending on what you need.

## What each one is good at, and where it is limited

**FluentResults** has the richest model of *why* something happened: reasons with metadata, success reasons next to errors, causes nested in
causes, and a logger hook. It is a class, so every result allocates; it has no `Ensure` (a check is a `Bind` step), no `ValueTask` support and
few asynchronous helpers. In the benchmarks it was one to two orders of magnitude slower on synchronous chains, which only matters on hot
paths. There has been no commit since July 2025.

**CSharpFunctionalExtensions** is the most complete: `Maybe`, `Result<T, E>` with your own error type, `UnitResult`, value objects, entities,
JSON helpers, transaction-scope helpers, and an asynchronous surface of about 750 methods that covers `Task` and `ValueTask`. It is actively
maintained. Its limits are the string error of the common `Result<T>` and a `default` that counts as success. Its package has no analyzers.

**ErrorOr** is small and quick: a struct whose errors are a list, with error types that map naturally to HTTP responses. It has `Then`,
`FailIf` and asynchronous forms for `Task`, and it was the closest to this library on the synchronous chains. It has no `ValueTask` support
and no `Try` wrapper, and every failure allocates a list of errors (88 B in the benchmarks). Its `default` is not a failure, and reading
`.Value` of a failed result returns the default value instead of throwing.

**Ardalis.Result** is built around returning an HTTP-like status from a service (`NotFound`, `Forbidden`, `Conflict`, `Unavailable` and so on)
and has a `PagedResult`. It is a class, so every result allocates, and `Value` of a failed result is the default value instead of an
exception. Its package has no `net9.0` or `net10.0` target, and the last release is from October 2024. In the benchmarks it was several
times slower than the struct libraries.

**LanguageExt** is not a result library but a complete functional base library, with `Fin<A>` as its result. If you want the rest (`Option`,
`Either`, `Eff`, immutable collections, parsers and a great deal more), it is the most ambitious package here. It is also by far the
largest: 1,823 public types and ten dependencies on its only target. Its stable line (4.x) is not where development happens any more:
version 5 is in beta and breaking. Its asynchronous work goes through separate types (`Aff`, `Eff`); we found no extension methods on
`Task<Fin<A>>`.

**OneOf** is a union library, not a result library. A `OneOf<T, Error<string>>` is a fine way to say "a value or an error", it does not
allocate, and it was fast in the benchmarks because there is very little to it. There is no `Map`, `Bind`, `Ensure` or `Try`, so a chain is
written with `Match`, and nothing is provided for `Task`. A `default` is a success holding the default value. It has had no commit on its
default branch since May 2024.

**Funcky** is a broad, active functional library (`Option`, `Either`, `Result`, `Reader`, `Lazy`, and many helpers on `IEnumerable`) with
built-in analyzers. Its `Result<T>` carries an `Exception`, which suits code that already works with exceptions and is costly for failures that
are expected: creating a failure measured about 19 μs and 19 KB in the benchmarks, hundreds to thousands of times more than the other libraries
(we did not investigate why). A `default` `Result<T>` throws `NotSupportedException` when matched. We found no `Map` or `Bind` for
`Task<Result<T>>`.

**Optional** is a small option library whose `Option<T, TException>` is usable as a result. The package has had no release since 2018, has no
`Task` support and no `Try`. It still works, and in the benchmarks a four-step chain allocated 24 B and took roughly twice as long as this
library, but treat it as finished software.

## Where this library is the weaker choice

- **It is not published yet.** Every other library here has a public track record that this one does not. See the [scope](scope.md) and the
  [design decisions](design-decisions.md) for what it is and is not.
- **One error type.** `Result<T, E>` with your own error type, as in CSharpFunctionalExtensions, Optional and OneOf, does not exist here.
  The `Error` is rich, but it is one type.
- **A `default` result is a success.** An unassigned field or array slot counts as "it worked", as in CSharpFunctionalExtensions, ErrorOr and OneOf; LanguageExt treats it as a failure and Funcky throws. Collect results with `Combine`, `Partition` or `SelectInParallel` instead of an array that you fill yourself.
- **Failures allocate.** An `Error` is an object (96 B for a plain one, 320 B with a detail and a context frame), where a string literal or a
  struct error costs nothing to create. The success path allocates nothing; a failure that is created on every call does.
- **Chaining on a `Task` allocates.** On a `Task<Result<T>>` that is already completed, each step that changes the result makes a new task
  (160 B for a `Map`, `Ensure`, `Bind` chain; steps that leave the result as it was hand back the task they received).
  Awaiting first and running the synchronous chain on the result allocated nothing and was nearly twice as fast, for this library and for
  the others alike. The `Task` extension methods are there so that a chain reads as one expression, not because they are the cheapest way.
  See [Performance](performance.md).
- **Smaller ecosystem.** No JSON support, no ASP.NET Core integration, no value objects, no transaction helpers.
- **Not every cell is checked everywhere.** The probes and benchmarks ran on .NET 10 on Windows. The `netstandard2.0` build of this
  library and other platforms were not part of the comparison.
