# Performance

[Back to the Result overview](../../src/Result/README.md)

These facts are checked by tests (`PerformanceTests`, `ExtensionReceiverTests`), not just stated:

- `Result` is exactly the size of one reference; `Result<T>` is `T` plus one reference.
- Creating results, `Map`, `Bind`, `Ensure`, `Check`, `Tap`, `MapError` and `Match` on the success path allocate **0 bytes** when the
  delegates are `static` lambdas (measured with `GC.GetAllocatedBytesForCurrentThread`).
- An already completed `Task` or `ValueTask` receiver takes a synchronous fast path without a state machine, and a synchronous receiver
  with a `Task`/`ValueTask` delegate hands back the task of the delegate for `Bind`, `Compensate` and `Match`.
- On an already completed `Task`, a step that leaves the result as it was hands back the task it received and allocates nothing: `Ensure` that
  passes or meets a failure, `Check`, `Tap`, `TapError`, `MapError`, `Recover` and `Compensate` on a result they do not change
  (`CompletedTaskReuseTests`). `Map` and `Bind` never do, because they can change the value.
- A ready-made error is one object: `Error.Create`, `Unavailable`, `Timeout` and `FromException` each allocate exactly one `Error`, and read the message
  of an exception once (`ErrorAllocationTests`).
- Receivers are passed **by value**, not by `in`, on purpose. `in` is faster when a result is read from a field, but a chain passes the
  result of the previous call (a temporary), whose address then has to be taken, which forces it into memory. Measured, a four-step
  chain was several times slower with `in`, and a chain of calls on a 72-byte `Result<T>` was no faster with it
  (`ParameterPassing` benchmark). `ExtensionReceiverTests` keeps it that way.
- Each public method checks its delegate and `Task` arguments for `null` up front (an inlined branch). On the async chains this cost
  about 0.5 ns per call (roughly 5-8 % of a 24 ns chain) when measured with and without the checks, in exchange for an
  `ArgumentNullException` at the call instead of a `NullReferenceException` later, or never, depending on the data.

## Benchmarks against the other libraries

`benchmarks/Result.Benchmarks` runs the same scenarios against eight other libraries, with this library as the baseline in every table: CSharpFunctionalExtensions
3.7.0, FluentResults 4.0.0, ErrorOr 2.1.1, Ardalis.Result 10.1.0, LanguageExt 4.4.9 (`Fin<A>`), Funcky 3.6.1 (`Result<T>`),
Optional 4.0.0 (`Option<T, TException>`) and OneOf 3.0.271 (a union, written as `OneOf<T, Error<string>>`). The libraries are described in the
[comparison](comparison.md). **Treat the numbers as indicative, not as a verdict.** The caveats are at the end of this section; read them
before you quote a cell.

Every scenario exists twice: with a **value type** (`int`) and with a **reference type** (a `Customer` class, from which the chain
takes the name, a `string`, and carries it on). The chains never allocate a new payload, so the allocation columns show what the result type
costs and not what the payload costs.

**How to read a cell:** `5.23 ns, 0 B (1.1×)` is the mean time per operation, the bytes allocated per operation, and the time relative to this
library (smaller than `1.0×` means faster than this library). The ratio is left out when this library's own time is below 0.5 ns, as a ratio
of such small numbers says nothing. The measurement setup: BenchmarkDotNet 0.15.8, .NET 10.0.12, an Intel Core Ultra 7 265H, Windows 11, the
in-process toolchain, 5 warm-up and 10 measured iterations of 100 ms per benchmark, one full run on 2026-10-19.

**What "nearest equivalent" means.** The chains are written the way a user of each library would write them, with the closest operations it has:

- A library without `Ensure` (FluentResults, Ardalis.Result, LanguageExt, Funcky, OneOf) does the check in a `Bind` step (or, for OneOf, in
  `Match`). OneOf has only `MapT0` and `Match`, so its chains are written with `Match`.
- A library without extension methods for `Task<Result<T>>` or `ValueTask<Result<T>>` is measured the way its user must write it: **await first,
  then run the synchronous chain** (LanguageExt, Funcky, Optional and OneOf for both; FluentResults, Ardalis.Result and ErrorOr for `ValueTask`). In the
  scenario with an asynchronous step after a synchronous result, every library except this one, CSharpFunctionalExtensions and ErrorOr is measured that way. The same style is shown for this library as an extra row, so the two ways can be compared.
- A library without a `Try` wrapper (ErrorOr, Ardalis.Result, OneOf, Funcky, Optional) is measured with the `try`/`catch` its user would write.
- Errors exist in advance (a static field or a literal) unless the scenario says it creates them. Delegates are `static` lambdas.

The code is in the repository. If you think a chain is written unfairly for a library, say so: it is a judgment, not a measurement.

### Synchronous chain, every step succeeds

Four steps: `Map`, `Ensure`, `Bind`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 4.27 ns, 0 B | 4.59 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 5.62 ns, 0 B (1.3×) | 8.38 ns, 0 B (1.8×) |
| FluentResults 4.0.0 | 179 ns, 1104 B (42×) | 194 ns, 1104 B (42×) |
| ErrorOr 2.1.1 | 5.75 ns, 0 B (1.3×) | 6.23 ns, 0 B (1.4×) |
| Ardalis.Result 10.1.0 | 22.7 ns, 256 B (5.3×) | 26.8 ns, 288 B (5.8×) |
| LanguageExt 4.4.9 `Fin<A>` | 16.2 ns, 0 B (3.8×) | 17.9 ns, 0 B (3.9×) |
| Funcky 3.6.1 `Result<T>` | 34.0 ns, 88 B (8.0×) | 31.3 ns, 88 B (6.8×) |
| Optional 4.0.0 `Option<T, TException>` | 9.07 ns, 24 B (2.1×) | 10.1 ns, 24 B (2.2×) |
| OneOf 3.0.271 | 6.02 ns, 0 B (1.4×) | 7.46 ns, 0 B (1.6×) |

### Synchronous chain, a step fails

The same chain, but the check fails, so the later steps are skipped and the error is matched.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 3.57 ns, 0 B | 3.90 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 10.9 ns, 0 B (3.1×) | 11.1 ns, 0 B (2.8×) |
| FluentResults 4.0.0 | 213 ns, 1200 B (60×) | 221 ns, 1200 B (57×) |
| ErrorOr 2.1.1 | 18.2 ns, 88 B (5.1×) | 19.1 ns, 88 B (4.9×) |
| Ardalis.Result 10.1.0 | 44.3 ns, 320 B (12×) | 43.1 ns, 352 B (11×) |
| LanguageExt 4.4.9 `Fin<A>` | 26.1 ns, 0 B (7.3×) | 24.4 ns, 0 B (6.3×) |
| Funcky 3.6.1 `Result<T>` | 36.5 ns, 88 B (10×) | 32.8 ns, 88 B (8.4×) |
| Optional 4.0.0 `Option<T, TException>` | 11.2 ns, 24 B (3.1×) | 11.1 ns, 24 B (2.9×) |
| OneOf 3.0.271 | 5.88 ns, 0 B (1.6×) | 5.85 ns, 0 B (1.5×) |

### Synchronous chain of eight steps

`Map`, `Ensure`, `Bind`, `Map`, `Ensure`, `Bind`, `Map`, `Match`: does the cost grow with the number of steps?

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 5.51 ns, 0 B | 6.67 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 8.73 ns, 0 B (1.6×) | 10.8 ns, 0 B (1.6×) |
| FluentResults 4.0.0 | 387 ns, 2256 B (70×) | 402 ns, 2256 B (60×) |
| ErrorOr 2.1.1 | 7.86 ns, 0 B (1.4×) | 7.99 ns, 0 B (1.2×) |
| Ardalis.Result 10.1.0 | 40.6 ns, 448 B (7.4×) | 46.5 ns, 504 B (7.0×) |
| LanguageExt 4.4.9 `Fin<A>` | 32.2 ns, 0 B (5.8×) | 33.5 ns, 0 B (5.0×) |
| Funcky 3.6.1 `Result<T>` | 76.0 ns, 264 B (14×) | 78.0 ns, 264 B (12×) |
| Optional 4.0.0 `Option<T, TException>` | 20.9 ns, 72 B (3.8×) | 25.3 ns, 72 B (3.8×) |
| OneOf 3.0.271 | 30.5 ns, 0 B (5.5×) | 32.3 ns, 0 B (4.8×) |

### Returning a result from a method

A method that is not inlined creates a successful result and returns it, and the caller reads the value. (Inlined, the compiler removes the
result type of the struct libraries altogether.)

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 0.99 ns, 0 B | 1.91 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 1.21 ns, 0 B (1.2×) | 1.40 ns, 0 B (0.7×) |
| FluentResults 4.0.0 | 24.9 ns, 160 B (25×) | 23.9 ns, 160 B (12×) |
| ErrorOr 2.1.1 | 1.14 ns, 0 B (1.2×) | 1.36 ns, 0 B (0.7×) |
| Ardalis.Result 10.1.0 | 6.20 ns, 64 B (6.2×) | 6.97 ns, 72 B (3.6×) |
| LanguageExt 4.4.9 `Fin<A>` | 1.34 ns, 0 B (1.3×) | 2.58 ns, 0 B (1.4×) |
| Funcky 3.6.1 `Result<T>` | 1.95 ns, 0 B (2.0×) | 2.48 ns, 0 B (1.3×) |
| Optional 4.0.0 `Option<T, TException>` | 1.15 ns, 0 B (1.2×) | 2.26 ns, 0 B (1.2×) |
| OneOf 3.0.271 | 1.46 ns, 0 B (1.5×) | 2.12 ns, 0 B (1.1×) |

> **Why this library is not the fastest for the `Customer`.** The struct libraries are within a nanosecond of each other. For the `int` this library is the fastest;
> for the `Customer` CSharpFunctionalExtensions (1.40 ns) and ErrorOr (1.36 ns) are about half a nanosecond ahead of its 1.91 ns. A result stores a single
> `null` for success here, as ErrorOr does, and is 16 bytes for either payload, so we did not find what the difference is, and a difference of this size
> moves from run to run.

### Creating a failure

A method that is not inlined creates a failed result with a new error and returns it; the caller asks whether it failed.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 10.3 ns, 96 B | 11.4 ns, 96 B |
| CSharpFunctionalExtensions 3.7.0 | 1.72 ns, 0 B (0.2×) | 1.83 ns, 0 B (0.2×) |
| FluentResults 4.0.0 | 48.3 ns, 360 B (4.7×) | 45.2 ns, 360 B (4.0×) |
| ErrorOr 2.1.1 | 8.36 ns, 88 B (0.8×) | 8.24 ns, 88 B (0.7×) |
| Ardalis.Result 10.1.0 | 9.53 ns, 96 B (0.9×) | 9.50 ns, 104 B (0.8×) |
| LanguageExt 4.4.9 `Fin<A>` | 6.23 ns, 48 B (0.6×) | 5.35 ns, 48 B (0.5×) |
| Funcky 3.6.1 `Result<T>` | 18 μs, 18922 B (1766×) | 19 μs, 18933 B (1665×) |
| Optional 4.0.0 `Option<T, TException>` | 1.29 ns, 0 B (0.1×) | 1.03 ns, 0 B (0.1×) |
| OneOf 3.0.271 | 1.01 ns, 0 B (0.1×) | 0.97 ns, 0 B (0.1×) |
| Wyzely, a rich error (kind, detail, context frame) | 38.2 ns, 320 B (3.7×) | n/a |

> **Why this library is not the fastest here.** An error is an object here: a plain one is 96 B and takes 8 to 11 ns to create, because it can carry a message
> template, details, a kind, a code, a cause and context. CSharpFunctionalExtensions, Optional and OneOf are faster because their error is a string literal or a
> struct, which costs nothing to create, and they cannot carry any of that. Among the libraries whose error is an object this library is on par (ErrorOr 88 B,
> Ardalis.Result 96 to 104 B), and LanguageExt (48 B) is smaller. Every factory builds exactly one object (`ErrorAllocationTests`). An error that is created once and
> reused, as in the chains above, costs nothing.

### A completed `Task<Result<T>>` as the receiver

The receiver is a task that is already completed and every delegate is synchronous: what does the asynchronous machinery cost?

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 21.9 ns, 160 B | 24.6 ns, 160 B |
| CSharpFunctionalExtensions 3.7.0 | 49.4 ns, 240 B (2.3×) | 57.6 ns, 264 B (2.3×) |
| FluentResults 4.0.0 | 192 ns, 1160 B (8.8×) | 205 ns, 1160 B (8.3×) |
| ErrorOr 2.1.1 | 41.8 ns, 240 B (1.9×) | 47.3 ns, 240 B (1.9×) |
| Ardalis.Result 10.1.0 | 49.3 ns, 408 B (2.2×) | 61.1 ns, 432 B (2.5×) |
| LanguageExt 4.4.9 `Fin<A>` | 24.4 ns, 0 B (1.1×) | 25.6 ns, 0 B (1.0×) |
| Funcky 3.6.1 `Result<T>` | 40.6 ns, 88 B (1.8×) | 42.3 ns, 88 B (1.7×) |
| Optional 4.0.0 `Option<T, TException>` | 17.4 ns, 24 B (0.8×) | 19.7 ns, 24 B (0.8×) |
| OneOf 3.0.271 | 12.0 ns, 0 B (0.5×) | 15.7 ns, 0 B (0.6×) |
| Wyzely, awaiting first and then running the synchronous chain | 11.1 ns, 0 B (0.5×) | 12.0 ns, 0 B (0.5×) |

> **Why this library is not the fastest here.** A method that returns a `Task<Result<T>>` has to allocate a task for every step that changes the result, so
> a chain of two such steps is 160 B whatever the library does: nothing below that is possible with this return type. LanguageExt, Optional, OneOf and Funcky are
> faster or allocate less only because they have no such methods, so their users await first, and so can you: the last row is as fast as theirs (11.1 ns against
> 12.0 ns for OneOf, 0 B). Among the libraries that do offer `Task` methods this one is the fastest (about twice as fast as CSharpFunctionalExtensions and
> ErrorOr) and allocates the least; steps that leave the result as it was hand back the task they received.

### A completed `ValueTask<Result<T>>` as the receiver

The same with a `ValueTask`, which exists so that work that completes synchronously allocates nothing.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 12.2 ns, 0 B | 12.8 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 84.4 ns, 0 B (6.9×) | 92.5 ns, 0 B (7.2×) |
| FluentResults 4.0.0 | 178 ns, 1056 B (15×) | 196 ns, 1056 B (15×) |
| ErrorOr 2.1.1 | 13.7 ns, 0 B (1.1×) | 14.8 ns, 0 B (1.2×) |
| Ardalis.Result 10.1.0 | 33.3 ns, 256 B (2.7×) | 38.3 ns, 288 B (3.0×) |
| LanguageExt 4.4.9 `Fin<A>` | 25.0 ns, 0 B (2.0×) | 27.3 ns, 0 B (2.1×) |
| Funcky 3.6.1 `Result<T>` | 38.5 ns, 88 B (3.1×) | 45.3 ns, 88 B (3.5×) |
| Optional 4.0.0 `Option<T, TException>` | 18.2 ns, 24 B (1.5×) | 21.4 ns, 24 B (1.7×) |
| OneOf 3.0.271 | 14.2 ns, 0 B (1.2×) | 16.6 ns, 0 B (1.3×) |
| Wyzely, awaiting first and then running the synchronous chain | 11.0 ns, 0 B (0.9×) | 12.1 ns, 0 B (0.9×) |

### A synchronous result followed by an asynchronous step

The receiver is a plain result, the first step is an asynchronous delegate that returns an already completed task, and a `Map` follows.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Result** | 13.8 ns, 80 B | 13.1 ns, 80 B |
| CSharpFunctionalExtensions 3.7.0 | 25.8 ns, 80 B (1.9×) | 27.6 ns, 80 B (2.1×) |
| FluentResults 4.0.0 | 48.2 ns, 256 B (3.5×) | 51.4 ns, 256 B (3.9×) |
| ErrorOr 2.1.1 | 31.6 ns, 160 B (2.3×) | 32.5 ns, 160 B (2.5×) |
| Ardalis.Result 10.1.0 | 7.97 ns, 0 B (0.6×) | 7.46 ns, 0 B (0.6×) |
| LanguageExt 4.4.9 `Fin<A>` | 9.05 ns, 0 B (0.7×) | 8.12 ns, 0 B (0.6×) |
| Funcky 3.6.1 `Result<T>` | 11.8 ns, 0 B (0.9×) | 15.7 ns, 0 B (1.2×) |
| Optional 4.0.0 `Option<T, TException>` | 13.4 ns, 24 B (1.0×) | 11.8 ns, 24 B (0.9×) |
| OneOf 3.0.271 | 8.97 ns, 0 B (0.6×) | 7.91 ns, 0 B (0.6×) |
| Wyzely, awaiting first and then running the synchronous chain | 9.51 ns, 0 B (0.7×) | 9.15 ns, 0 B (0.7×) |

> **Why this library is not the fastest here.** The `Map` that follows the asynchronous step has to return a task with a new value, which is the 80 B. The libraries
> in the table that are faster are measured with hand-written code (check the result, await the step), which has no task to create; the last row is the same
> thing written with this library (9.2 to 9.5 ns, 0 B), which is within 2 ns of the fastest of them (7.5 to 9.1 ns). Among the libraries that offer a `Bind`
> with an asynchronous delegate this one is the fastest.

### Code that can throw, turned into a result

`Result.Try` or its nearest equivalent, around a call that succeeds and around a call that throws (`int.Parse` for the `int`, a lookup that
throws for the `Customer`). A throw costs microseconds whatever the library, so the failure columns are dominated by the exception.

| Library | `int`, call succeeds | `int`, call throws | `Customer`, call succeeds | `Customer`, call throws |
|---|---|---|---|---|
| **Wyzely.Functional.Result** | 5.80 ns, 0 B | 1.7 μs, 560 B | 3.16 ns, 0 B | 1.6 μs, 552 B |
| CSharpFunctionalExtensions 3.7.0 | 12.8 ns, 0 B (2.2×) | 1.6 μs, 464 B (0.9×) | 10.9 ns, 0 B (3.5×) | 1.6 μs, 456 B (0.9×) |
| FluentResults 4.0.0 | 40.0 ns, 208 B (6.9×) | 1.8 μs, 1080 B (1.1×) | 36.4 ns, 208 B (12×) | 1.9 μs, 1072 B (1.2×) |
| ErrorOr 2.1.1 | 7.56 ns, 0 B (1.3×) | 1.6 μs, 552 B (0.9×) | 4.24 ns, 0 B (1.3×) | 1.5 μs, 544 B (0.9×) |
| Ardalis.Result 10.1.0 | 11.1 ns, 64 B (1.9×) | 1.6 μs, 560 B (0.9×) | 8.58 ns, 72 B (2.7×) | 1.5 μs, 560 B (0.9×) |
| LanguageExt 4.4.9 `Fin<A>` | 33.3 ns, 200 B (5.7×) | 1.7 μs, 664 B (1.0×) | 28.6 ns, 208 B (9.0×) | 1.8 μs, 704 B (1.1×) |
| Funcky 3.6.1 `Result<T>` | 8.90 ns, 0 B (1.5×) | 5.3 μs, 3529 B (3.1×) | 5.09 ns, 0 B (1.6×) | 5.1 μs, 3985 B (3.1×) |
| Optional 4.0.0 `Option<T, TException>` | 7.65 ns, 0 B (1.3×) | 1.5 μs, 464 B (0.9×) | 4.50 ns, 0 B (1.4×) | 1.4 μs, 328 B (0.8×) |
| OneOf 3.0.271 | 7.75 ns, 0 B (1.3×) | 1.6 μs, 464 B (0.9×) | 4.80 ns, 0 B (1.5×) | 1.5 μs, 456 B (0.9×) |

> **Why this library is not the fastest on the failure columns.** A throw costs about 1.5 μs whatever the library does, and the columns differ by less than the
> run-to-run noise of an exception (1.2 to 1.8 μs). This library allocates 96 B more than a hand-written `try`/`catch` that returns a string or a struct, and that
> is the `Error` object. On the success columns it is the fastest. Funcky is three times slower on the failure path (about 5 μs).

### What the numbers say

- **Synchronous chains: this library is the fastest, with ErrorOr, OneOf and CSharpFunctionalExtensions close behind, and none of them allocates.** The four-step chain
  took 4.3 ns here and 5.6 to 6.0 ns in those three for an `int` (4.6 ns against 6.2 to 8.4 ns for a `Customer`); differences of this size can change order from
  run to run. The others cost more: Optional 2 times (and 24 B), LanguageExt 4 times, Ardalis.Result 5 to 6 times (it is a class),
  Funcky 7 to 8 times (88 B) and FluentResults 42 times (1,104 B). The eight-step chain shows the same picture.
- **The failure path is cheap here because the error already exists.** With a ready-made error this library took about 4 ns and allocated nothing.
  OneOf took 5.9 ns (also without allocating), CSharpFunctionalExtensions and Optional about three times as long, and ErrorOr 18 to 19 ns and 88 B (its list
  of errors), Ardalis.Result 320 to 352 B, FluentResults 1,200 B.
- **Creating an error is a cost of its own, and this library is not the cheapest at it** (see the note under the table). A plain `Error` is one object, 96 B and
  8 to 11 ns, and a rich one with a detail and a context frame is 320 B and about 36 ns. Funcky's `Result<T>.Error(exception)` took about 19 μs and
  19 KB; we did not investigate why.
- **Returning a result from a method costs 1 to 3 ns for every struct library, and the differences are a few tenths of a nanosecond.** FluentResults and
  Ardalis.Result allocate (160 B and 64 to 72 B) and take 6 to 25 ns.
- **Chaining on a completed `Task` allocates, for every library that offers it, and awaiting first is cheaper.** A step that changes the result
  creates a new task, and the chain here (`Map`, `Ensure`, `Bind`) took 160 B and 22 to 25 ns: the `Ensure` step hands back the task it received when the result is
  left as it was, as do `Check`, `Tap`, `TapError`, `MapError`, `Recover` and `Compensate`, which saved 80 B and about a quarter of the time per such step. The
  other libraries with `Task` methods took 240 to 264 B (CSharpFunctionalExtensions, ErrorOr), 408 to 432 B (Ardalis.Result) and 1,160 B (FluentResults),
  and two to nine times as long. Awaiting the task first and then running the synchronous chain allocated nothing and took 11 to 12 ns, twice as fast (see the
  extra row). **If you can await, await, and chain on the result.** The `Task` extension methods are for when you want the chain to read as one expression,
  or when the result has to stay in a task.
- **`ValueTask`: this library is the cheapest of the two that offer extension methods for it.** With a completed `ValueTask`, this library
  allocated nothing and took 12 to 13 ns. CSharpFunctionalExtensions, the only other one with `ValueTask` extension methods, took 84 to 93 ns, seven
  times as long, also without allocating. For the libraries without them, await first and a synchronous chain took 14 to 45 ns, and
  FluentResults and Ardalis.Result allocated (1,056 B and 256 to 288 B).
- **A synchronous result followed by an asynchronous step** allocated 80 B in this library and CSharpFunctionalExtensions (the task of the `Map`),
  and nothing when the code checks the result and awaits by hand; the by-hand version was about 30 % faster in this library's case.
- **`Try` on the success path** took 3 to 6 ns and allocated nothing in this library, and 4 to 9 ns in Funcky, Optional, OneOf and ErrorOr
  (hand-written `try`/`catch`, so the cost is the `try` region). CSharpFunctionalExtensions took 11 to 13 ns, and FluentResults and
  LanguageExt 29 to 40 ns and about 200 B. **On the failure path every library took 1.4 to 1.9 μs, which is the cost of the exception**, except
  Funcky at about 5 μs and 3.5 to 4 KB.
- **Value type and reference type behave alike.** The struct libraries allocate nothing in either; the class-based libraries allocate in both.
  Where a reference payload costs more, it is the larger struct (24 B instead of 16 B in most libraries), not an allocation.

### Caveats

- **One machine, one configuration.** The numbers are from one Windows machine and .NET 10. A second full run of the same code gave a median
  difference of 4 % per benchmark (97 compared, in the scenarios whose code did not change between the runs), with the worst at 34 % and two of them above 25 %; the ratios of sub-10 ns benchmarks are the
  least stable. The runtime's tiered profile-guided optimization changes how well it inlines delegate calls, and earlier runs of this suite
  with other settings ranked the synchronous chains differently. Do not read a rank from a 10 % difference.
- **The scenarios are small on purpose.** They isolate the result type, with `static` lambdas and no I/O. Real code spends its time elsewhere,
  and only in loops over millions of results does the difference between 5 ns and 30 ns matter. FluentResults at 150 ns per chain is more than
  enough for most applications.
- **Equivalents are our choice**, as described above, and a library may have a faster way to do the same that we did not find.
- **No other runtime or platform.** Not the `netstandard2.0` builds, not Native AOT, not Linux or arm64. LanguageExt 5 (beta) is not included.
- **Delegates that capture variables allocate a closure** like in any C# code; use `static` lambdas on hot paths, as these benchmarks do.

Run them yourself (the whole suite takes about ten minutes; results differ by machine and runtime):

```shell
dotnet run -c Release --project benchmarks/Result.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Result.Benchmarks -- --filter "*SyncSuccess*"
```
