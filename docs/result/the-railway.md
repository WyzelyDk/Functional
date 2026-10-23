# The railway: Map, Bind, Match and friends

[Back to the Result overview](../../src/Result/README.md)

The operations are extension methods. Each exists for `Result<T>` and, where it makes sense, for `Result`.

| Method | Runs the delegate when | Does |
|---|---|---|
| `Map(f)` | success | transforms the value |
| `Bind(f)` | success | chains an operation that returns a `Result`/`Result<U>` |
| `Tap(f)` | success | runs a side effect, returns the result unchanged; the side effect may also take no value |
| `TapError(f)` | failure | runs a side effect with the error |
| `MapError(f)` | failure | replaces the error, for example to add context |
| `Ensure(predicate, error)` | success | turns the success into a failure when the predicate is false; the error can also be a plain message, or a function of the value that creates the error (or the message) only when the predicate is false; on an untyped result the condition takes no value |
| `EnsureNot(predicate, error)` | success | the mirror of `Ensure`: turns the success into a failure when the predicate is true |
| `EnsureNotNull(error)` | success | fails when the value is `null`, and turns a `Result<T?>` into a `Result<T>` (for a reference type and for a nullable value type) |
| `Ensure(f)` | success | validates with an operation that returns a `Result` (or a typed result, whose value is ignored), keeping the original value; the operation may also take no parameter |
| `MapIf(condition, f)`, `BindIf`, `TapIf`, `EnsureIf` | success | the same step, run only when the condition holds; the condition is a `bool`, a function, or a function of the value, and `MapIf` and `BindIf` keep the type of the value |
| `TapErrorIf(condition, f)` | failure | `TapError`, run only when the condition holds; the condition can be a function of the error |
| `MapTry(f)`, `BindTry`, `TapTry`, `TapIfTry` | success | the same step for a delegate that may throw: the exception becomes a failure, like `Result.Try`; a handler can create the error, and a handler that names an exception type catches only that type |
| `BindZip(f)` | success | chains an operation and keeps the value next to its value, in a tuple; on a tuple result it adds the value to the tuple, up to five |
| `Map(f)`, `Bind(f)` on a tuple | success | the delegate takes the values of a tuple result (for example of `Zip` or `BindZip`) as separate parameters, up to five |
| `Recover(f)` | failure | computes a replacement value, turning the failure into a success |
| `Compensate(f)` | failure | tries a fallback operation that returns a `Result` |
| `GetValueOrThrow(f)`, `ThrowIfFailure(f)` | always | leaves the railway with an exception of your own type, created from the error; without `f` on a `Task` or `ValueTask` it throws a `ResultFailedException`, as `GetValue()` does |
| `Match(onSuccess, onFailure)` | always | leaves the railway: one value out |
| `Switch(onSuccess, onFailure)` | always | leaves the railway for side effects |

A step that is only sometimes needed does not have to leave the chain. The condition is a `bool` you already have, or a function that is
evaluated only when the result succeeded (a function of the value gets it). When it does not hold, the result passes through as it was:

```csharp
var price = Result.Success(100)
    .MapIf(isMember, static value => value * 9 / 10)
    .BindIf(() => couponIsValid, static value => Result.Success(value - 5))
    .EnsureIf(static value => value > 50, static value => Result.SuccessIf(value < 1000, "Needs approval."));
```

A delegate that may throw does not need a `try`/`catch` in the chain. `MapTry`, `BindTry`, `TapTry` and `TapIfTry` turn the exception into a
failure, as `Result.Try` does, so a cancellation keeps travelling unless `ShouldCatch` says otherwise. Name the exception type with the parameter of the
handler to catch only that type; any other exception keeps travelling:

```csharp
var address = Result.Success("not a web address")
    .MapTry(static text => new Uri(text), static (UriFormatException exception) => Error.Validation("The address is not valid."));
```

Where a failure has to become an exception, say which one. `GetValueOrThrow` and `ThrowIfFailure` take a function that creates the exception from the
error, and the form without it works on a `Task` or a `ValueTask`, so the end of a chain needs no `await` in the middle. Unlike `GetValue()` they are
the deliberate way out, so the analyzer does not ask for a check first:

```csharp
var id = Result.Success(7)
    .GetValueOrThrow(static error => new KeyNotFoundException(error.Message));      // 7, or the exception of your own type

var code = await Task.FromResult(Result.Success("A12"))
    .Map(static text => text.ToLowerInvariant())
    .GetValueOrThrow();                                                             // "a12", or a ResultFailedException
```

A value that may be `null` becomes a value that is not, and the type says so. `EnsureNotNull` turns a `Result<T?>` into a `Result<T>`, for a reference
type and for a nullable value type, and fails with your error when the value is `null`:

```csharp
var name = Result.Success<string?>(null)
    .EnsureNotNull(Error.NotFound("No such customer."));    // a Result<string>, no longer a Result<string?>
```

A step often needs a value from an earlier one. `BindZip` keeps the value next to the value of the next step, and `Map` and `Bind` take the values of
such a tuple as separate parameters, so no `Item1` or lambda that closes over a variable is needed. `Zip` returns the same kind of tuple. A tuple result that is
given a `BindZip` grows by one value, up to five:

```csharp
var line = Result.Success(3)
    .BindZip(static quantity => Result.Success(quantity * 5))
    .Map(static (quantity, price) => $"{quantity} for {price}");
```

LINQ query syntax and `Zip` are supported as well:

```csharp
var total = from price in Result.Success(40)
            from tax in Result.Success(2)
            select price + tax;                          // Result<int> with 42; stops at the first failure

var pair = Result.Success(1).Zip(Result.Failure<string>("x"));   // reports the errors of both when both failed

var triple = Result.Success(1).Zip(Result.Success("a"), Result.Success(true));   // Result<(int First, string Second, bool Third)>

var flat = Result.Success(Result.Success(5)).Flatten();   // Result<int>: Result<Result<T>> becomes Result<T>
```


`Zip` combines 2 to 5 independent results (tuple elements are named `First` to `Fifth`, or pass a selector), reports every failure (the single error, or an aggregate in argument order), and is generated into `ResultExtensions`. `Flatten` exists for plain, `Task` and `ValueTask` receivers. Both are used in the [samples](../../samples/Result.Samples).
