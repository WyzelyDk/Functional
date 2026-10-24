# Wyzely.Functional.Result

[![NuGet](https://img.shields.io/nuget/v/Wyzely.Functional.Result)](https://www.nuget.org/packages/Wyzely.Functional.Result)
[![Downloads](https://img.shields.io/nuget/dt/Wyzely.Functional.Result)](https://www.nuget.org/packages/Wyzely.Functional.Result)

**A `Result` type for .NET that makes expected failures explicit, costs nothing on the success path, and is hard to misuse.**

```csharp
using Wyzely.Functional;

// Task<Result<Order>>        FindOrderAsync(int id)       a Task step
// ValueTask<Result<Receipt>> ChargeAsync(Order order)     a ValueTask step

var outcome = await FindOrderAsync(8)
    .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
    .Bind(ChargeAsync)
    .Map(receipt => receipt.Number)
    .MapError(error => error.WithContext("Checkout"));

outcome.IsFailure;                  // true
outcome.GetError().ToString();      // "Checkout: The order is closed."
```

One chain, no `await` in the middle, no exception thrown, and the failure carries what it needs: a message, a kind, and where it came from.

> **Lineage:** built on code that has been in production use in many codebases, then expanded, improved and documented with Claude Code.
> [What that does and does not mean](https://github.com/WyzelyDk/Functional#where-this-code-comes-from).

## Fluent from first step to last

The whole railway is available as chained calls: `Map`, `Bind`, `Tap`, `TapError`, `MapError`, `Ensure`, `Recover` and
`Compensate` continue the chain; `Match` and `Switch` leave it. Each accepts a synchronous, `Task` or `ValueTask` receiver and delegate in
any mix, so the chain above needs no `await` in the middle, and LINQ query syntax follows the same rules. `Zip` (2 to 5 independent results) and `Flatten` (`Result<Result<T>>` to `Result<T>`) are there too.
[The full list](https://github.com/WyzelyDk/Functional/blob/main/docs/result/the-railway.md).

## Why this exists

Exceptions are for bugs and the unexpected. A missing order, an invalid age or a conflicting update is *expected*: it is an ordinary outcome
of the operation, and it belongs in the signature as a value. That is what a `Result` is.

There are good Result libraries already (CSharpFunctionalExtensions, FluentResults, ErrorOr and others). This one exists because we wanted
a specific combination that we could not find in one package:

- **Two states, no hidden third one.** A struct always has a default value, and here it is a success: `default(Result)` is `Result.Success()`, so
  nothing has to check for, or throw about, a result that was never assigned.
- **One error that can hold many failures**, with a message template, details, kind, code, cause and context, and still a result that is
  one pointer wide.
- **Every sync / `Task` / `ValueTask` combination under the same method name**, so chains never need an `await` in the middle.
- **Analyzers and code fixes in the package**, for the mistakes that matter.
- **No dependencies** outside the BCL, and **performance that is measured**: nothing allocated on the success path.

It also gives something up: it has not been published to NuGet yet and has no track record *as a package* (the code it grew from has production history; see the lineage note above), it does not include a `Maybe` (that is the separate package [`Wyzely.Functional.Maybe`](https://github.com/WyzelyDk/Functional/blob/main/src/Maybe/README.md)), and it is opinionated. If you already use one of the libraries above and are
happy, there is little reason to switch. [The honest long version, including when to use something else.](https://github.com/WyzelyDk/Functional/blob/main/docs/result/why.md)

## Why use a library when writing your own Result is easy?

It is easy. This is a perfectly good Result for a small application, about twenty lines:

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

If that covers your needs, **keep it**: no dependency, no new concept for the team, nothing to upgrade. The question is what happens when the
needs grow. What this package adds, each point checked against the source or a test:

- **An `Error` instead of a string.** A message template plus structured details, a `Kind` (an open set), an optional `Code`, a
  sensitive-detail flag with redacted output (`ToRedactedString()`), context frames (`WithContext`), aggregation of several errors into one
  (`Error.Aggregate`, used by `Combine` and `Zip`), an `IsTransient` flag, and `Error.FromException`, which keeps the exception as the cause.
  A string loses all of that.
- **The whole railway, in every async shape.** `Map`, `Bind`, `Tap`, `TapError`, `MapError`, `Ensure` (with an `Error`, with a message, with an
  error or a message created from the value, with an operation that returns a `Result`, and for an untyped result with a condition), `Recover`,
  `Compensate`, `Match` and `Switch`, plus the conditional `MapIf`, `BindIf`, `TapIf`, `EnsureIf` and `TapErrorIf` and the exception-catching `MapTry`, `BindTry`, `TapTry` and
  `TapIfTry`, are 105 operations in `src/Result/Generated` (counting the `Result` and `Result<T>` forms, the ten `Ensure` forms, the 25 `*If` forms
  the 26 `*Try` forms and the conveniences `EnsureNot`, `EnsureNotNull` and `Tap` without a value, the tuple steps `BindZip` and `Map`/`Bind` with separate parameters, and `GetValueOrThrow` and `ThrowIfFailure` separately), each in the nine combinations of a plain, `Task` or `ValueTask` receiver and delegate (27 for `Ensure` with a
  factory, where the condition and the factory each choose their own kind): 919 generated methods,
  covered by generated tests. Writing those by hand, consistently, is the part nobody enjoys.
- **Analyzers and code fixes in the package:** `WYZ0101` (a discarded result, with a code fix) and `WYZ0102`
  (`GetError()` / `GetValue()` not guarded), and eight suggestions (`WYZ0103` to `WYZ0110`, info severity, each with a code fix) that point at `BindIf`,
  `MapIf`, `EnsureNot`, `EnsureNotNull`, `BindZip`, `ConvertFailure`, `SuccessIf`/`FailureIf` and `MapTry`. For example:

  ```csharp
  Save(order);                       // WYZ0101: a failure would go unnoticed.  Code fix: _ = Save(order);
  var error = result.GetError();     // WYZ0102: not guarded.  Check IsFailure first, or use TryGetError / Match
  ```

- **Nullability as part of the API:** `TryGetValue` is `[MaybeNullWhen(false)]` and `TryGetError` is `[NotNullWhen(true)]`.
- **Combining:** `Combine`, `Partition`, `Zip` (2 to 5 values, every failure reported), `Flatten`, LINQ query syntax, and the bounded-parallelism
  `SelectInParallel` / `ForEachInParallel`, which never expose an unfilled slot.
- **Configuration:** nine settings with defaults (separators, messages, which exceptions `Try` catches, ...) instead of constants buried in your own type.
- **Size and allocation, only what a test checks:** an untyped `Result` is one reference wide, a `Result<T>` is `T` plus one reference, and the
  success path of `Map`, `Bind`, `Ensure`, `Tap`, `MapError` and `Match` allocates 0 bytes with `static` lambdas (`PerformanceTests`). Your
  own sketch can reach similar numbers; this is not a claim that it cannot.
- **C# 15 unions:** on a `net11.0` build `Result<T>` is a union of `T` and `Error`, for an exhaustive `switch`.
- **Interop with Maybe:** the umbrella package `Wyzely.Functional` adds `ToResult` and `ToMaybe` between the two types.

**The cost of the dependency, honestly.** A package to keep updated, an API you do not control (it can change before 1.0), a vocabulary the team has
to learn, and an opinion (nothing converts implicitly, `default` throws) that you may not share. **Write your own when** the application is small, has
few layers, never aggregates errors, and never mixes `Task` and `ValueTask`. **Use a library when** several teams or services share the type, when errors
must be logged safely and grouped, or when you would otherwise hand-write the async variants.

## Samples

[`samples/Result.Samples`](https://github.com/WyzelyDk/Functional/tree/main/samples/Result.Samples) is a runnable project with one heavily commented file per
theme: basics, errors, the railway, async, combining, exceptions, parallel, configuration, untyped results and C# 15 unions. Run it with
`dotnet run --project samples/Result.Samples`. The examples are run as tests, so they cannot silently go stale.

## Install

```shell
dotnet add package Wyzely.Functional.Result
```

| Target | Package dependencies |
|---|---|
| `net10.0`, `net11.0` | none |
| `net8.0` (also used by .NET 9) | none |
| `netstandard2.0` (.NET Framework 4.6.1+, Unity, ...) | `System.Threading.Tasks.Extensions`, only because `ValueTask` is not part of `netstandard2.0` |

The analyzers and code fixes come with the package. Use **C# 13 or later** to write `async` lambdas without ambiguity
([why](https://github.com/WyzelyDk/Functional/blob/main/docs/result/async.md#async-lambdas-and-the-language-version)). All public types are in the namespace
`Wyzely.Functional`.

## A five-minute tour

### Create a result and read it

```csharp
using System.Globalization;
using Wyzely.Functional;

static Result<int> ParseAge(string text) =>
    int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age)
        ? Result.Success(age)
        : Result.Failure<int>(Error.Validation("'{Text}' is not a number.").WithDetail("Text", text));
```

Results are created with `Result.Success(...)` and `Result.Failure(...)`; nothing converts to a result implicitly, so `return false;` in a
method that returns `Result<bool>` can never be mistaken for a failure.

### Chain steps; the failure skips the rest

```csharp
var message = ParseAge("42")
    .Ensure(age => age >= 18, Error.Validation("Must be an adult."))
    .Map(age => $"Welcome, age {age}.")
    .Match(text => text, error => $"Rejected: {error}");
// "Welcome, age 42."      (for "abc": "Rejected: 'abc' is not a number.")
```

`Map`, `Bind`, `Ensure`, `Tap`, `MapError`, `Recover`, `Match` and friends: [the railway](https://github.com/WyzelyDk/Functional/blob/main/docs/result/the-railway.md).

### Mix synchronous and asynchronous steps freely

The receiver can be a result, a `Task<Result<T>>` or a `ValueTask<Result<T>>`, and each delegate can be synchronous, return a `Task`, or return
a `ValueTask`. All nine combinations exist under the same name, which is why the example at the top has no `await` between the steps.
[Async in detail](https://github.com/WyzelyDk/Functional/blob/main/docs/result/async.md).

### Describe the failure properly

```csharp
var error = Error.NotFound("Order {OrderId} of {Customer} was not found.")
    .WithDetail("OrderId", 7)
    .WithDetail("Customer", "anna@example.com", isSensitive: true)
    .WithCode("ORDER_NOT_FOUND")
    .WithContext("Checkout");

error.ToString();          // "Checkout: Order 7 of anna@example.com was not found."
error.ToRedactedString();  // "Checkout: Order 7 of *** was not found."      <- safe to log
error.Template;            // "Order {OrderId} of {Customer} was not found."  <- stable key for grouping
```

[Errors in depth](https://github.com/WyzelyDk/Functional/blob/main/docs/result/errors.md): templates, details, kinds, aggregation, redaction.

### Handle many results at once

```csharp
Result[] checks = [Result.Success(), Result.Failure("Name is missing."), Result.Failure("Email is invalid.")];

var combined = Result.Combine(checks);
combined.GetError().Message;   // "Name is missing., Email is invalid."   (one failure holding both)
```

`Combine`, `Partition` and bounded-parallelism helpers always look at *every* item: [many results](https://github.com/WyzelyDk/Functional/blob/main/docs/result/many-results.md).

### Bring exceptions in at the boundary

```csharp
var result = Result.Try(() => int.Parse("not a number", CultureInfo.InvariantCulture));
result.IsFailure;                       // true
result.GetError().Exception;            // the FormatException, kept as the cause
```

### Let the compiler catch the mistakes

```csharp
Run();                                  // WYZ0101: Run() returns a Result that is ignored   (code fix: _ = Run();)
result.GetError();                      // WYZ0102: not guarded by an IsFailure check
```

[Safety and analyzers](https://github.com/WyzelyDk/Functional/blob/main/docs/result/safety-and-analyzers.md).

## Everything else

| Topic | |
|---|---|
| [Getting started](https://github.com/WyzelyDk/Functional/blob/main/docs/result/getting-started.md) | install, core types, the full list of ways to create and read a result |
| [The railway](https://github.com/WyzelyDk/Functional/blob/main/docs/result/the-railway.md) | every operation and what it does |
| [Async](https://github.com/WyzelyDk/Functional/blob/main/docs/result/async.md) | the nine variants and `async` lambdas |
| [Errors](https://github.com/WyzelyDk/Functional/blob/main/docs/result/errors.md) | the `Error` type in depth |
| [Many results](https://github.com/WyzelyDk/Functional/blob/main/docs/result/many-results.md) | `Combine`, `Partition`, parallel helpers |
| [Safety and analyzers](https://github.com/WyzelyDk/Functional/blob/main/docs/result/safety-and-analyzers.md) | `default`, the two rules, the code fix |
| [Configuration](https://github.com/WyzelyDk/Functional/blob/main/docs/result/configuration.md) | what you can change, and the defaults |
| [Performance](https://github.com/WyzelyDk/Functional/blob/main/docs/result/performance.md) | what is guaranteed and what was measured, with the caveats |
| [How it compares](https://github.com/WyzelyDk/Functional/blob/main/docs/result/comparison.md) | a factual comparison with eight other libraries: type design, behavior of `default` and `null`, API surface, activity |
| [C# 15 unions](https://github.com/WyzelyDk/Functional/blob/main/docs/result/unions.md) | exhaustive `switch` on `net11.0` |
| [Design decisions](https://github.com/WyzelyDk/Functional/blob/main/docs/result/design-decisions.md) | why it is the way it is, and what we changed after measuring |
| [Scope](https://github.com/WyzelyDk/Functional/blob/main/docs/result/scope.md) | what this package will and will not take |

## Performance, in short

A `Result` is exactly one pointer wide, and the success path allocates nothing (checked by tests). Benchmarked against eight other libraries, for value and reference types, a four-step
synchronous chain took about 5 ns without allocating, as it did in CSharpFunctionalExtensions, ErrorOr and OneOf; the others took 2 to 29 times as long and
most allocated. Chaining on a completed `Task` allocates in every library, ours included, and awaiting first is cheaper. The differences between
the four fastest are inside the run-to-run noise, so we do not rank them. [Numbers and caveats](https://github.com/WyzelyDk/Functional/blob/main/docs/result/performance.md).

## Contributing

Bug reports and ideas are welcome. Please read the [scope](https://github.com/WyzelyDk/Functional/blob/main/docs/result/scope.md) first; it says what will not be
added and why. [Building, testing and how the generated code works](https://github.com/WyzelyDk/Functional/blob/main/docs/result/contributing.md).

## License

[MIT](https://github.com/WyzelyDk/Functional/blob/main/LICENSE)
