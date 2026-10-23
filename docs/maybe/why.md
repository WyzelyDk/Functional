# Why another Maybe type?

[Back to the Maybe overview](../../src/Maybe/README.md)

There are good option types for .NET already, and the language itself has nullable reference types. This page explains why this one exists,
what a library adds over a few lines you could write yourself, what you give up by choosing it, and when you should simply use something else.
The comparisons with libraries are based on inspecting the packages, running small probes and benchmarks (see [How it compares](comparison.md)
and [Performance](performance.md)); they describe the versions named there, not the libraries in general.

## Do you need a `Maybe` at all?

Often the honest answer is no. With nullable reference types enabled, `User?` already tells the reader that a value may be missing, the compiler
warns about a missing check, and `?.`, `??` and `is not null` are built in and known to every C# developer. `int?` does the same for value
types. If that is enough for your code, a library type adds a concept without adding much.

A `Maybe<T>` starts to pay off when:

- **You chain.** Several steps that each may find nothing (`FindCustomer`, then its email, then its address) read as one chain with
  `Bind` and `Map`, instead of a ladder of `if (x is null) return null;`.
- **Some steps are asynchronous.** The chain keeps its shape when a step returns a `Task` or a `ValueTask`.
- **`null` is a legitimate value in your domain** or an unreliable signal (a generic method that must tell "found nothing" from "found
  `null`" is the classic case); a `Maybe<T>` with `where T : notnull` never holds `null`.
- **You want the absence to be part of the signature in generic code**, in the same way for `string`, `int` and your own types.
- **You also use `Result`**, and want "not found" to become an error with one call (`ToResult`) in the same chain.

## Why use a library when writing your own Maybe is easy?

It is easy, and for a small code base it may be the right call. This is a complete, working `Maybe<T>` in about twenty lines:

```csharp
public readonly struct HandWrittenMaybe<T>
{
    private readonly T? _value;
    private readonly bool _hasValue;

    private HandWrittenMaybe(T value) { _value = value; _hasValue = true; }

    public static HandWrittenMaybe<T> Some(T value) => new(value);
    public static HandWrittenMaybe<T> None => default;

    public bool IsSome => _hasValue;

    public HandWrittenMaybe<TResult> Map<TResult>(Func<T, TResult> map) =>
        _hasValue ? HandWrittenMaybe<TResult>.Some(map(_value!)) : default;

    public HandWrittenMaybe<TResult> Bind<TResult>(Func<T, HandWrittenMaybe<TResult>> bind) =>
        _hasValue ? bind(_value!) : default;

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) =>
        _hasValue ? some(_value!) : none();
}
```

Note what this already gets right: it is a struct, so it allocates nothing, and with a flag the `default` value is a perfectly good "no
value". So the library's advantage is **not** the core idea. It is everything around the core. Each point below was checked against the source
and, where it says so, is pinned by a test:

- **`Some` rejects `null`, and `T : notnull` keeps one way to be empty.** The sketch above accepts `Some(null)` and `HandWrittenMaybe<string?>`,
  so it has a "some that holds null", and every reader has to ask which empty they got. Here `Maybe.Some(null)` throws
  `ArgumentNullException`, `Maybe.From(x)` turns a `null` into none, and `Maybe<string?>` is reported against the `notnull` constraint
  (CS8714; `ConsumerNullabilityTests`).
- **The conversions are decided and pinned.** A `T` (and a `null` reference) converts to a `Maybe<T>` implicitly, so `return user;` works,
  and nothing converts out of a maybe; `ImplicitConversionTests` pins that this is the only conversion.
- **The whole railway, in every sync / `Task` / `ValueTask` combination.** `Map`, `Bind`, `Filter`, `Tap`, `TapNone`, `Or`, `ValueOr`,
  `Match` and `Switch` exist for a maybe, a `Task<Maybe<T>>` and a `ValueTask<Maybe<T>>` receiver, times a synchronous, `Task` or
  `ValueTask` delegate: eleven operations times nine variants, plus the three of `Contains`, is **111 methods** (with the nullable-value form of `Map`), generated from one catalog (111 files under
  `src/Maybe/Generated`, plus 12 more for `Zip`), each variant with a generated test. Writing and testing those by hand is the part that is
  not twenty lines.
- **No `await` in the middle of a chain.** Because the variants share one name, `FindAsync(...).Filter(...).Bind(...).Map(...)` reads
  top to bottom, with the first `await` where you read the result. An already completed `Task`/`ValueTask` receiver is read without a
  state machine ([Async](async.md)).
- **Collection and dictionary lookups.** `FirstOrNone`, `LastOrNone`, `SingleOrNone` (none when there are none *or several*),
  `ElementAtOrNone`, `GetValueOrNone` for read-only dictionaries, and `Choose` (two overloads) ([Collections](collections.md)).
- **LINQ query syntax, `Zip` and `Flatten`.** `Select`, `SelectMany` and `Where`; `Zip` for two to five maybes, as a tuple or with a
  selector; `Flatten` for a `Maybe<Maybe<T>>`, also on `Task` and `ValueTask` receivers ([The railway](the-railway.md)).
- **An analyzer and honest nullability.** `GetValue()` is a method, and the analyzer `WYZ0201` reports a call that is not guarded by a check
  ([Safety and analyzers](safety-and-analyzers.md)). `TryGetValue` is annotated `[MaybeNullWhen(false)]`, a `Map` delegate may return a
  nullable reference without a warning, and the annotations are pinned by tests that compile consumer code.
- **Configuration.** How a maybe prints, the message of `GetValue()` on none, whether the value may appear in logs, and the
  `ConfigureAwait` behavior are settings with defaults ([Configuration](configuration.md)).
- **Equality, ordering and formatting done once.** Two somes are equal when their values are, every none equals every other none and
  `default`, none sorts before every some; there are `==`, `!=`, `<`, `<=`, `>`, `>=`, `IEquatable<Maybe<T>>` and `IComparable<Maybe<T>>`,
  and `ToString()` never throws and can redact the value so that it does not reach logs.
- **Measured size and allocation, and only those.** `Maybe<int>` is 8 bytes and `Maybe<string>` is two references wide; a synchronous
  chain allocates 0 bytes on success and on none; a `Task` chain over a reference type that stays none allocates 0 bytes too, because the
  fast paths hand out one cached completed task per type. These are tests (`PerformanceTests`), and [Performance](performance.md) lists what
  is *not* claimed, which includes every timing.
- **Interop with `Result`.** With the umbrella package `Wyzely.Functional`, `ToResult(error)` and `ToMaybe()` move between the two inside
  one chain, for all three receiver kinds ([Interop](../functional/interop.md)).
- **C# 15 union support on `net11.0`.** A `switch` with a `T` arm and a `null` arm is exhaustive ([C# 15 unions](unions.md)).

### What a dependency costs

- **You adopt a vocabulary and a release cadence.** Your team has to learn `Bind`, `Match` and `Or`, and you follow our releases and our
  breaking changes.
- **There is generated code you did not write.** A stack trace through `Map.Task.ValueTask` is longer than one through your own twenty
  lines. The generated files are in the repository and readable, but there are 123 of them.
- **An extra package in every project that uses it.** The library has no dependencies outside the BCL (except
  `System.Threading.Tasks.Extensions` on `netstandard2.0`); it still is one more thing to restore, scan and update.
- **It is opinionated** (`where T : notnull`, a guarded `GetValue()`, a `null` that becomes none); if you disagree, your own type is
  yours to shape ([Design decisions](design-decisions.md)).

### When writing your own is the better choice

- **You need it in one place**, with a handful of call sites and synchronous steps. The sketch above, plus a `TryGetValue`, is enough.
- **You need behavior this library declined** (arithmetic on options, folds, `Sequence`/`Traverse`, JSON or EF Core integration): see
  [Scope](scope.md); write it, or use a larger library.
- **You cannot take a dependency**, for example in a code base that vendors everything. The code is MIT-licensed, but copying 102 generated
  files is a worse deal than writing the part you need.
- **You are happy with `T?`.** If your chains are short and your team knows `?.` and `??`, there is no gap to fill.

## The short version

We wanted one specific combination of properties:

| What we wanted | The problem behind it |
|---|---|
| **`default` is a valid "no value"** | A struct always has a default value. For an option type the obvious meaning of `default` is "nothing", so an unassigned field or array slot is safe instead of an error. |
| **No `null` inside** | `where T : notnull` and a `Some` that rejects `null` make "a some that holds null" unrepresentable; there is one way to be empty. |
| **Every sync / `Task` / `ValueTask` combination under one name** | A chain such as `FindAsync(...).Filter(...).Bind(...).Map(...)` should not need an `await` in the middle just because one step is synchronous and the next returns a `ValueTask`. |
| **An analyzer in the same package** | `GetValue()` without a check is the one mistake that throws; it is flagged while you type, with no extra package to find. |
| **Nothing outside the BCL to depend on** | A type this basic should not drag a dependency graph into every project that uses it. |
| **Allocation claims that are measured** | A struct that allocates nothing on a synchronous chain, with the claim checked by a test. |
| **A sibling of `Result`** | The same naming, the same async shape and the same configuration style, and one call to move between the two. |

## What already exists, and where it is better

Be clear-eyed: the established libraries have things this one does not. The facts below come from inspecting the packages, not from experience of using them.

- **CSharpFunctionalExtensions** has a `Maybe<T>` in the same package as its `Result` types, with Task and ValueTask forms for many of its
  operations, conversions to its result types, and years of use in many codebases. If you already use it, its `Maybe` is the natural
  choice and there is no reason to add this one next to it.
- **LanguageExt** has a very large `Option<A>` with a wide surface: folds, applicative and traversal helpers, arithmetic on options, and
  conversions to many of its other types. If you want a full functional-programming toolbox, that is the library for it; it is a different scale
  of commitment, and this package deliberately stays small.
- **Funcky** has an `Option<T>` with the widest set of helpers on sequences (`MaxOrNone`, `AverageOrNone`, `Traverse` and more), ships analyzers
  and is actively maintained. If you want those, or already use Funcky for its other types, its `Option` is the natural choice.
- **Optional** has an `Option<T>` with a compact, readable API and collection helpers whose names we borrowed (`FirstOrNone`,
  `SingleOrNone`, ...). The probe found no `Task`/`ValueTask` extension methods on it. If you want a small option type and your chains are
  synchronous, it is a good choice.

## What you give up with this library

- **Where it comes from, and what that does not guarantee.** Like the other packages of this repository it is built on code that has been in
  production use in many codebases, then expanded, improved and documented with Claude Code (see the
  [main README](../../README.md#where-this-code-comes-from)). It has not been run in production *as this package*, so treat
  the lineage as background, not as a guarantee. What backs the claims is the tests.
- **It does less.** No `Apply`, `Sequence`, `Traverse` or arithmetic, no folds, no JSON, EF Core or ASP.NET Core integrations
  ([Scope](scope.md)).
- **It is one team's opinion.** `where T : notnull`, `GetValue()` as a guarded method, and a `null` that becomes none in `Map` and in the
  implicit conversion are choices you may disagree with ([Design decisions](design-decisions.md)).
- **Some paths are lighter on tests than others.** The `netstandard2.0` build is compile-checked, but the test suite runs on `net8.0`,
  `net10.0` and `net11.0`.
- **There is no community yet**: no Stack Overflow answers, no ecosystem of add-ons.

## Should you use it?

| If you... | Then |
|---|---|
| are happy with nullable reference types and `int?` | **Stay.** A library type is not needed for its own sake. |
| already use the `Maybe` of CSharpFunctionalExtensions, or `Option` of LanguageExt or Optional, and are happy | **Stay.** There is no reason to switch for its own sake. |
| need arithmetic or folds on options, `Sequence`/`Traverse`, or a large functional toolbox | Use LanguageExt. |
| need a `Maybe` in one corner of one project | Consider writing the twenty lines above. |
| are starting a new codebase, chain several fallible lookups (some of them asynchronous) and want a sibling of `Result` | **Try it.** It is small enough to evaluate in an afternoon; [the samples](../../samples/Maybe.Samples/Program.cs) are the fastest tour. |
| want to understand a design with these trade-offs on purpose | Read [Design decisions](design-decisions.md); every one is written down with its reason. |

## How we try to keep this honest

- Claims about behavior are tests; the only claims about speed are allocation and size checks ([Performance](performance.md)).
- The README examples are compiled and run as tests, and so are the samples (`test/Samples.Tests`).
- What we have *not* verified says so, including in the [comparison](comparison.md).
