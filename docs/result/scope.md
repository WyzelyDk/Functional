# Scope of Wyzely.Functional.Result

This library is about **`Result`**: the outcome of an operation that can fail, how to chain such operations, and how to describe
the failure. It is not meant to be the answer to every situation in which a result could be useful. A small, coherent surface is
a feature: every public method costs documentation, tests, analyzers and, for anything on the railway, nine generated variants.

## The admission test for new API

A new public member goes in only if the answer to every question is yes:

1. **Is it about the outcome of an operation that can fail?** Not about collections, HTTP, logging, validation frameworks, retries
   or dependency injection, even if a `Result` happens to flow through them.
2. **Does the core need it, or can a user write it in a few lines with the public API?** If three lines of `Bind`/`Match` do it, it
   does not belong here. Convenience that is merely "nice" is declined, because it can never be removed again.
3. **Is the cost proportional?** An operation on the railway becomes **nine methods** (sync/`Task`/`ValueTask` receiver times
   delegate), plus tests and documentation. Ask whether the operation is used often enough to carry that.
4. **Does it need a dependency, or does it only matter to some applications?** Then it belongs in a separate package
   (`Wyzely.Functional.Result.<Thing>`), never in this one. This package stays free of dependencies outside the BCL.
5. **Can it be misused in a way an analyzer cannot catch?** If it makes it easy to bypass the safety the library provides
   (`default` throws, `GetError()` is checked), it needs a good reason.

## Deliberately not in this library

- **`Maybe` / `Option` / `Unit` and other functional types.** They are different concepts. `Maybe` has its own package,
  [`Wyzely.Functional.Maybe`](../maybe/scope.md), and conversions between the two live in the umbrella package `Wyzely.Functional`, which
  references both, not in either.
- **HTTP, ASP.NET Core, `ProblemDetails`, gRPC mapping.** An adapter package.
- **Logging and telemetry.** An adapter package; the library does not log.
- **Retry, circuit breaker, timeouts.** A resilience library already does this and can consume `Result`.
- **Validation frameworks.** `Ensure` and `Combine` are the building blocks; the rest is the application's.
- **Serialization attributes and JSON converters.** An adapter package.
- **Implicit conversions** from a value or an error to a result, and **throwing properties** for the error or value.

## Wider than the core today (candid review)

These are in the library now but were the first to question. **Decision (2026-10-17): all of them stay.** The table is kept so that a later
change of mind is deliberate. None is wrong; each would be a candidate to remove or move if the surface ever needs
to shrink before 1.0. They are listed so the decision is deliberate.

| Area | Why it is questionable | Suggestion |
|---|---|---|
| LINQ query syntax: `Select`, `SelectMany` (`ResultLinqExtensions`) | Duplicates `Map` and `Bind`; query syntax over results is an acquired taste. | Remove unless someone asks for it. |
| `Zip` for 2 to 5 values and `Flatten` | Close to `Combine` and `Bind(x => x)`; added because they are common and `Zip` reports every failure. | Keep; the limit of 5 is one setting in the generator. |
| `Result.Combine(...)` **and** `results.Combine()` | The same operation in two shapes, six times over. | Keep one shape (the extension reads best). |
| `Combine` for `IEnumerable<Task<...>>` / `ValueTask` (and their duplicates) | `await Task.WhenAll(tasks)` followed by `Combine` does the same in one line. | Keep the `Task` forms at most. |
| `SelectInParallel` / `ForEachInParallel` | About running work in parallel rather than about results; `Parallel.ForEachAsync` plus `Combine` is close. | Kept on request, with forms for an `IAsyncEnumerable<T>` added on request (2026-10-23): they read the stream first and then run, so they do not feed work while reading. Do not grow it further (no retries, no channels). A separate package if it ever needs more. |
| `Deconstruct` (`var (value, error) = result`) | The one way to read the payload without a check that the analyzer does not see. | Consider removing, or document it as the unchecked route. |
| `Compensate` next to `Recover` | Close cousins; each is justified, but together they are two operations a reader must tell apart. | Keep, but add nothing alongside them. |
| `ContinueOnCapturedContext` setting | A process-wide switch for something that is almost always `false` in a library. | The first setting to drop if the configuration is trimmed. |
| `AggregateMessage`, `CancelledMessage` settings | Text that is closer to localization than to behavior. | Keep only while someone needs it. |
| Kind factories `Unauthorized`, `Forbidden`, `Unavailable`, `Timeout` | HTTP-flavored; they match other libraries, but they are convenience over `Error.Create(message, kind)`. | Add no further kinds; users can use `new ErrorKind("...")`. |

## How to propose a change

Say which of the five questions the change answers and how, and show the three lines a user would write today without it. If it
adds a railway operation, say how often it is needed, because that decides whether it earns its nine variants.
