# Getting started

[Back to the Result overview](../../src/Result/README.md)

## Install

```shell
dotnet add package Wyzely.Functional.Result
```

| Target | Package dependencies |
|---|---|
| `net10.0` | none |
| `net8.0` (also used by .NET 9) | none |
| `netstandard2.0` (.NET Framework 4.6.1+, Unity, Xamarin, ...) | `System.Threading.Tasks.Extensions`, only because `ValueTask` is not part of `netstandard2.0` |

The analyzers are part of the package (`analyzers/dotnet/cs`); there is nothing else to install. For the best experience with
`async` lambdas use **C# 13 or later**; see [Async lambdas and the language version](async.md#async-lambdas-and-the-language-version).

## Quick start

```csharp
using Wyzely.Functional;

static Result<int> ParseAge(string text) =>
    int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age)
        ? Result.Success(age)
        : Result.Failure<int>(Error.Validation("'{Text}' is not a number.").WithDetail("Text", text));

var message = ParseAge("42")
    .Ensure(age => age >= 18, Error.Validation("Must be an adult."))
    .Map(age => $"Welcome, age {age}.")
    .Match(text => text, error => $"Rejected: {error}");
// "Welcome, age 42."   (and for "abc": "Rejected: 'abc' is not a number.")
```

## Core types

### `Result` and `Result<T>`

Both are `readonly struct`s. A result is in exactly one of two states:

| State | How you get it | What you can read |
|---|---|---|
| **Success** | `Result.Success()`, `Result.Success(value)`, and a `default` result: `default(Result)`, `new Result()`, an array element or field that was never assigned | `GetValue()`; `GetError()` throws |
| **Failure** | `Result.Failure(error)`, `Result.Failure<T>("message")`, `Result.FromException(ex)`, `Result.Try(...)` | `GetError()`; `GetValue()` throws |

A `default` result is a success: `default(Result)` is the same as `Result.Success()`, and `default(Result<T>)` is the same as
`Result.Success(default(T))`. A field or an array slot that nobody assigned therefore counts as "it worked", which is why results are collected
with `Combine`, `Partition` or `SelectInParallel` and not in an array that you fill yourself.

| Create | |
|---|---|
| `Result.Success()` / `Result.Success(value)` | success, with or without a value (the value may be `null` for a nullable type) |
| `Result.Failure(error)` / `Result.Failure("message")` / `Result.Failure<T>(...)` | failure |
| `Result.FromException(exception)` | failure that keeps the exception as the cause |
| `Result.SuccessIf(condition, error)` / `Result.FailureIf(condition, error)` | conditional; the condition can be a `bool`, a function, a `Task<bool>` or a `ValueTask<bool>` (or a function that returns one), and the error can also be given as a plain message or as a function that creates the error or the message only when the result fails |
| `Result.Try(...)` | run code that may throw (see below) |

Nothing converts to a result **implicitly**. `return false;` in a method that returns `Result<bool>` would otherwise compile as a
*success* holding `false`, which reads like a failure; with explicit factories a reader sees what happens. (The one implicit
conversion is the lossless `Result<T>` to `Result`, which only drops the value.)

| Read | |
|---|---|
| `IsSuccess`, `IsFailure` | the state |
| `GetValue()`, `GetError()` | the payload; they throw if the state does not match (`ResultFailedException` carries the `Error`) |
| `TryGetValue(out v)`, `TryGetError(out e)` | the non-throwing way |
| `ValueOr(fallback)`, `var (value, error) = result;` | convenience |
| `ThrowIfFailure()` | leave the railway as an exception |
| `ToResult()` / implicit conversion | drop the value of a `Result<T>` |

### Exceptions at the boundary: `Try`

```csharp
var result = Result.Try(() => int.Parse(text, CultureInfo.InvariantCulture));   // Result<int>
var saved  = await Result.Try(() => file.WriteAsync(bytes));                    // Task<Result>
```

`Try` turns an exception into a failure (`Error.FromException`, or your own mapping as the second argument) and keeps the exception
as the cause. By default an `OperationCanceledException` is **not** turned into a result: cancellation is a request from the caller
and keeps travelling as an exception. Both the default mapping and which exceptions are caught can be changed, see
[Configuration](configuration.md).

To catch **one kind of exception** and let every other kind keep travelling, name its type with the parameter of the handler, and narrow it
further with `when` if you need to. Both the type of the value and the type of the exception are inferred:

```csharp
var order = Result.Try(
    () => JsonSerializer.Deserialize<Order>(json),
    (JsonException exception) => Error.FromException(exception, "The order is not valid JSON."));

var response = await Result.Try(
    () => client.GetAsync(url, cancellationToken),
    (OperationCanceledException exception) => Error.Timeout("The request timed out.").WithException(exception),
    when: _ => !cancellationToken.IsCancellationRequested);   // a timeout, not a cancellation by the caller
```

Naming the type is the decision, so `ShouldCatch` does not apply to this form, which is also how a cancellation is caught on purpose. A
handler that takes a plain `Exception` is the `Try` above, which does follow the setting.

## Samples

[`samples/Result.Samples`](../../samples/Result.Samples) is a runnable project with one heavily commented file per theme: basics, errors, the railway, async, combining, exceptions, parallel, configuration, untyped results and C# 15 unions. Run it with `dotnet run --project samples/Result.Samples`. The examples are run as tests.
