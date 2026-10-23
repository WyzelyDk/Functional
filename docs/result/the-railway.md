# The railway: Map, Bind, Match and friends

[Back to the Result overview](../../src/Result/README.md)

The operations are extension methods. Each exists for `Result<T>` and, where it makes sense, for `Result`.

| Method | Runs the delegate when | Does |
|---|---|---|
| `Map(f)` | success | transforms the value |
| `Bind(f)` | success | chains an operation that returns a `Result`/`Result<U>` |
| `Tap(f)` | success | runs a side effect, returns the result unchanged |
| `TapError(f)` | failure | runs a side effect with the error |
| `MapError(f)` | failure | replaces the error, for example to add context |
| `Ensure(predicate, error)` | success | turns the success into a failure when the predicate is false; the error can also be a plain message, or a function of the value that creates the error (or the message) only when the predicate is false; on an untyped result the condition takes no value |
| `Ensure(f)` | success | validates with an operation that returns a `Result` (or a typed result, whose value is ignored), keeping the original value; the operation may also take no parameter |
| `Recover(f)` | failure | computes a replacement value, turning the failure into a success |
| `Compensate(f)` | failure | tries a fallback operation that returns a `Result` |
| `Match(onSuccess, onFailure)` | always | leaves the railway: one value out |
| `Switch(onSuccess, onFailure)` | always | leaves the railway for side effects |

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
