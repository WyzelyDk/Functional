# Interop: ToResult and ToMaybe

[Back to the Wyzely.Functional overview](../../src/Functional/README.md)

The umbrella package `Wyzely.Functional` references [`Wyzely.Functional.Result`](../../src/Result/README.md) and
[`Wyzely.Functional.Maybe`](../../src/Maybe/README.md), and adds the two methods between them. They are the only code in the umbrella package.
Where this code comes from is the same for every package of the repository (see
[where this code comes from](../../README.md#where-this-code-comes-from)). For a guided tour see [Getting started](getting-started.md),
and for the reasoning [Why an umbrella package](why.md). Runnable versions of everything on this page are in
[`samples/Functional.Samples`](../../samples/Functional.Samples).

All of it is in the namespace `Wyzely.Functional`, in the static class `MaybeResultExtensions`.

## Every overload

| Method | Receiver | Argument | Returns |
|---|---|---|---|
| `ToResult` | `Maybe<T>` | `Error` | `Result<T>` |
| `ToResult` | `Maybe<T>` | `Func<Error>` | `Result<T>` |
| `ToResult` | `Task<Maybe<T>>` | `Error` | `Task<Result<T>>` |
| `ToResult` | `Task<Maybe<T>>` | `Func<Error>` | `Task<Result<T>>` |
| `ToResult` | `ValueTask<Maybe<T>>` | `Error` | `ValueTask<Result<T>>` |
| `ToResult` | `ValueTask<Maybe<T>>` | `Func<Error>` | `ValueTask<Result<T>>` |
| `EnsureSome` | `Result<Maybe<T>>` | `Error` | `Result<T>` |
| `EnsureSome` | `Result<Maybe<T>>` | `Func<Error>` | `Result<T>` |
| `EnsureSome` | `Task<Result<Maybe<T>>>` | `Error` | `Task<Result<T>>` |
| `EnsureSome` | `Task<Result<Maybe<T>>>` | `Func<Error>` | `Task<Result<T>>` |
| `EnsureSome` | `ValueTask<Result<Maybe<T>>>` | `Error` | `ValueTask<Result<T>>` |
| `EnsureSome` | `ValueTask<Result<Maybe<T>>>` | `Func<Error>` | `ValueTask<Result<T>>` |
| `ToMaybe` | `Result<T>` | none | `Maybe<T>` |
| `ToMaybe` | `Task<Result<T>>` | none | `Task<Maybe<T>>` |
| `ToMaybe` | `ValueTask<Result<T>>` | none | `ValueTask<Maybe<T>>` |
| `ToInvertedResult` | `Maybe<T>` | `Error` | `Result` |
| `ToInvertedResult` | `Maybe<T>` | `Func<T, Error>` | `Result` |
| `ToInvertedResult` | `Task<Maybe<T>>` | `Error` | `Task<Result>` |
| `ToInvertedResult` | `Task<Maybe<T>>` | `Func<T, Error>` | `Task<Result>` |
| `ToInvertedResult` | `ValueTask<Maybe<T>>` | `Error` | `ValueTask<Result>` |
| `ToInvertedResult` | `ValueTask<Maybe<T>>` | `Func<T, Error>` | `ValueTask<Result>` |

`T` is constrained with `notnull`, as in `Maybe<T>`.

## `ToResult`: from "no value" to a failure you describe

```csharp
using Wyzely.Functional;

var found = Maybe.Some("ada@example.com");
var missing = Maybe.None<string>();

var success = found.ToResult(Error.NotFound("No such customer."));          // success with "ada@example.com"
var failure = missing.ToResult(Error.NotFound("No such customer."));        // failure: the NotFound error you gave

var lazy = missing.ToResult(() => Error.NotFound("No such customer."));     // the same, but the error is created only when needed
```

- A maybe with a value becomes a **success with that value**; a maybe without one becomes a **failure with the error you give**. The library
  does not invent an error, because "not found", "not configured" and "not allowed" are different things that only the caller knows.
- **`Func<Error>` runs only for none**, and only once. Use it when the error carries details that are costly to build, or when you would
  otherwise allocate an error for every success. The `Error` overload takes an error you already hold.
- A `null` `error`, and a `Func<Error>` that is `null`, throw `ArgumentNullException`; so does a `Func<Error>` that returns `null`. These checks happen when you call the
  method, not when you await. A `null` `Task` receiver throws as well.

## `ToMaybe`: from a result to "is there a value"

```csharp
using Wyzely.Functional;

var someValue = Result.Success(5).ToMaybe();                      // Some(5)
var noValue = Result.Failure<int>("It broke.").ToMaybe();         // None: the error is gone
var nullValue = Result.Success<string>(null!).ToMaybe();          // None: a Maybe cannot hold null
```

- A **success becomes a maybe with the value**. A **failure becomes none, and its error is dropped on purpose**: `ToMaybe()` is for the place
  where only "is there a value" matters. If you need to know why there is none, stay with the `Result`.
- A **success whose value is `null` becomes none**, because a `Maybe<T>` cannot hold `null`. (A `Result<T>` can hold a `null`, for example when it was created
  with `Result.Success<string>(null!)`; the conversion does not throw for it.)
- A **`default` `Result<T>` is a success holding `default(T)`**, so its `ToMaybe()` is a maybe with that value (none for a reference type, whose `default` is `null`). This is tested for the plain receiver.
- There is no `ToMaybe` that keeps the error, and no `Maybe.Try`; `Result.Try(...).ToMaybe()` is the explicit form of "an exception means no
  value" ([why](../maybe/design-decisions.md#no-maybetry)).

## `ToInvertedResult`: when finding something is the problem

`ToResult` treats a value as the good news. Some checks are the other way around: "no user may have this email yet". There, finding a user is the
failure and finding nothing lets the chain continue. `ToInvertedResult` turns the maybe around:

```csharp
using Wyzely.Functional;

var taken = Maybe.Some("ada@example.com");
var free = Maybe.None<string>();

var refused = taken.ToInvertedResult(Error.Conflict("The email is already registered."));   // failure: the error you gave
var allowed = free.ToInvertedResult(Error.Conflict("The email is already registered."));    // success, without a value

var detailed = taken.ToInvertedResult(email => Error.Conflict($"{email} is already registered."));   // the error is built from what was found
```

- A maybe **with** a value becomes a **failure with the error you give**, and a maybe **without** one becomes a **success**. The result is an
  untyped `Result`, because a success carries no value: nothing was found, so there is nothing to carry.
- The `Func<T, Error>` overload receives the value that was found, so that the error can say what exists already (an id, a name). It runs only
  when there is a value, and a factory that returns `null` throws `ArgumentNullException`, like the other overloads.
- The usual shape is a lookup followed by a chain that continues only when nothing was found:

```csharp
var created = await FindUserAsync(email)                                        // Task<Maybe<User>>
    .ToInvertedResult(user => Error.Conflict($"{user.Email} is already registered."))
    .Bind(() => CreateUserAsync(email));                                        // runs only when nobody had the email
```

- A `null` error or factory, and a `null` `Task` receiver, throw when you call the method. A faulted task lets its exception travel.

## Async and `ConfigureAwait`

Both methods exist for a plain receiver, a `Task<...>` and a `ValueTask<...>`, so a chain can move between the two types without an `await` in the middle:

```csharp
using Wyzely.Functional;

// Task<Maybe<Customer>>      FindCustomerAsync(int id)
// ValueTask<Result<Receipt>> ChargeAsync(Customer customer)

var outcome = await FindCustomerAsync(7)
    .ToResult(Error.NotFound("No such customer."))
    .Bind(ChargeAsync)
    .Map(receipt => receipt.Number);                  // Result<string>: "R-7", or the NotFound failure
```

- A receiver that has **already completed successfully** is converted directly, with no state machine. Otherwise the conversion waits for it
  by using the `Match` of the package that owns the receiver.
- A **faulted** task keeps travelling as it is: its exception surfaces when you await the converted task (tested for a faulted
  `Task<Maybe<T>>`). Nothing is turned into a failure or into none.
- **`ConfigureAwait` follows the settings of the two packages.** Because the waiting is done by the `Match` of the receiver's package, `ToResult` on a task that
  is not yet complete follows `MaybeConfiguration`'s `ContinueOnCapturedContext`, and `ToMaybe` follows `ResultConfiguration`'s. Both default to `false`
  (the context is not captured); see the configuration pages of [Maybe](../maybe/configuration.md) and [Result](../result/configuration.md).
  This is pinned by `InteropConfigurationTests` (six tests: each conversion captures the context only when its own package's setting says
  so, and ignores the other package's setting), and shown by [`ConfigurationExample`](../../samples/Functional.Samples/ConfigurationExample.cs).
