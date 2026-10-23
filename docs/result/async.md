# Async: Task and ValueTask

[Back to the Result overview](../../src/Result/README.md)

Every operation exists in **nine variants** with the same name. The receiver can be a result, a `Task<Result<T>>` or a
`ValueTask<Result<T>>`, and the delegate can be synchronous, return a `Task`, or return a `ValueTask`:

|  | sync delegate | `Task` delegate | `ValueTask` delegate |
|---|---|---|---|
| **result** | `Result<T>` | `Task<Result<T>>` | `ValueTask<Result<T>>` |
| **`Task<Result<T>>`** | `Task<Result<T>>` | `Task<Result<T>>` | `Task<Result<T>>` |
| **`ValueTask<Result<T>>`** | `ValueTask<Result<T>>` | `ValueTask<Result<T>>` | `ValueTask<Result<T>>` |

The return type follows the receiver; a synchronous receiver takes the kind of the delegate. So a chain can mix synchronous and
asynchronous steps freely:

```csharp
var outcome = await FindOrderAsync(id)                              // Task<Result<Order>>
    .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
    .Bind(ChargeAsync)                                              // ValueTask<Result<Receipt>>
    .Map(receipt => receipt.Number)
    .MapError(error => error.WithContext("Checkout"));
```

Awaiting inside the methods does not capture the synchronization context (a setting, see [Configuration](configuration.md)). Argument
validation happens when you call the method, not when you await the returned task. An already completed `Task`/`ValueTask` takes a
synchronous fast path without a state machine; a step that leaves the result as it was (`Ensure` that passes, `Tap`, `TapError`,
`MapError`, `Recover`, `Compensate`) hands back the task it received instead of allocating a new one; and a synchronous receiver with an asynchronous delegate (for `Bind`, `Compensate`
and `Match`) hands back the delegate's own task. The 909 methods are generated from one catalog (see [Building from source](contributing.md)), together with a
test for every variant.

## Async lambdas and the language version

An `async` lambda converts to both a `Task` and a `ValueTask` delegate, so a call such as `.Bind(async n => ...)` is
ambiguous to the compiler unless something breaks the tie. This library marks the `Task` overloads with C# 13's
`[OverloadResolutionPriority]`, so **with C# 13 or later `async` lambdas just work**. This also holds for the `netstandard2.0` and
`net8.0` builds, because the attribute is polyfilled inside the assembly.

On **C# 12 or earlier**, the default language version for `net8.0` projects, the compiler reports `CS0121`. Any of these fixes it:

```csharp
// 1. Opt in to a newer language version (the compiler of the .NET 9+ SDK is needed)
//    <LangVersion>latest</LangVersion>

// 2. Name the return type of the lambda
.Bind(async Task<Result<int>> (n) => { await Task.Yield(); return Result.Success(n * 2); })

// 3. Use a method group or a lambda that is not `async`
.Bind(ChargeAsync)
.Bind(n => ChargeAsync(n))
```

