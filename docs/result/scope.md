# Scope of Wyzely.Functional.Result

This library is about **`Result`**: the outcome of an operation that can fail, how to chain such operations, and how to describe the failure.
It is not meant to be the answer to every situation in which a result could be useful.
A coherent surface is a feature: every public method is something a developer has to find, read and rely on for as long as the library exists.

## The admission test for new API

A new public member goes in only if the answer to every question is yes:

1. **Is it about the outcome of an operation that can fail?**
   Not about collections, HTTP, logging, validation frameworks, retries or dependency injection, even if a `Result` happens to flow through them.
2. **Is the name self-explanatory, and is it the only name for what it does?**
   A developer who knows other languages or libraries should recognise what it does from the name alone.
   It is not a second name for something the library already has, and the same concept has the same name in `Result` and `Maybe`.
   Convenience is welcome when its name makes clear what it is for.
   An operation that belongs to a family is added for the whole family (every receiver, delegate kind and result form), never for part of it.
3. **Is it worth its place in the surface for good?**
   Writing, testing and documenting an operation is cheap, because the code generator and the tooling do the repetitive part
   (an operation on the railway becomes **nine methods**: sync/`Task`/`ValueTask` receiver times delegate).
   What stays expensive is what every developer who uses the library pays for as long as the operation exists:
   one more entry to find or ignore in IntelliSense and in the documentation,
   and a contract that cannot be changed or removed after 1.0 without a breaking change.
   Ask whether enough developers will recognise the operation and reach for it to justify that.
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
- **Retry, circuit breaker, timeouts.** A resilience library already does this and can consume `Result`.
- **Validation frameworks.** `Ensure` and `Combine` are the building blocks; the rest is the application's.
- **Serialization attributes and JSON converters.** An adapter package.
- **Throwing properties** for the error or value.

## Wider than the core today (candid review)

These are in the library now but were the first to question. **Decision (2026-10-17): all of them stay.**
The table is kept so that a later change of mind is deliberate.
None is wrong; each would be a candidate to remove or move if the surface ever needs to shrink before 1.0. They are listed so the decision is deliberate.

| Area | Why it is questionable | Suggestion |
|---|---|---|
| LINQ query syntax: `Select`, `SelectMany` (`ResultLinqExtensions`) | Duplicates `Map` and `Bind`; query syntax over results is an acquired taste. | Remove unless someone asks for it. |
| `Zip` for 2 to 5 values and `Flatten` | Close to `Combine` and `Bind(x => x)`; added because they are common and `Zip` reports every failure. | Keep; the limit of 5 is one setting in the generator. |
| `Result.Combine(...)` **and** `results.Combine()` | The same operation in two shapes, six times over. | Keep one shape (the extension reads best). |
| `Combine` for `IEnumerable<Task<...>>` / `ValueTask` (and their duplicates) | `await Task.WhenAll(tasks)` followed by `Combine` does the same in one line. | Keep the `Task` forms at most. |
| `SelectInParallel` / `ForEachInParallel` | About running work in parallel rather than about results; `Parallel.ForEachAsync` plus `Combine` is close. | Kept on request, with forms for an `IAsyncEnumerable<T>` added on request (2026-10-23): they read the stream first and then run, so they do not feed work while reading. Do not grow it further (no retries, no channels). A separate package if it ever needs more. |
| `Deconstruct` (`var (value, error) = result`) | The one way to read the payload without a check that the analyzer does not see. | Consider removing, or document it as the unchecked route. |
| `ContinueOnCapturedContext` setting | A process-wide switch for something that is almost always `false` in a library. | The first setting to drop if the configuration is trimmed. |
| `AggregateMessage`, `CanceledMessage` settings | Text that is closer to localization than to behavior. | Keep only while someone needs it. |
| Kind factories `Unauthorized`, `Forbidden`, `Unavailable`, `Timeout` | HTTP-flavored; they match other libraries, but they are convenience over `Error.Create(message, kind)`. | Add no further kinds; users can use `new ErrorKind("...")`. |

## How to propose a change

Say which of the five questions the change answers and how, and give the name and an example call.
If it adds a railway operation, list the whole family it brings (receivers, delegate kinds and result forms) and the number of methods.
