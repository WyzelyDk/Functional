# Scope of Wyzely.Functional.Result

This library is about **`Result`**: the outcome of an operation that can fail, how to chain such operations, and how to describe the failure.
It is not meant to be the answer to every situation in which a result could be useful.
A coherent surface is a feature: every public method is something a developer has to find, read and rely on for as long as the library exists.

## The admission test for new API

A new public member goes in only if the answer to every question is yes:

1. **Is it about the outcome of an operation that can fail?**
   Not about collections, HTTP, logging, validation frameworks, retries or dependency injection, even if a `Result` happens to flow through them.
2. **Is the name self-explanatory, and is it the only name for what it does?**
   A developer who knows other languages or libraries should recognize what it does from the name alone.
   It is not a second name for something the library already has, and the same concept has the same name in `Result` and `Maybe`.
   Convenience is welcome when its name makes clear what it is for.
   An operation that belongs to a family is added for the whole family (every receiver, delegate kind and result form), never for part of it.
3. **Is it worth its place in the surface for good?**
   Writing, testing and documenting an operation is cheap, because the code generator and the tooling do the repetitive part
   (an operation on the railway becomes **nine methods**: sync/`Task`/`ValueTask` receiver times delegate).
   What stays expensive is what every developer who uses the library pays for as long as the operation exists:
   one more entry to find or ignore in IntelliSense and in the documentation,
   and a contract that cannot be changed or removed after 1.0 without a breaking change.
   Ask whether enough developers will recognize the operation and reach for it to justify that.
4. **Does it need a dependency, or does it only matter to some applications?**
   Then it belongs in a separate package (`Wyzely.Functional.Result.<Thing>`), never in this one. This package stays free of dependencies outside the BCL.
5. **Can it be misused in a way an analyzer cannot catch?**
   If it makes it easy to bypass the safety the library provides
   (`GetValue()` and `GetError()` are checked by an analyzer, and a `default` result is a success by design), it needs a good reason.

## Deliberately not in this library

- **`Maybe` / `Option` / `Unit` and other functional types.** They are different concepts.
  `Maybe` has its own package, [`Wyzely.Functional.Maybe`](../maybe/scope.md), and conversions between the two live in the umbrella package `Wyzely.Functional`,
  which references both, not in either.
- **HTTP, ASP.NET Core, `ProblemDetails`, gRPC mapping.** An adapter package.
- **Logging and telemetry.** An adapter package; the library does not log.
- **`Zip` for a `Task` or a `ValueTask` of a result.**
  Every argument could be plain, a `Task` or a `ValueTask`, so a complete family has 3 to the power of the number of results overloads per form,
  and a partial one leaves holes. Await the tasks first and zip the results ([Async](async.md#zip-and-tasks)).
- **Retry, circuit breaker, timeouts.** A resilience library already does this and can consume `Result`.
- **Validation frameworks.** `Ensure` and `Combine` are the building blocks; the rest is the application's.
- **JSON converters.** `System.Text.Json` refuses the types on purpose ([Serialization](safety-and-analyzers.md#serialization)); a converter that
  reads and writes them would be an adapter package.
- **Throwing properties** for the error or value.
- **`OfType` and `Cast` for a result.** A failed cast has no natural error to become: a result has no error for a value of the wrong type.
  `Maybe` has `OfType` (the wrong type is none) and no `Cast`.

## Wider than the core today (candid review)

These are in the library but were the first to question. **Decision for 1.0 (2026-11-20): the surface is frozen as it is, and all of them stay.**
It confirms the decision of 2026-10-17. After 1.0, removing any of them is a breaking change, and adding something is not, so what is unsure waits
(see below) instead of going in now. The table is kept so that a later change of mind is deliberate.

| Area | Why it was questioned | Decision for 1.0 |
|---|---|---|
| LINQ query syntax: `Select`, `SelectMany` (`ResultLinqExtensions`) | Duplicates `Map` and `Bind`; query syntax over results is an acquired taste. | Stays. C# query syntax calls those two names, so they are the documented exception to the naming rule (`Map`, `Bind`). |
| `Zip` for 2 to 5 values and `Flatten` | Close to `Combine` and `Bind(x => x)`. | Stays. `Zip` reports every failure; the limit of 5 is one setting of the generator. |
| `Result.Combine(...)` **and** `results.Combine()` | The same operation in two shapes, six times over. | Both stay. A collection expression has no type of its own, so `[a, b].Combine()` does not compile (`CS9176`) while `Result.Combine([a, b])` does; the extension reads best for a sequence that already exists. |
| `Combine` for `IEnumerable<Task<...>>` / `ValueTask` (and their duplicates) | `await Task.WhenAll(tasks)` followed by `Combine` does the same in one line. | Stays. A sequence of results can be combined in every receiver kind, and the `ValueTask` form reads a value task that has already completed without making a `Task` for it. |
| `SelectInParallel` / `ForEachInParallel` | About running work in parallel rather than about results; `Parallel.ForEachAsync` plus `Combine` is close. | Stays. It was kept on request, with forms for an `IAsyncEnumerable<T>` added on request (2026-10-23): they read the stream first and then run, so they do not feed work while reading. Do not grow it further (no retries, no channels); a separate package if it ever needs more. The names follow the naming rule: an operation on a plain sequence uses LINQ's words. |
| `Deconstruct` (`var (value, error) = result`) | The one way to read the payload without a check that the analyzer does not see. | Stays, and is documented as the unchecked route in [Safety and analyzers](safety-and-analyzers.md). |
| `ContinueOnCapturedContext` setting | A process-wide switch for something that is almost always `false` in a library. | Stays; the default is `false` ([Configuration](configuration.md)). |
| `AggregateMessage`, `CanceledMessage` settings | Text that is closer to localization than to behavior. | Stay; they are settings with defaults ([Configuration](configuration.md)). |
| Kind factories `Unauthorized`, `Forbidden`, `Unavailable`, `Timeout` | HTTP-flavored; they match other libraries, but they are convenience over `Error.Create(message, kind)`. | Stay. Add no further kinds; users can use `new ErrorKind("...")`. |

## Considered, and not in 1.0

Adding an operation later is not a breaking change, so a candidate waits until someone needs it, and then goes through the admission test above.
Two were looked at for 1.0 and left out:

- **`Error.Is(kind)`.** `error.Kind == ErrorKind.NotFound` already reads the same: `ErrorKind` has `==`, `!=` and `Equals`.
- **`Tap`, `Match` and `Ensure` with separate parameters for a tuple value.** `Map` and `Bind` have them (the `Zip` family: a delegate that takes the elements
  of the tuple); `Tap`, `Match` and `Ensure` take the tuple itself.

## How to propose a change

Say which of the five questions the change answers and how, and give the name and an example call.
If it adds a railway operation, list the whole family it brings (receivers, delegate kinds and result forms) and the number of methods.
