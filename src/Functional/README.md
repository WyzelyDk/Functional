# Wyzely.Functional

[![NuGet](https://img.shields.io/nuget/v/Wyzely.Functional)](https://www.nuget.org/packages/Wyzely.Functional)
[![Downloads](https://img.shields.io/nuget/dt/Wyzely.Functional)](https://www.nuget.org/packages/Wyzely.Functional)

**`Result`, `Maybe` and the methods that connect them, in one package: "not found" becomes an error with `ToResult`,
and a failure becomes "no value" with `ToMaybe`.**

```csharp
using Wyzely.Functional;

// Task<Maybe<Customer>>      FindCustomerAsync(int id)          may find nothing
// ValueTask<Result<Receipt>> ChargeAsync(Customer customer)     may fail

var outcome = await FindCustomerAsync(7)
    .ToResult(Error.NotFound("No such customer."))     // Maybe becomes Result: none becomes this error
    .Bind(ChargeAsync)
    .Map(receipt => receipt.Number);

var text = outcome.Match(number => number, error => error.Message);   // "R-7"   (for an unknown customer: "No such customer.")
```

One chain from "maybe there is a customer" to "the charge went through", with no `await` in the middle.

> **Lineage:** this is not code written from scratch. These libraries are built on code that has already been in production use in many codebases,
> and for this repository that code was expanded, improved and documented with Claude Code.
> The package depends on the two packages and adds only the methods between them.
> [What that does and does not mean](https://github.com/WyzelyDk/Functional#where-this-code-comes-from).

## When to install this instead of the two packages

Install **`Wyzely.Functional`** when you use both `Result` and `Maybe` and move between them (a lookup that may find nothing, then an operation that may fail).
It references [`Wyzely.Functional.Result`](https://github.com/WyzelyDk/Functional/blob/main/src/Result/README.md) and
[`Wyzely.Functional.Maybe`](https://github.com/WyzelyDk/Functional/blob/main/src/Maybe/README.md), so both arrive as dependencies, with the analyzers of both,
and it adds `ToResult`, `ToMaybe`, `EnsureSome` and `ToInvertedResult`.

Install **only one of the two** when you need only that one: the packages are independent, and neither references the other.
`Wyzely.Functional` is the only package that connects them, so taking `Result` never makes you take `Maybe`.

All public types are in the namespace `Wyzely.Functional`, so one `using Wyzely.Functional;` gives you everything.

## Install

```shell
dotnet add package Wyzely.Functional
```

| Target | Package dependencies |
|---|---|
| `net11.0`, `net10.0` | `Wyzely.Functional.Result`, `Wyzely.Functional.Maybe` |

The last versions with a `netstandard2.0` asset (for .NET Framework, Unity, and .NET 8 or 9) are 0.3.x; a project on those stays on 0.3.x.

The analyzers of both packages (`WYZ0101` to `WYZ0120` for `Result`, `WYZ0201` to `WYZ0208` for `Maybe`) come with the dependencies;
there is nothing else to install.
(CI checks it on every build: it packs the three packages,
builds a project that references only `Wyzely.Functional` with the oldest .NET 10 SDK (10.0.100), the newest one and SDK 11,
and requires `WYZ0102` and `WYZ0201` to be reported.)
Use **C# 13 or later** to write `async` lambdas without ambiguity: under `LangVersion` 12 the compiler reports `CS0121` (an ambiguous call) for some of them,
and under 13 it does not (verified by compiling a scratch project with both language versions).

## The interop methods

| From | Method | Gives |
|---|---|---|
| `Maybe<T>`, `Task<Maybe<T>>`, `ValueTask<Maybe<T>>` | `ToResult(Error)` or `ToResult(Func<Error>)` | a success with the value, or a failure with your error |
| `Result<T>`, `Task<Result<T>>`, `ValueTask<Result<T>>` | `ToMaybe()` | a maybe with the value of a success, or none |
| `Result<Maybe<T>>`, `Task<Result<Maybe<T>>>`, `ValueTask<Result<Maybe<T>>>` | `EnsureSome(Error)` or `EnsureSome(Func<Error>)` | a `Result<T>`: a failure passes through, a maybe with a value becomes a success, and none becomes your error |
| `Maybe<T>`, `Task<Maybe<T>>`, `ValueTask<Maybe<T>>` | `ToInvertedResult(Error)` or `ToInvertedResult(Func<T, Error>)` | the meaning turned around, for "this must not exist yet": a failure when there is a value, a success when there is none |

```csharp
using Wyzely.Functional;

var found = Maybe.Some("ada@example.com");
var missing = Maybe.None<string>();

var success = found.ToResult(Error.NotFound("No such customer."));      // success with "ada@example.com"
var failure = missing.ToResult(() => Error.NotFound("No such customer."));   // failure; the Func<Error> only runs for none

var someValue = Result.Success(5).ToMaybe();                  // Some(5)
var noValue = Result.Failure<int>("It broke.").ToMaybe();     // None: the error is dropped, on purpose
```

`EnsureSome` is for a step that returns a result of a maybe, such as a lookup that can fail and can come back empty.
It is to `Result<Maybe<T>>` what `EnsureNotNull` is to `Result<T?>`:

```csharp
var lookup = Result.Success(Maybe.None<string>());

var customer = lookup.EnsureSome(Error.NotFound("No such customer."));     // a Result<string>: failed, with that error
```

Two things are worth knowing before you use `ToMaybe()`: **the error is dropped on purpose**, because the method is for the place where only
"is there a value" matters, and **a success whose value is `null` becomes none**, because a `Maybe` cannot hold `null`. Every overload, the async behavior and
the details are in [Interop](https://github.com/WyzelyDk/Functional/blob/main/docs/functional/interop.md).

## Everything else

| Topic | |
|---|---|
| [Getting started](https://github.com/WyzelyDk/Functional/blob/main/docs/functional/getting-started.md) | a guided tour of the two directions, a full async chain, and what to read next |
| [Why an umbrella package](https://github.com/WyzelyDk/Functional/blob/main/docs/functional/why.md) | what it adds over the two packages, and when you do not need it |
| [Interop](https://github.com/WyzelyDk/Functional/blob/main/docs/functional/interop.md) | every overload of `ToResult` and `ToMaybe`, and what each does |
| [Samples](https://github.com/WyzelyDk/Functional/tree/main/samples/Functional.Samples) | seven runnable examples of the two types together, including an order checkout you can copy |
| [`Result` documentation](https://github.com/WyzelyDk/Functional/blob/main/src/Result/README.md) | the `Result` type, the railway, errors, analyzers |
| [`Maybe` documentation](https://github.com/WyzelyDk/Functional/blob/main/src/Maybe/README.md) | the `Maybe` type, the railway, collections, analyzer |

## Contributing

Bug reports and ideas are welcome. The scope of the interop is the same as that of the packages it connects:
[Result](https://github.com/WyzelyDk/Functional/blob/main/docs/result/scope.md), [Maybe](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/scope.md).
[Building and testing](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/contributing.md).

## License

[MIT](https://github.com/WyzelyDk/Functional/blob/main/LICENSE)
