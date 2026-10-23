# Why another Result library?

[Back to the Result overview](../../src/Result/README.md)

There are good, widely used Result libraries for .NET already. This page explains why this one exists, what you give up by choosing it,
and when you should simply use something else. The comparisons are based on inspecting the packages, running small probes and benchmarks (see
[How it compares](comparison.md) and [Performance](performance.md)); they describe the versions named there, not the libraries in general.

## The short version

We wanted one specific combination of properties, and we could not find all of them in one package:

| What we wanted | The problem behind it |
|---|---|
| **One error type that can hold many failures, without a list on the result** | Validation and batch work produce several failures. Carrying a list on the result makes it big; a string loses the structure. One immutable `Error` with a message template, details, kind, code, cause, context and children keeps the result one pointer wide and the failure fully described. |
| **Every sync / `Task` / `ValueTask` combination under one name** | A chain such as `FindAsync(...).Ensure(...).Bind(...).Map(...)` should not need an `await` in the middle just because one step is synchronous and the next returns a `ValueTask`. |
| **Analyzers and code fixes in the same package** | The mistakes that matter (a discarded result, reading the error of a success) are caught while you type, with no extra package to find and install. |
| **Nothing outside the BCL to depend on** | A library this basic should not drag a dependency graph into every project that uses it. |
| **Performance claims that are measured** | A struct that allocates nothing on the success path, with the claim checked by a test and the benchmarks published, caveats included. |
| **Defaults you can change, and a surface that stays small** | Separators, messages and the handling of exceptions are settings, not hidden constants; and a [scope](scope.md) document says what will not be added. |

## Why use a library when writing your own Result is easy?

Because the first twenty lines are easy and the next two thousand are not. Here is a complete, honest minimal Result:

```csharp
public readonly record struct SimpleResult<T>(T? Value, string? Error)
{
    public bool IsSuccess => Error is null;

    public static SimpleResult<T> Ok(T value) => new(value, null);

    public static SimpleResult<T> Fail(string error) => new(default, error);

    public SimpleResult<TNext> Map<TNext>(Func<T, TNext> map) =>
        IsSuccess ? SimpleResult<TNext>.Ok(map(Value!)) : SimpleResult<TNext>.Fail(Error!);

    public SimpleResult<TNext> Bind<TNext>(Func<T, SimpleResult<TNext>> bind) =>
        IsSuccess ? bind(Value!) : SimpleResult<TNext>.Fail(Error!);

    public TOut Match<TOut>(Func<T, TOut> ok, Func<string, TOut> fail) => IsSuccess ? ok(Value!) : fail(Error!);
}
```

For a small application, **use that**. It has no dependency and everybody can read it. What this library adds, point by point (each checked against the
source or a test):

| Point | Your sketch | This library |
|---|---|---|
| The error | a string | an `Error`: message template, structured details, an open `ErrorKind`, a `Code`, sensitive details with `ToRedactedString()`, `WithContext` frames, `Error.Aggregate` for many failures, `IsTransient` (set by `Unavailable` and `Timeout`), `Error.FromException` keeping the cause |
| The railway | `Map`, `Bind`, `Match` | `Map`, `Bind`, `Tap`, `TapError`, `MapError`, `Ensure`, `Recover`, `Compensate`, `Match`, `Switch`: 29 generated operations (`Result` and `Result<T>` forms, and all ten `Ensure` forms, counted separately) in the nine plain/`Task`/`ValueTask` combinations (27 for the `Ensure` forms with a factory), 297 generated methods with generated tests |
| Mistakes | none caught | analyzers shipped in the package: `WYZ0101` (discarded result, with a code fix), `WYZ0102` (unguarded `GetError`/`GetValue`) |
| Nullability | `Value` is `T?` and you suppress with `!` | `TryGetValue` is `[MaybeNullWhen(false)]`, `TryGetError` is `[NotNullWhen(true)]`; `ConsumerNullabilityTests` pin the warnings |
| Many results | write it | `Combine`, `Partition`, `Zip` for 2 to 5 values, `Flatten`, LINQ query syntax, `SelectInParallel` / `ForEachInParallel` |
| Settings | constants in your code | `ResultConfiguration` with nine settings |
| Size and allocation | probably the same, if you use a struct | tested: `Result` is one reference wide, `Result<T>` is `T` plus one reference, the success path of the main operations allocates 0 bytes with `static` lambdas (`PerformanceTests`) |
| Language and ecosystem | none | C# 15 union support on `net11.0`; `ToResult` / `ToMaybe` with `Maybe` through the umbrella package `Wyzely.Functional` |

**The honest cost.** A dependency to update; an API that can still change before 1.0; a vocabulary and a set of opinions (no implicit conversion,
`default` throws) that the whole team must accept; and no public track record as a package yet. **Write your own** when the application is small, the
error is a message, nobody mixes `Task` and `ValueTask`, and one team owns the code. **Use a library** when the type is shared, when errors must be
logged safely and grouped, or when you would otherwise hand-write nine variants of every operation.

## What already exists, and where it is better

Be clear-eyed: the established libraries have things this one does not.

- **CSharpFunctionalExtensions** is mature and widely adopted. It has `Maybe`, a very large set of extension methods, separate add-on
  packages for testing, Web API and analyzers, and years of production use. If you rely on any of that, use it.
- **FluentResults** has a rich model of "reasons" and successes attached to results, and a configurable logger. If you want to attach
  many typed reasons to a result and you are fine with classes, it fits.
- **ErrorOr** is small and pleasant, with a struct and a compact error. If you want exactly that and no `ValueTask`, it is a good choice.
- **Ardalis.Result** returns an HTTP-like status from a service (`NotFound`, `Forbidden`, `Conflict` and so on). If that is the model you want, it fits; it is a class,
  and its last package is from October 2024.
- **Funcky** is an active, broad functional library with built-in analyzers. Its `Result<T>` carries an `Exception`, which suits code that
  already works with exceptions.
- **LanguageExt**, **OneOf** and **Optional** are not result libraries in the first place: a complete functional toolbox, a union library and
  an option library. The [comparison](comparison.md) lists what each gives you when you use it as a result.

## What you give up with this library

- **It is a young package.** The code it grew from has been in production use in many codebases, then expanded, improved and documented with Claude Code (the [main README](../../README.md#where-this-code-comes-from) explains what that does and does not mean). It has not been published to NuGet yet, so it has no track record *as a package*, and the API can change before 1.0.
- **It does less.** `Maybe` is a separate package ([`Wyzely.Functional.Maybe`](../maybe/why.md)) and not part of this one. There are no value objects
  and no JSON, EF Core or ASP.NET Core integrations; those are not part of this package either.
- **It is one team's opinion.** Conversions are explicit, `default` throws, and `GetValue()`/`GetError()` are guarded by an analyzer.
  If you prefer `return value;` to compile as a success, this library will annoy you on purpose.
- **Some paths are lighter on tests than others.** The `netstandard2.0` build is compile-checked but the test suite does not run on
  it, and the C# 15 union support targets a runtime that is still a release candidate.
- **There is no community yet**: no Stack Overflow answers, no ecosystem of add-ons.

## Should you use it?

| If you... | Then |
|---|---|
| already use one of the libraries above and are happy | **Stay.** There is no reason to switch for its own sake. |
| need value objects or framework integrations *today* | Use CSharpFunctionalExtensions, which has them. |
| need a `Maybe` next to the `Result` | [`Wyzely.Functional.Maybe`](../maybe/why.md) exists (it has the same lineage, and is equally unpublished and young); CSharpFunctionalExtensions has a `Maybe` with years of use as a package. Choose by that trade-off. |
| are starting a new codebase and the table above describes pains you recognise (a result that can silently be a success, `ValueTask` chains that need awaits, analyzers you have to hunt for) | **Try it.** It is small enough to evaluate in an afternoon. |
| want to understand a design with these trade-offs on purpose | Read [Design decisions](design-decisions.md); every one is written down with its reason. |

## How we try to keep this honest

- Claims about behavior are tests; claims about speed are benchmarks with their limits stated ([Performance](performance.md)).
- The README examples are compiled and run as tests.
- When a measurement turned out wrong we changed the code and the text, not just the text (the history is in
  [Design decisions](design-decisions.md)).
- What we have *not* verified says so, including in the [comparison](comparison.md).
