# Performance

[Back to the Result overview](../../src/Result/README.md)

These facts are checked by tests (`PerformanceTests`, `ExtensionReceiverTests`), not just stated:

- `Result` is exactly the size of one reference; `Result<T>` is `T` plus one reference.
- Creating results, `Map`, `Bind`, `Ensure`, `Tap`, `MapError` and `Match` on the success path allocate **0 bytes** when the
  delegates are `static` lambdas (measured with `GC.GetAllocatedBytesForCurrentThread`).
- An already completed `Task` or `ValueTask` receiver takes a synchronous fast path without a state machine,
  and a synchronous receiver with a `Task`/`ValueTask` delegate hands back the task of the delegate
  for `Bind`, `Match` and `OrElse` with an alternative operation.
- On an already completed `Task`, a step that leaves the result as it was hands back the task it received and allocates nothing:
  `Ensure` that passes or meets a failure (with a predicate or with an operation), `Tap`, `TapError`, `MapError`,
  `OrElse` on a result it does not change (`CompletedTaskReuseTests`). `Map` and `Bind` never do, because they can change the value.
- On an already completed `Task`, a step that ends in a **successful result without a value** (for example `Bind` to an
  operation that returns a `Result`) hands out one shared completed task instead of allocating a new one (`PerformanceTests`).
  A result with a value is a different task for every value, so it cannot be shared.
- A ready-made error is one object: `Error.Create`, `Unavailable`, `Timeout` and `FromException` each allocate exactly one `Error`,
  and read the message of an exception once (`ErrorAllocationTests`).
- Receivers are passed **by value**, not by `in`, on purpose. `in` is faster when a result is read from a field,
  but a chain passes the result of the previous call (a temporary), whose address then has to be taken, which forces it into memory.
  In the `ParameterPassing` benchmark a four-step chain over a `Result<int>` took 0.43 ns by value and 0.55 ns with `in`,
  and a chain over a 72-byte `Result<T>` took 4.3 ns by value and 2.7 ns with `in`. `ExtensionReceiverTests` keeps the receivers by value.
- Each public method checks its delegate and `Task` arguments for `null` up front (an inlined branch).
  On the async chains this cost about 0.5 ns per call (roughly 5-8 % of a 24 ns chain) when measured with and without the checks,
  in exchange for an `ArgumentNullException` at the call instead of a `NullReferenceException` later, or never, depending on the data.

## Benchmarks against the other libraries

`benchmarks/Result.Benchmarks` runs the same scenarios against eight other libraries, with this library as the baseline in every table:
CSharpFunctionalExtensions 3.7.0, FluentResults 4.0.0, ErrorOr 2.1.1, Ardalis.Result 10.1.0, LanguageExt 4.4.9 (`Fin<A>`),
Funcky 3.8.0 (`Result<T>`), Optional 4.0.0 (`Option<T, TException>`) and OneOf 3.0.271 (a union, written as `OneOf<T, Error<string>>`).
The libraries are described in the [comparison](comparison.md). **Treat the numbers as indicative, not as a verdict.**
The caveats are at the end of this section; read them before you quote a cell.

Every scenario exists twice: with a **value type** (`int`) and with a **reference type** (a `Customer` class, from which the chain takes the name, a `string`,
and carries it on). The chains never allocate a new payload, so the allocation columns show what the result type costs and not what the payload costs.

**How to read a cell:** `5.71 ns, 0 B (1.1×)` is the mean time per operation, the bytes allocated per operation,
and the time relative to this library (smaller than `1.0×` means faster than this library). The ratio is left out when this library's own time is below 0.5 ns,
as a ratio of such small numbers says nothing. The measurement setup: BenchmarkDotNet 0.15.8, .NET 10.0.15, an Intel Core Ultra 7 265H, Windows 11,
the in-process toolchain, 5 warm-up and 10 measured iterations of 100 ms per benchmark, one full run on 2026-11-20.

**What "nearest equivalent" means.** The chains are written the way a user of each library would write them, with the closest operations it has:

- A library without `Ensure` (FluentResults, Ardalis.Result, LanguageExt, Funcky, OneOf) does the check in a `Bind` step (or, for OneOf, in `Match`).
  OneOf has only `MapT0` and `Match`, so its chains are written with `Match`.
- A library without extension methods for `Task<Result<T>>` or `ValueTask<Result<T>>` is measured the way its user must write it: **await first,
  then run the synchronous chain** (LanguageExt, Funcky, Optional and OneOf for both; FluentResults, Ardalis.Result and ErrorOr for `ValueTask`).
  In the scenario with an asynchronous step after a synchronous result, every library except this one,
  CSharpFunctionalExtensions and ErrorOr is measured that way. The same style is shown for this library as an extra row, so the two ways can be compared.
- A library without a `Try` wrapper (ErrorOr, Ardalis.Result, OneOf, Funcky, Optional) is measured with the `try`/`catch` its user would write.
- Errors exist in advance (a static field or a literal) unless the scenario says it creates them. Delegates are `static` lambdas.

The code is in the repository. If you think a chain is written unfairly for a library, say so: it is a judgment, not a measurement.

### Synchronous chain, every step succeeds

Four steps: `Map`, `Ensure`, `Bind`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 5.03 ns, 0 B | 6.04 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 5.71 ns, 0 B (1.1×) | 8.49 ns, 0 B (1.4×) |
| FluentResults 4.0.0 | 185 ns, 1104 B (37×) | 228 ns, 1104 B (38×) |
| ErrorOr 2.1.1 | 6.00 ns, 0 B (1.2×) | 7.04 ns, 0 B (1.2×) |
| Ardalis.Result 10.1.0 | 25.4 ns, 256 B (5.1×) | 36.9 ns, 288 B (6.1×) |
| LanguageExt 4.4.9 `Fin<A>` | 18.0 ns, 0 B (3.6×) | 22.1 ns, 0 B (3.6×) |
| Funcky 3.8.0 `Result<T>` | 40.9 ns, 88 B (8.1×) | 48.1 ns, 88 B (8.0×) |
| Optional 4.0.0 `Option<T, TException>` | 10.6 ns, 24 B (2.1×) | 14.5 ns, 24 B (2.4×) |
| OneOf 3.0.271 | 6.24 ns, 0 B (1.2×) | 8.91 ns, 0 B (1.5×) |

### Synchronous chain, a step fails

The same chain, but the check fails, so the later steps are skipped and the error is matched.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 4.68 ns, 0 B | 4.44 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 8.64 ns, 0 B (1.8×) | 9.50 ns, 0 B (2.1×) |
| FluentResults 4.0.0 | 260 ns, 1200 B (56×) | 201 ns, 1200 B (45×) |
| ErrorOr 2.1.1 | 22.6 ns, 88 B (4.8×) | 21.7 ns, 88 B (4.9×) |
| Ardalis.Result 10.1.0 | 46.2 ns, 320 B (9.9×) | 55.3 ns, 352 B (12×) |
| LanguageExt 4.4.9 `Fin<A>` | 30.5 ns, 0 B (6.5×) | 23.4 ns, 0 B (5.3×) |
| Funcky 3.8.0 `Result<T>` | 45.5 ns, 88 B (9.7×) | 41.4 ns, 88 B (9.3×) |
| Optional 4.0.0 `Option<T, TException>` | 11.4 ns, 24 B (2.4×) | 14.8 ns, 24 B (3.3×) |
| OneOf 3.0.271 | 7.45 ns, 0 B (1.6×) | 7.64 ns, 0 B (1.7×) |

### Synchronous chain of eight steps

`Map`, `Ensure`, `Bind`, `Map`, `Ensure`, `Bind`, `Map`, `Match`: does the cost grow with the number of steps?

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 10.1 ns, 0 B | 12.1 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 13.7 ns, 0 B (1.4×) | 16.5 ns, 0 B (1.4×) |
| FluentResults 4.0.0 | 446 ns, 2256 B (44×) | 471 ns, 2256 B (39×) |
| ErrorOr 2.1.1 | 12.2 ns, 0 B (1.2×) | 15.1 ns, 0 B (1.3×) |
| Ardalis.Result 10.1.0 | 50.9 ns, 448 B (5.0×) | 60.7 ns, 504 B (5.0×) |
| LanguageExt 4.4.9 `Fin<A>` | 38.0 ns, 0 B (3.8×) | 40.2 ns, 0 B (3.3×) |
| Funcky 3.8.0 `Result<T>` | 109 ns, 264 B (11×) | 105 ns, 264 B (8.7×) |
| Optional 4.0.0 `Option<T, TException>` | 25.7 ns, 72 B (2.5×) | 36.0 ns, 72 B (3.0×) |
| OneOf 3.0.271 | 13.1 ns, 0 B (1.3×) | 17.6 ns, 0 B (1.5×) |

### Returning a result from a method

A method that is not inlined creates a successful result and returns it, and the caller reads the value.
(Inlined, the compiler removes the result type of the struct libraries altogether.)

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 0.994 ns, 0 B | 1.97 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 1.09 ns, 0 B (1.1×) | 2.17 ns, 0 B (1.1×) |
| FluentResults 4.0.0 | 28.0 ns, 160 B (28×) | 24.9 ns, 160 B (13×) |
| ErrorOr 2.1.1 | 0.990 ns, 0 B (1.0×) | 1.98 ns, 0 B (1.0×) |
| Ardalis.Result 10.1.0 | 7.22 ns, 64 B (7.3×) | 9.90 ns, 72 B (5.0×) |
| LanguageExt 4.4.9 `Fin<A>` | 2.19 ns, 0 B (2.2×) | 3.22 ns, 0 B (1.6×) |
| Funcky 3.8.0 `Result<T>` | 2.71 ns, 0 B (2.7×) | 4.12 ns, 0 B (2.1×) |
| Optional 4.0.0 `Option<T, TException>` | 1.27 ns, 0 B (1.3×) | 3.16 ns, 0 B (1.6×) |
| OneOf 3.0.271 | 1.43 ns, 0 B (1.4×) | 1.99 ns, 0 B (1.0×) |

> **This library is level with the fastest here.** The struct libraries are within a nanosecond of each other.
> For the `int` ErrorOr (0.990 ns) and this library (0.994 ns) are level; for the `Customer` this library (1.97 ns), ErrorOr (1.98 ns)
> and OneOf (1.99 ns) are level, and CSharpFunctionalExtensions (2.17 ns) is a fifth of a nanosecond behind.
> In the previous run ErrorOr was a quarter of a nanosecond ahead for the `int` (0.98 ns against 1.21 ns) and OneOf about 0.6 ns ahead for the `Customer`
> (1.27 ns against 1.87 ns). A result stores a single `null` for success here, as ErrorOr does, and is 16 bytes for either payload,
> so we did not find a stable difference, and a difference of this size moves from run to run.

### Creating a failure

A method that is not inlined creates a failed result with a new error and returns it; the caller asks whether it failed.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 15.4 ns, 96 B | 14.1 ns, 96 B |
| CSharpFunctionalExtensions 3.7.0 | 0.982 ns, 0 B (0.1×) | 1.00 ns, 0 B (0.1×) |
| FluentResults 4.0.0 | 50.2 ns, 360 B (3.3×) | 52.5 ns, 360 B (3.7×) |
| ErrorOr 2.1.1 | 9.29 ns, 88 B (0.6×) | 11.1 ns, 88 B (0.8×) |
| Ardalis.Result 10.1.0 | 10.9 ns, 96 B (0.7×) | 11.4 ns, 104 B (0.8×) |
| LanguageExt 4.4.9 `Fin<A>` | 7.14 ns, 48 B (0.5×) | 6.71 ns, 48 B (0.5×) |
| Funcky 3.8.0 `Result<T>` | 19 μs, 17560 B (1234×) | 19 μs, 18026 B (1338×) |
| Optional 4.0.0 `Option<T, TException>` | 1.06 ns, 0 B (0.1×) | 0.912 ns, 0 B (0.1×) |
| OneOf 3.0.271 | 1.27 ns, 0 B (0.1×) | 1.06 ns, 0 B (0.1×) |
| Wyzely, a rich error (kind, detail, context frame) | 49.2 ns, 368 B (3.2×) | n/a |

> **Why this library is not the fastest here.** An error is an object here: a plain one is 96 B and takes 14 to 15 ns to create,
> because it can carry a message template, details, a kind, a code, a cause and context. CSharpFunctionalExtensions,
> Optional and OneOf are faster because their error is a string literal or a struct, which costs nothing to create, and they cannot carry any of that.
> Among the libraries whose error is an object this library allocates about the same (ErrorOr 88 B, Ardalis.Result 96 to 104 B) but was slower to create it
> in this run (14 to 15 ns, against 9 to 11 ns for those two), and LanguageExt (48 B, 7 ns) is smaller and faster.
> Every factory builds exactly one object (`ErrorAllocationTests`). An error that is created once and reused, as in the chains above, costs nothing.

### A completed `Task<Result<T>>` as the receiver

The receiver is a task that is already completed and every delegate is synchronous: what does the asynchronous machinery cost?

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 24.3 ns, 160 B | 27.4 ns, 160 B |
| CSharpFunctionalExtensions 3.7.0 | 54.9 ns, 240 B (2.3×) | 63.3 ns, 264 B (2.3×) |
| FluentResults 4.0.0 | 220 ns, 1160 B (9.1×) | 209 ns, 1160 B (7.6×) |
| ErrorOr 2.1.1 | 49.3 ns, 240 B (2.0×) | 56.9 ns, 240 B (2.1×) |
| Ardalis.Result 10.1.0 | 63.3 ns, 408 B (2.6×) | 64.7 ns, 432 B (2.4×) |
| LanguageExt 4.4.9 `Fin<A>` | 26.7 ns, 0 B (1.1×) | 27.8 ns, 0 B (1.0×) |
| Funcky 3.8.0 `Result<T>` | 49.2 ns, 88 B (2.0×) | 48.6 ns, 88 B (1.8×) |
| Optional 4.0.0 `Option<T, TException>` | 17.6 ns, 24 B (0.7×) | 21.7 ns, 24 B (0.8×) |
| OneOf 3.0.271 | 15.3 ns, 0 B (0.6×) | 15.8 ns, 0 B (0.6×) |
| Wyzely, awaiting first and then running the synchronous chain | 11.0 ns, 0 B (0.5×) | 11.1 ns, 0 B (0.4×) |

> **Why this library is not the fastest here.** A method that returns a `Task<Result<T>>` has to allocate a task for every step that changes the result,
> so a chain of two such steps is 160 B whatever the library does: nothing below that is possible with this return type.
> LanguageExt, Optional, OneOf and Funcky are faster or allocate less only because they have no such methods, so their users await first, and so can you:
> the last row is as fast as theirs or faster (11.0 ns against 15.3 ns for OneOf, 0 B).
> Among the libraries that do offer `Task` methods this one is the fastest (roughly
> twice as fast as CSharpFunctionalExtensions and ErrorOr) and allocates the least; steps that leave the result as it was hand back the task they received.

### A completed `ValueTask<Result<T>>` as the receiver

The same with a `ValueTask`, which exists so that work that completes synchronously allocates nothing.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 11.7 ns, 0 B | 12.2 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 95.3 ns, 0 B (8.1×) | 97.9 ns, 0 B (8.1×) |
| FluentResults 4.0.0 | 179 ns, 1056 B (15×) | 224 ns, 1056 B (18×) |
| ErrorOr 2.1.1 | 15.2 ns, 0 B (1.3×) | 17.2 ns, 0 B (1.4×) |
| Ardalis.Result 10.1.0 | 39.9 ns, 256 B (3.4×) | 41.4 ns, 288 B (3.4×) |
| LanguageExt 4.4.9 `Fin<A>` | 27.4 ns, 0 B (2.3×) | 28.2 ns, 0 B (2.3×) |
| Funcky 3.8.0 `Result<T>` | 50.1 ns, 88 B (4.3×) | 51.8 ns, 88 B (4.3×) |
| Optional 4.0.0 `Option<T, TException>` | 19.1 ns, 24 B (1.6×) | 22.8 ns, 24 B (1.9×) |
| OneOf 3.0.271 | 14.7 ns, 0 B (1.3×) | 18.4 ns, 0 B (1.5×) |
| Wyzely, awaiting first and then running the synchronous chain | 11.8 ns, 0 B (1.0×) | 13.0 ns, 0 B (1.1×) |

### A synchronous result followed by an asynchronous step

The receiver is a plain result, the first step is an asynchronous delegate that returns an already completed task, and a `Map` follows.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 17.6 ns, 80 B | 17.2 ns, 80 B |
| CSharpFunctionalExtensions 3.7.0 | 31.1 ns, 80 B (1.8×) | 29.4 ns, 80 B (1.7×) |
| FluentResults 4.0.0 | 50.8 ns, 256 B (2.9×) | 51.7 ns, 256 B (3.0×) |
| ErrorOr 2.1.1 | 35.0 ns, 160 B (2.0×) | 35.5 ns, 160 B (2.1×) |
| Ardalis.Result 10.1.0 | 8.40 ns, 0 B (0.5×) | 8.74 ns, 0 B (0.5×) |
| LanguageExt 4.4.9 `Fin<A>` | 8.99 ns, 0 B (0.5×) | 9.37 ns, 0 B (0.5×) |
| Funcky 3.8.0 `Result<T>` | 12.1 ns, 0 B (0.7×) | 12.3 ns, 0 B (0.7×) |
| Optional 4.0.0 `Option<T, TException>` | 13.6 ns, 24 B (0.8×) | 12.9 ns, 24 B (0.8×) |
| OneOf 3.0.271 | 9.31 ns, 0 B (0.5×) | 9.16 ns, 0 B (0.5×) |
| Wyzely, awaiting first and then running the synchronous chain | 9.83 ns, 0 B (0.6×) | 9.17 ns, 0 B (0.5×) |

> **Why this library is not the fastest here.** The `Map` that follows the asynchronous step has to return a task with a new value, which is the 80 B.
> The libraries in the table that are faster are measured with hand-written code (check the result, await the step), which has no task to create;
> the last row is the same thing written with this library (9.2 to 9.8 ns, 0 B), which is within about 1.5 ns of the fastest of them (8.4 to 8.7 ns).
> Among the libraries that offer a `Bind` with an asynchronous delegate this one is the fastest.

### Code that can throw, turned into a result

`Result.Try` or its nearest equivalent, around a call that succeeds and around a call that throws (`int.Parse` for the `int`,
a lookup that throws for the `Customer`). A throw costs microseconds whatever the library, so the failure columns are dominated by the exception.

| Library | `int`, call succeeds | `int`, call throws | `Customer`, call succeeds | `Customer`, call throws |
|---|---|---|---|---|
| **Wyzely.Functional.Result** | 6.12 ns, 0 B | 1.8 μs, 560 B | 3.10 ns, 0 B | 1.6 μs, 552 B |
| CSharpFunctionalExtensions 3.7.0 | 13.8 ns, 0 B (2.3×) | 1.6 μs, 464 B (0.9×) | 13.3 ns, 0 B (4.3×) | 1.5 μs, 456 B (0.9×) |
| FluentResults 4.0.0 | 41.5 ns, 208 B (6.8×) | 2.0 μs, 1080 B (1.1×) | 45.0 ns, 208 B (15×) | 1.9 μs, 1072 B (1.2×) |
| ErrorOr 2.1.1 | 8.05 ns, 0 B (1.3×) | 1.6 μs, 552 B (0.9×) | 4.50 ns, 0 B (1.5×) | 1.6 μs, 544 B (1.0×) |
| Ardalis.Result 10.1.0 | 14.4 ns, 64 B (2.4×) | 1.6 μs, 560 B (0.9×) | 10.5 ns, 72 B (3.4×) | 1.5 μs, 560 B (0.9×) |
| LanguageExt 4.4.9 `Fin<A>` | 40.5 ns, 200 B (6.6×) | 1.9 μs, 664 B (1.0×) | 38.0 ns, 208 B (12×) | 2.0 μs, 704 B (1.2×) |
| Funcky 3.8.0 `Result<T>` | 8.03 ns, 0 B (1.3×) | 5.8 μs, 3513 B (3.2×) | 5.46 ns, 0 B (1.8×) | 6.0 μs, 3945 B (3.6×) |
| Optional 4.0.0 `Option<T, TException>` | 7.51 ns, 0 B (1.2×) | 1.6 μs, 464 B (0.9×) | 4.07 ns, 0 B (1.3×) | 1.4 μs, 328 B (0.8×) |
| OneOf 3.0.271 | 8.13 ns, 0 B (1.3×) | 1.7 μs, 464 B (0.9×) | 4.30 ns, 0 B (1.4×) | 1.5 μs, 456 B (0.9×) |

> **Why this library is not the fastest on the failure columns.**
> A throw costs about 2 μs whatever the library does, and the columns differ by less than the run-to-run noise of an exception
> (1.4 to 2.0 μs in this run, 1.3 to 2.3 μs in the previous one).
> This library allocates 96 B more than a hand-written `try`/`catch` that returns a string or a struct, and that is the `Error` object.
> On the success columns it is the fastest. Funcky is about three times slower on the failure path (about 6 μs).

### Conditional steps

`MapIf`, `BindIf` and `EnsureNot`, then `Match`: a step that runs only when a condition holds.
A library without conditional methods has the condition written inside the step, as a conditional expression (and a failing check as a `Bind`);
CSharpFunctionalExtensions has `MapIf`, `BindIf` and `EnsureNot` of its own. Every library computes the same value.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 1.62 ns, 0 B | 3.45 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 21.3 ns, 24 B (13×) | 30.4 ns, 24 B (8.8×) |
| FluentResults 4.0.0 | 183 ns, 1104 B (113×) | 230 ns, 1104 B (67×) |
| ErrorOr 2.1.1 | 2.74 ns, 0 B (1.7×) | 3.28 ns, 0 B (0.9×) |
| Ardalis.Result 10.1.0 | 26.6 ns, 256 B (16×) | 36.2 ns, 288 B (10×) |
| LanguageExt 4.4.9 `Fin<A>` | 17.2 ns, 0 B (11×) | 21.1 ns, 0 B (6.1×) |
| Funcky 3.8.0 `Result<T>` | 39.7 ns, 88 B (24×) | 43.2 ns, 88 B (13×) |
| Optional 4.0.0 `Option<T, TException>` | 7.97 ns, 24 B (4.9×) | 9.60 ns, 24 B (2.8×) |
| OneOf 3.0.271 | 3.59 ns, 0 B (2.2×) | 6.73 ns, 0 B (1.9×) |
| Wyzely, written with `Map`, `Bind` and `Ensure` instead of the conditional methods | 1.56 ns, 0 B (1.0×) | 2.80 ns, 0 B (0.8×) |

> **What this shows.** The conditional methods cost what the steps they replace cost: the last row, the same chain written with `Map`,
> `Bind` and `Ensure`, took 1.56 ns for the `int` against 1.62 ns, and 2.80 ns against 3.45 ns for the `Customer`, which is inside the noise.
> At these sizes the JIT inlines the whole chain, and a difference of a nanosecond says nothing about the libraries.
> ErrorOr (2.74 and 3.28 ns) and OneOf (3.59 and 6.73 ns) are in the same range, and ErrorOr was slightly ahead for the `Customer`; we did not find why.
> CSharpFunctionalExtensions offers the same three methods, took 9 to 13 times as long and allocated 24 B;
> we did not investigate where the allocation comes from.

### Combining results

Eight results combined into one result that holds the list of the values; in the failing columns the fifth of them has failed.
CSharpFunctionalExtensions (`Combine`), FluentResults (`Merge`), Funcky (`Sequence`) and LanguageExt (`Sequence`) offer the operation.
ErrorOr, Ardalis.Result, Optional and OneOf do not, so a loop that a user would write is measured for them. Every library computes the same value.

| Library | `int`, all succeed | `int`, one fails | `string`, all succeed | `string`, one fails |
|---|---|---|---|---|
| **Wyzely.Functional.Result** | 31.6 ns, 128 B | 83.4 ns, 432 B | 39.1 ns, 176 B | 87.1 ns, 480 B |
| CSharpFunctionalExtensions 3.7.0 | 178 ns, 448 B (5.6×) | 255 ns, 816 B (3.1×) | 167 ns, 528 B (4.3×) | 249 ns, 888 B (2.9×) |
| FluentResults 4.0.0 | 418 ns, 1104 B (13×) | 235 ns, 640 B (2.8×) | 281 ns, 1136 B (7.2×) | 176 ns, 640 B (2.0×) |
| ErrorOr 2.1.1 | 18.5 ns, 88 B (0.6×) | 13.4 ns, 88 B (0.2×) | 31.3 ns, 120 B (0.8×) | 21.5 ns, 120 B (0.2×) |
| Ardalis.Result 10.1.0 | 19.6 ns, 88 B (0.6×) | 13.3 ns, 88 B (0.2×) | 30.6 ns, 120 B (0.8×) | 18.2 ns, 120 B (0.2×) |
| LanguageExt 4.4.9 `Fin<A>` | 42.2 ns, 128 B (1.3×) | 20.8 ns, 72 B (0.2×) | 61.2 ns, 176 B (1.6×) | 25.0 ns, 88 B (0.3×) |
| Funcky 3.8.0 `Result<T>` | 122 ns, 168 B (3.9×) | 67.8 ns, 88 B (0.8×) | 139 ns, 232 B (3.5×) | 85.8 ns, 120 B (1.0×) |
| Optional 4.0.0 `Option<T, TException>` | 19.4 ns, 88 B (0.6×) | 13.0 ns, 88 B (0.2×) | 25.6 ns, 120 B (0.7×) | 15.2 ns, 120 B (0.2×) |
| OneOf 3.0.271 | 17.4 ns, 88 B (0.5×) | 12.9 ns, 88 B (0.2×) | 26.8 ns, 120 B (0.7×) | 16.0 ns, 120 B (0.2×) |

> **The hand-written loops are faster, and they do less.** A loop stops at the first failure and keeps one error (ErrorOr, Ardalis.Result,
> Optional and OneOf: 13 to 31 ns, 88 to 120 B). `Combine` in this library reads every result and aggregates the errors of all the failures,
> so that a consumer sees them at once; that is the difference between 13 to 22 ns and 83 to 87 ns in the failing columns.
> Among the libraries that offer the operation this one is the fastest when all succeed (32 and 39 ns;
> LanguageExt 42 and 61 ns) and allocates the same as LanguageExt.
> When one fails it is behind LanguageExt, and behind Funcky for the `int` (83 against 68 ns) but level with it for the `string` (87 against 86 ns).
> They are probably stopping at the first failure (we read that from the times, not from their code).
> We did not investigate the 128 B on the success path, against 88 B for the loops.

### What the numbers say

- **Synchronous chains: this library is the fastest, with ErrorOr, OneOf and CSharpFunctionalExtensions close behind, and none of them allocates.**
  The four-step chain took 5.0 ns here and 5.7 to 6.2 ns in those three for an `int` (6.0 ns against 7.0 to 8.9 ns for a `Customer`);
  differences of this size can change order from run to run. The others cost more: Optional 2 times (and 24 B), LanguageExt 3.6 times,
  Ardalis.Result 5 to 6 times (it is a class), Funcky 8 times (88 B) and FluentResults 37 to 38 times (1,104 B). The eight-step chain shows the same picture.
- **The failure path is cheap here because the error already exists.** With a ready-made error this library took about 4.5 ns and allocated nothing.
  OneOf took 7 to 8 ns (also without allocating), CSharpFunctionalExtensions and Optional two to three times as long,
  and ErrorOr 22 to 23 ns and 88 B (its list of errors), Ardalis.Result 320 to 352 B, FluentResults 1,200 B.
- **Creating an error is a cost of its own, and this library is not the cheapest at it** (see the note under the table).
  A plain `Error` is one object, 96 B and 14 to 15 ns, and a rich one with a detail and a context frame is 368 B and about 49 ns.
  Funcky's `Result<T>.Error(exception)` took about 19 μs and 17 to 18 KB; we did not investigate why.
- **Returning a result from a method costs 1 to 4 ns for every struct library, and the differences are a few tenths of a nanosecond.**
  FluentResults and Ardalis.Result allocate (160 B and 64 to 72 B) and take 7 to 28 ns.
- **Chaining on a completed `Task` allocates, for every library that offers it, and awaiting first is cheaper.**
  A step that changes the result creates a new task, and the chain here (`Map`, `Ensure`, `Bind`) took 160 B and 24 to 27 ns:
  the `Ensure` step hands back the task it received when the result is left as it was, as do `Tap`, `TapError`, `MapError` and `OrElse`,
  which saved 80 B and about a quarter of the time per such step. The other libraries with `Task` methods took 240 to 264 B (CSharpFunctionalExtensions,
  ErrorOr), 408 to 432 B (Ardalis.Result) and 1,160 B (FluentResults), and two to nine times as long.
  Awaiting the task first and then running the synchronous chain allocated nothing and took about 11 ns, about twice as fast (see the extra row).
  **If you can await, await, and chain on the result.**
  The `Task` extension methods are for when you want the chain to read as one expression, or when the result has to stay in a task.
- **`ValueTask`: this library is the cheapest of the two that offer extension methods for it.** With a completed `ValueTask`,
  this library allocated nothing and took 12 ns. CSharpFunctionalExtensions, the only other one with `ValueTask` extension methods, took 95 to 98 ns,
  about eight times as long, also without allocating. For the libraries without them, await first and a synchronous chain took 15 to 52 ns,
  and FluentResults and Ardalis.Result allocated (1,056 B and 256 to 288 B).
- **A synchronous result followed by an asynchronous step** allocated 80 B in this library and CSharpFunctionalExtensions (the task of the `Map`),
  and nothing when the code checks the result and awaits by hand; the by-hand version took 44 to 47 % less time in this library's case.
- **`Try` on the success path** took 3 to 6 ns and allocated nothing in this library, and 4 to 8 ns in Funcky,
  Optional, OneOf and ErrorOr (hand-written `try`/`catch`, so the cost is the `try` region).
  CSharpFunctionalExtensions took 13 to 14 ns, and FluentResults and LanguageExt 38 to 45 ns and about 200 B.
  **On the failure path every library took 1.4 to 2.0 μs, which is the cost of the exception**, except Funcky at about 6 μs and 3.5 to 3.9 KB.
- **Conditional steps cost what the steps they replace cost.**
  `MapIf`, `BindIf` and `EnsureNot` took 1.6 ns (`int`) and 3.5 ns (`Customer`) in a four-step chain, the same as the version written with `Map`,
  `Bind` and `Ensure` within the noise. Use them for the readability.
- **`Combine` is fastest of the libraries that offer it when everything succeeds,
  but a hand-written loop is faster, because it stops at the first failure and this one collects every error.**
  All succeeding it took 32 to 39 ns and 128 to 176 B for eight results; with one failure 83 to 87 ns, against 13 to 22 ns for the loops.
- **Value type and reference type behave alike.** The struct libraries allocate nothing in either; the class-based libraries allocate in both.
  Where a reference payload costs more, it is the larger struct (24 B instead of 16 B in most libraries), not an allocation.

### Caveats

- **One machine, one configuration.** The numbers are from one Windows machine and .NET 10.
  The previous full run (2026-10-25, .NET 10.0.13) differs from this one by a median of 11 % per table cell (217 compared, every cell with a time
  except Funcky, which changed version), with the worst at 59 % and 22 of the cells of 2 ns or more above 25 %;
  the ratios of sub-10 ns benchmarks are the least stable. Creating a plain `Error` took 11.0 ns in the previous run and 15.4 ns in this one,
  and OneOf returning a `Customer` took 1.27 ns and 1.99 ns, so even a number that is the baseline of its table is not a constant
  (the eight-step `int` chain of this library took 10.1 ns in both).
  The runtime's tiered profile-guided optimization changes how well it inlines delegate calls,
  and earlier runs of this suite with other settings ranked the synchronous chains differently. Do not read a rank from a 10 % difference.
- **The scenarios are small on purpose.** They isolate the result type, with `static` lambdas and no I/O.
  Real code spends its time elsewhere, and only in loops over millions of results does the difference between 5 ns and 30 ns matter.
  FluentResults at 200 ns per chain is more than enough for most applications.
- **Equivalents are our choice**, as described above, and a library may have a faster way to do the same that we did not find.
- **No other runtime or platform.** Not Native AOT, not Linux or arm64. LanguageExt 5 (beta) is not included.
- **Delegates that capture variables allocate a closure** like in any C# code; use `static` lambdas on hot paths, as these benchmarks do.

Run them yourself (the whole suite takes about fifteen minutes; results differ by machine and runtime):

```shell
dotnet run -c Release --project benchmarks/Result.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Result.Benchmarks -- --filter "*SyncSuccess*"
```
