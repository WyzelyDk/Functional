# Async: Task and ValueTask

[Back to the Maybe overview](../../src/Maybe/README.md)

Every operation exists in **nine variants** with the same name. The receiver can be a `Maybe<T>`, a `Task<Maybe<T>>` or a
`ValueTask<Maybe<T>>`, and the delegate can be synchronous, return a `Task`, or return a `ValueTask`:

|  | sync delegate | `Task` delegate | `ValueTask` delegate |
|---|---|---|---|
| **`Maybe<T>`** | `Maybe<T>` | `Task<Maybe<T>>` | `ValueTask<Maybe<T>>` |
| **`Task<Maybe<T>>`** | `Task<Maybe<T>>` | `Task<Maybe<T>>` | `Task<Maybe<T>>` |
| **`ValueTask<Maybe<T>>`** | `ValueTask<Maybe<T>>` | `ValueTask<Maybe<T>>` | `ValueTask<Maybe<T>>` |

The return type follows the receiver; a synchronous receiver takes the kind of the delegate. (For the operations that leave the railway,
`ValueOr`, `Match` and `Switch`, the "maybe" in the table is the plain result, `Task<T>` or `Task`, in the same way.) So a chain can mix
synchronous and asynchronous steps freely:

```csharp
using Wyzely.Functional;

// Task<Maybe<Customer>>     FindCustomerAsync(int id)             a Task step
// ValueTask<Maybe<string>>  FindEmailAsync(Customer customer)     a ValueTask step

var email = await FindCustomerAsync(7)
    .Filter(customer => customer.IsActive)
    .Bind(FindEmailAsync)
    .Map(address => address.Trim())
    .ValueOr(() => "unknown");
// "ada@example.com"   (for an unknown or inactive customer, or one without an email: "unknown")
```

Awaiting inside the methods does not capture the synchronization context (a setting, see [Configuration](configuration.md)). Argument
validation happens when you call the method, not when you await the returned task. An already completed `Task`/`ValueTask` receiver is
read directly, without a state machine, and a step that leaves the maybe as it was (`Filter` that keeps the value, `Tap`, `TapNone`, `Or`) hands
back the task it received instead of allocating a new one. The 116 methods (eleven operations times nine variants, the three variants of `Contains`, the five of `GetValueOrThrow`, and a second form of `Map` for nullable value types) are generated from one catalog (see
[Building from source](contributing.md)), together with a test for every variant. `Flatten` for a `Maybe<Maybe<T>>` also takes a `Task` or a
`ValueTask` receiver (it is `Bind` with the identity function), and `Zip` is generated too, but combines plain maybes only.

A `Task` chain over a **reference type** that ends in none allocates nothing: in the generated fast paths (8 of the generated files use it), a none is
answered with one cached completed task per type instead of a new one. (The runtime already caches the completed task of `default` for value types without
references, so `Maybe<int>` never needed it.) `PerformanceTests` checks two cases, a chain of `Map`, `Filter` and `Bind` over an already completed none
of `Maybe<string>`, and a synchronous none bound to a `Task` delegate; other variants use the same cache but have no allocation test of their own. See
[Performance](performance.md).

The awaits inside the library follow the `ContinueOnCapturedContext` setting, which defaults to not capturing the context
([Configuration](configuration.md)); `MaybeConfigurationTests` covers it. A runnable demonstration with a counting synchronization context is
`samples/Maybe.Samples/ConfigurationExample.cs`, and all nine combinations side by side are in `AsyncExample.cs`.

## Async lambdas and the language version

An `async` lambda converts to both a `Task` and a `ValueTask` delegate, so a call such as `.Bind(async n => ...)` is
ambiguous to the compiler unless something breaks the tie. This library marks the `Task` overloads with C# 13's
`[OverloadResolutionPriority]`, so **with C# 13 or later `async` lambdas just work**. This also holds for the `netstandard2.0` and
`net8.0` builds, because the attribute is polyfilled inside the assembly.

```csharp
using Wyzely.Functional;

var doubled = await Maybe.Some(21).Bind(async n =>
{
    await Task.Yield();
    return Maybe.Some(n * 2);
});                                                  // Some(42)
```

On **C# 12 or earlier**, the default language version for `net8.0` projects, the compiler reports `CS0121`. Any of these fixes it:

```csharp
// 1. Opt in to a newer language version (the compiler of the .NET 9+ SDK is needed)
//    <LangVersion>latest</LangVersion>

// 2. Name the return type of the lambda
.Bind(async Task<Maybe<int>> (n) => { await Task.Yield(); return Maybe.Some(n * 2); })

// 3. Use a method group or a lambda that is not `async`
.Bind(FindEmailAsync)
.Bind(n => FindEmailAsync(n))
```

The snippets on this page were compiled with `LangVersion` `latest`, the setting of this repository. The ambiguity (`CS0121` under C# 12, fine under
C# 13) was checked with a scratch project outside the repository; there is no test for it in the repository.
