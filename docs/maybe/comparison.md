# How it compares

[Back to the Maybe overview](../../src/Maybe/README.md)

This page compares `Wyzely.Functional.Maybe` with the "maybe" type of five other libraries: CSharpFunctionalExtensions (`Maybe<T>`),
LanguageExt (`Option<A>`), Funcky (`Option<T>`), Optional (`Option<T>`) and OneOf (used as `OneOf<T, None>`, since it has no option type of its
own). FluentResults, ErrorOr and Ardalis.Result have no maybe type; they appear in the [Results comparison](../result/comparison.md), which
also lists the version, release date, targets and repository activity of every library. Read that table first for the question "is this library
alive?".

How the facts were found, on 2026-10-18: the latest stable package of each library was downloaded from nuget.org and its public types and
extension methods listed by loading the assemblies; `default` and `null` handling were checked by running small programs; speed and allocation
are in [Performance](performance.md). Nothing is read from marketing pages, "none found" means the probe found nothing, and the absence of a
feature is not a statement about a roadmap. Check the current versions before relying on a cell.

## The type

| | **Wyzely.Functional.Maybe** | CSharpFunctionalExtensions 3.7.0 | LanguageExt 4.4.9 | Funcky 3.6.1 | Optional 4.0.0 | OneOf 3.0.271 |
|---|---|---|---|---|---|---|
| Type | `Maybe<T>` | `Maybe<T>` | `Option<A>` | `Option<T>` | `Option<T>` | `OneOf<T, None>` |
| Is a struct | yes | yes | yes | yes | yes | yes |
| Size, `T` = `int` / `string` | 8 B / 16 B | 8 B / 16 B | 8 B / 16 B | 8 B / 16 B | 8 B / 16 B | 12 B / 16 B |
| Constraint on `T` | `notnull` | none found | none found | none found | none found | none |
| `default` | none | none | none | none | none | **the first type, with its default value**: `default(OneOf<int, None>)` holds a `0` |
| Interfaces on the type | `IEquatable`, `IComparable` | `IEquatable`, `IMaybe` | `IEquatable`, `IComparable`, `IEnumerable`, `IOptional`, `ISerializable` | `IEquatable`, `IComparable`, `IOption` | `IEquatable`, `IComparable` | not examined |
| Ordering operators (`<`, `>`, ...) | yes | none | yes | yes | yes | not applicable |
| Implicit conversion from `T` | yes (a null reference becomes none) | yes | yes | yes | none | yes (a union converts implicitly from its cases) |

## What happens with `null`

These were run, not read.

| | Putting `null` into the "some" factory | Making a maybe from something that may be `null` |
|---|---|---|
| **Wyzely.Functional.Maybe** | `Maybe.Some<string>(null)` throws `ArgumentNullException` | `Maybe.From` gives none |
| CSharpFunctionalExtensions | there is no separate "some" factory | `Maybe<string>.From(null)` gives none |
| LanguageExt | `Some(null)` throws `ValueIsNullException` | `Optional(null)` gives none |
| Funcky | `Option.Some<string>(null)` throws `ArgumentNullException` | `Option.FromNullable` gives none |
| Optional | `Option.Some<string>(null)` **gives a some that holds `null`** (`HasValue` is `true`) | `SomeNotNull()` gives none |
| OneOf | not applicable | not applicable |

A "some" that can hold `null` is the case this type exists to remove, so it is worth knowing that Optional allows it unless you
use `SomeNotNull`.

## API surface

| | **Wyzely.Functional.Maybe** | CSharpFunctionalExtensions | LanguageExt | Funcky | Optional | OneOf |
|---|---|---|---|---|---|---|
| Operations on the type (names) | `Map`, `Bind`, `Filter`, `Tap`, `TapNone`, `Or`, `ValueOr`, `Match`, `Switch`, `Select`, `SelectMany`, `Where`, `Zip` (2 to 5), `Flatten`, `AsNullable` | `Map`, `Bind`, `Where`, `Tap`, `TapNoValue`, `Or`, `Match`, `Execute`, `ExecuteNoValue`, `Flatten`, `Choose`, `GetValueOrDefault`, `GetValueOrThrow`, `Select`, `SelectMany`, `BindOptional`, `Optional`, `Required` | about 50 as members plus extensions: `Map`, `Bind`, `Filter`, `Match`, `IfNone`, `IfSome`, `Fold`, `Apply`, `Sum`, `Product`, `Traverse`, `Sequence` and many more | `Select`, `SelectMany`, `Where`, `Match`, `Switch`, `GetOrElse`, `OrElse`, `AndThen`, `Inspect`, `InspectNone`, `Flatten`, `WhereSelect`, `Sequence`, `Traverse` | `Map`, `FlatMap`, `Filter`, `Match`, `MatchSome`, `MatchNone`, `Else`, `Or`, `ValueOr`, `Exists`, `Contains`, `NotNull`, `Flatten`, `Select`, `SelectMany`, `Where` | `MapT0`, `MapT1`, `Match`, `Switch`, `TryPickT0`; no `Bind` or `Filter` |
| Extension methods in the assembly (all types) | 122 | 1,664 | 5,782 | 781 | 47 | 0 |
| Of those, mentioning `Task` / `ValueTask` | 51 / 51 | 751 / 634 | 840 / 140 | 10 / 139 | 0 / 0 | 0 / 0 |
| Asynchronous forms of the operations | all nine railway operations, each in nine variants (receiver and delegate each synchronous, `Task` or `ValueTask`), under the same name | many, under the same name, for `Task` and `ValueTask` | `...Async` names on `Task<Option<A>>`, and a separate `OptionAsync<A>` type | awaiters for options; no `Map` or `Bind` for `Task<Option<T>>` found | none | none |
| Lookups on a sequence returning it | `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone` (read-only dictionaries) | `TryFirst`, `TryLast`, `TryFind` | `HeadOrNone`, `LastOrNone`, `Find`, `Head` and others, for its own collection types too | `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `MaxOrNone`, `MinOrNone`, `AverageOrNone`, `AggregateOrNone` | `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone` | none |
| Keeping the values of the somes in a sequence | `Choose` | `Choose` | `Somes` | `WhereSelect` | `Values` | none |
| From a nullable | `Maybe.From` | `AsMaybe` | `Optional` | `Option.FromNullable` | `SomeNotNull`, `ToOption` | none |
| To a nullable | `AsNullable` (value types) | `AsNullable` | `ToNullable` | `ToNullable` | `ToNullable` | none |
| To and from a result type | `ToResult` and `ToMaybe` in the umbrella package `Wyzely.Functional` (a separate package) | `ToResult`, `ToUnitResult`, `ToInvertedResult`, and `AsMaybe` from a result, in the same package | `ToFin`, `ToEither`, `ToValidation` and others, to its own types | `ToEither`, to its own type | `WithException` (to `Option<T, TException>`) | none |
| Analyzers in the package | yes (`WYZ0201`) | no | no | yes (built-in analyzers and code fixes) | no | no |
| Dependencies, per nuspec | none on `net8.0` and later | none | ten, on its only target | none on `net5.0` and later | none | none |

The counts only show how much of each surface exists. They are whole-assembly counts, so for CSharpFunctionalExtensions, LanguageExt and Funcky
they include everything else the library does, and they are not a measure of quality.

## What each one is good at, and where it is limited

**CSharpFunctionalExtensions** is the one to look at if you also want its `Result` types, value objects and entities from the same package. Its
`Maybe` has `Execute`, `Choose`, `Optional`/`Required` bridges and a broad asynchronous surface. It has no ordering operators and no analyzer.

**LanguageExt** has the largest `Option<A>`: about fifty members, folds, `Apply`, arithmetic over options, `Traverse` and `Sequence`, and
conversions to all of its other types. If you want the whole functional toolbox it is the choice; if you want only an option, you take
ten dependencies and a package of 1,823 types with it. Version 4 is the stable line, and version 5 (in beta) changes the API. Its asynchronous work
is a separate type, `OptionAsync<A>`.

**Funcky** has a complete `Option<T>` with the widest set of sequence helpers (`MaxOrNone`, `AverageOrNone`, `AggregateOrNone`,
`Traverse`), ships analyzers, targets `net10.0`, and is actively maintained. It is a small project (55 stars) with a large API, and it
has no `Map` or `Bind` for `Task<Option<T>>`; in the benchmarks a synchronous chain allocated 24 B where this library allocated nothing.

**Optional** is a small, mature option type with the usual operations, `Exists`, `Contains` and `MatchSome`/`MatchNone`. It has not had a release since 2018,
has no asynchronous support, lets a "some" hold `null`, and has no implicit conversion. A synchronous chain
in the benchmarks allocated 24 B.

**OneOf** has no option type: `OneOf<T, None>` works for "a value or nothing" and does not allocate, but there is no `Bind`, `Filter` or
asynchronous support, a `default` is a "some" holding the default value, and the package has had no commit on its default branch since May 2024.

**This library** is deliberately small. It does not have what the table shows others have: arithmetic and `Apply` (`Add`, `Subtract`, `Product`,
`Divide`, `Sum`, `Apply`), `Sequence`/`Traverse` and folds in LanguageExt, `Execute` and `ExecuteNoValue` in CSharpFunctionalExtensions, and
`Contains`, `Exists` and `NotNull` in Optional. [Scope](scope.md) says why. It is also not published yet, so none of the others' track records
applies to it.

## What was not compared

- Behavior beyond `default` and `null`: equality and ordering of the absent state, `ToString`, serialization, and thread-safety.
- Anything on a runtime other than .NET 10 on Windows; the `netstandard2.0` build of this library.
- How much of each library's surface is *good*: the counts are counts.
