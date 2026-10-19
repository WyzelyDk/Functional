# Why an umbrella package?

[Back to the Wyzely.Functional overview](../../src/Functional/README.md)

The umbrella package `Wyzely.Functional` contains two methods (`ToResult` and `ToMaybe`, in nine overloads) and depends on the other two packages.
This page says why that is a package of its own, and when you do not need it.

## Why not put the methods in `Result` or `Maybe`?

Because the packages are independent on purpose: taking `Result` should never make you take `Maybe`, and the reverse. If `Result` had
`Maybe<T>.ToResult`, it would have to reference `Maybe`. Something has to sit above both, and what lies between two things belongs in neither.
The umbrella package is that something, and `AGENTS.md` keeps it to that: it holds only what connects the others.

## What you get by installing it

- `ToResult` and `ToMaybe` for plain, `Task` and `ValueTask` receivers, so a chain can cross from one type to the other without an `await`.
- Both packages and both sets of analyzers (`WYZ0101`, `WYZ0102` and `WYZ0201`) as dependencies, with one command.
- A single `using Wyzely.Functional;`, because every package uses that namespace.

## When you do not need it

- You use only `Result`, or only `Maybe`. Install that package.
- You use both but never move between them. Install the two; the result is the same.
- You are happy to write the two conversions yourself. They are short:

```csharp
using Wyzely.Functional;

Result<string> ToResult(Maybe<string> maybe, Error error) =>
    maybe.TryGetValue(out var value) ? Result.Success(value) : Result.Failure<string>(error);
```

What the package adds over that is the nine overloads (so no `await` is needed in the middle of a chain), the null checks that fire when you call the
method rather than when you await, and tests for how it behaves with faulted tasks and with the two `ContinueOnCapturedContext` settings
(`InteropConfigurationTests`).

## What it costs

A dependency on two packages, and the opinions that come with them (see the [main README](../../README.md#the-opinions)). The conversion from a
result to a maybe is lossy by design, and a success whose value is `null` becomes none. Nothing is published on NuGet yet, and the API can change before 1.0.
