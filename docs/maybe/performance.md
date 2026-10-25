# Performance

[Back to the Maybe overview](../../src/Maybe/README.md)

These facts are checked by tests (`PerformanceTests` in `test/Maybe.Tests`), not just stated:

- `Maybe<int>` is **8 bytes**: the `int` and the flag.
- `Maybe<string>` is **two references wide**: the reference and the flag, padded to the size of a reference.
- A **synchronous chain that succeeds allocates 0 bytes**. The tested chain is `Some`, `Map`, `Filter`, `Bind` and `Match`, with `static`
  lambdas, measured with `GC.GetAllocatedBytesForCurrentThread` over 1000 iterations after a warm-up.
- A **synchronous chain that ends in none allocates 0 bytes** either. The tested chain is `Some`, `Filter` (which turns it into none),
  `Map` and `ValueOr`.

- A **`Task` chain over a reference type that stays none allocates 0 bytes.** Two tests cover it: `Map`, `Filter` and `Bind` on an already completed
  `Task<Maybe<string>>` that holds none, and `Bind` of a synchronous none with a `Task` delegate. The generated fast paths return one cached
  completed task per type for none (`MaybeTasks<T>.None`, internal). Before the cache, `Task.FromResult(default)` of a `Maybe<string>` allocated
  80 bytes (measured with `GC.GetAllocatedBytesForCurrentThread`); for `Maybe<int>` the runtime already caches it, so there was never anything to fix.

- On an already completed `Task`, a step that leaves the maybe as it was hands back the task it received and allocates nothing: `Filter` that
  keeps the value or meets none, `Tap`, `TapNone` and `Or` (`CompletedTaskReuseTests`). `Map` and `Bind` never do, because they can change the value.

Limits of what is claimed:

- The tests run on `net8.0`, `net10.0` and `net11.0`; the file is compiled for .NET 8 and later only. The `netstandard2.0` build is not covered by them.
- The size claims are for the layout on a 64-bit runtime as the tests ran it; the test for a reference compares with `2 * nint.Size`.
- Delegates that capture variables allocate a closure like in any C# code; use `static` lambdas on hot paths, as the tested chains do.
- Asynchronous chains allocate what `Task` and `ValueTask` allocate. **We make no allocation claim for them**, apart from the none case above, and none for the
  `Task`/`ValueTask` fast paths beyond what the code does (an already completed receiver is read without a state machine).
- The none cache is covered for the two cases named above only; the other generated variants use it but have no test of their own.
- **Timings are measurements, not guarantees.** They are in the [benchmarks](#benchmarks-against-the-other-libraries) below, with their caveats; only the size
  and allocation claims above are tests.

## Benchmarks against the other libraries

`benchmarks/Maybe.Benchmarks` runs the same scenarios against five other libraries, with this library as the baseline in every table:
CSharpFunctionalExtensions 3.7.0 (`Maybe<T>`), LanguageExt 4.4.9 (`Option<A>`), Funcky 3.6.1 (`Option<T>`), Optional 4.0.0 (`Option<T>`) and
OneOf 3.0.271 (a union, written as `OneOf<T, None>`). The libraries are described in the [comparison](comparison.md). **Treat the numbers as
indicative, not as a verdict.** The caveats are at the end of this section.

Every scenario exists twice: with a **value type** (`int`) and with a **reference type** (a `Customer` class, from which the chain takes the
name, a `string`, and carries it on). The chains never allocate a new payload, so the allocation columns show what the maybe type costs and
not what the payload costs.

**How to read a cell:** `4.53 ns, 0 B (1.2×)` is the mean time per operation, the bytes allocated per operation, and the time relative to this
library (smaller than `1.0×` means faster than this library). The ratio is left out when this library's own time is below 0.5 ns. The setup:
BenchmarkDotNet 0.15.8, .NET 10.0.13, an Intel Core Ultra 7 265H, Windows 11, the in-process toolchain, 5 warm-up and 10 measured iterations
of 100 ms per benchmark, one full run on 2026-10-25.

**What "nearest equivalent" means.** The chains are written the way a user of each library would write them:

- `Filter` is `Where` in CSharpFunctionalExtensions, Funcky and the LINQ-style names, `Bind` is `SelectMany` or `FlatMap`, and Funcky puts the
  none branch first in `Match`. OneOf has only `MapT0` and `Match`, so its chains are written with `Match`.
- A library without extension methods for `Task<Maybe<T>>` or `ValueTask<Maybe<T>>` is measured the way its user must write it: **await first,
  then run the synchronous chain** (LanguageExt, whose asynchronous work goes through a separate `OptionAsync<A>` type that is not measured here,
  Funcky, Optional and OneOf). The same style is shown for this library as an extra row, so the two ways can be compared. In the scenario with an
  asynchronous step after a synchronous maybe, every library except this one and CSharpFunctionalExtensions is measured that way.
- Delegates are `static` lambdas.

The code is in the repository. If you think a chain is written unfairly for a library, say so: it is a judgment, not a measurement.

### Synchronous chain, every step keeps its value

Four steps: `Map`, `Filter`, `Bind`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 4.08 ns, 0 B | 5.93 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 5.32 ns, 0 B (1.3×) | 12.7 ns, 0 B (2.1×) |
| LanguageExt 4.4.9 `Option<A>` | 5.51 ns, 0 B (1.3×) | 11.0 ns, 0 B (1.9×) |
| Funcky 3.6.1 `Option<T>` | 13.6 ns, 24 B (3.3×) | 25.4 ns, 24 B (4.3×) |
| Optional 4.0.0 `Option<T>` | 16.6 ns, 24 B (4.1×) | 10.6 ns, 24 B (1.8×) |
| OneOf 3.0.271 `OneOf<T, None>` | 5.96 ns, 0 B (1.5×) | 6.67 ns, 0 B (1.1×) |

### Synchronous chain, the filter rejects the value

The same chain, but the filter rejects the value, so the later steps are skipped and the none branch of `Match` runs.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 4.04 ns, 0 B | 4.38 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 5.12 ns, 0 B (1.3×) | 5.49 ns, 0 B (1.3×) |
| LanguageExt 4.4.9 `Option<A>` | 5.43 ns, 0 B (1.3×) | 4.05 ns, 0 B (0.9×) |
| Funcky 3.6.1 `Option<T>` | 16.7 ns, 24 B (4.1×) | 17.5 ns, 24 B (4.0×) |
| Optional 4.0.0 `Option<T>` | 14.2 ns, 24 B (3.5×) | 10.8 ns, 24 B (2.5×) |
| OneOf 3.0.271 `OneOf<T, None>` | 7.12 ns, 0 B (1.8×) | 7.49 ns, 0 B (1.7×) |

### Synchronous chain of eight steps

`Map`, `Filter`, `Bind`, `Map`, `Filter`, `Bind`, `Map`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 6.24 ns, 0 B | 10.1 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 8.99 ns, 0 B (1.4×) | 24.0 ns, 0 B (2.4×) |
| LanguageExt 4.4.9 `Option<A>` | 8.36 ns, 0 B (1.3×) | 24.5 ns, 0 B (2.4×) |
| Funcky 3.6.1 `Option<T>` | 34.0 ns, 72 B (5.5×) | 68.1 ns, 72 B (6.8×) |
| Optional 4.0.0 `Option<T>` | 43.9 ns, 72 B (7.0×) | 32.8 ns, 72 B (3.3×) |
| OneOf 3.0.271 `OneOf<T, None>` | 27.1 ns, 0 B (4.3×) | 48.0 ns, 0 B (4.8×) |

### Returning a maybe from a method

A method that is not inlined creates a maybe that holds a value and returns it, and the caller reads the value. (Inlined, the compiler
removes the maybe type of the struct libraries altogether.)

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 1.10 ns, 0 B | 1.43 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 0.910 ns, 0 B (0.8×) | 1.78 ns, 0 B (1.2×) |
| LanguageExt 4.4.9 `Option<A>` | 1.19 ns, 0 B (1.1×) | 1.17 ns, 0 B (0.8×) |
| Funcky 3.6.1 `Option<T>` | 1.23 ns, 0 B (1.1×) | 2.22 ns, 0 B (1.6×) |
| Optional 4.0.0 `Option<T>` | 0.938 ns, 0 B (0.9×) | 2.80 ns, 0 B (2.0×) |
| OneOf 3.0.271 `OneOf<T, None>` | 1.00 ns, 0 B (0.9×) | 1.83 ns, 0 B (1.3×) |

> **Why this library is not the fastest here.** For the `int` CSharpFunctionalExtensions (0.91 ns) and Optional (0.94 ns) are 0.2 ns ahead of this library (1.10 ns): within
> noise. For the `Customer` LanguageExt (1.17 ns) is ahead of its 1.43 ns; the others are between 1.8 and 2.8 ns.

### From a value that may be missing

An `int?` or a `string` that may be `null` becomes a maybe, which is then read. OneOf has no counterpart.

| Library | `int?` with a value | `int?` that is null | `string` with a value | `string` that is null |
|---|---|---|---|---|
| **Wyzely.Functional.Maybe** | 0.432 ns, 0 B | 0.254 ns, 0 B | 1.38 ns, 0 B | 0.660 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 2.19 ns, 0 B | 0.526 ns, 0 B | 2.43 ns, 0 B (1.8×) | 0.686 ns, 0 B (1.0×) |
| LanguageExt 4.4.9 `Option<A>` | 2.56 ns, 0 B | 1.92 ns, 0 B | 2.51 ns, 0 B (1.8×) | 1.73 ns, 0 B (2.6×) |
| Funcky 3.6.1 `Option<T>` | 3.19 ns, 0 B | 1.55 ns, 0 B | 1.85 ns, 0 B (1.3×) | 1.29 ns, 0 B (1.9×) |
| Optional 4.0.0 `Option<T>` | 2.57 ns, 0 B | 0.987 ns, 0 B | 1.73 ns, 0 B (1.3×) | 0.899 ns, 0 B (1.4×) |

> **Why this library is not the fastest in every column.** Where a value is missing the work is a single test, and the times are 0.3 to 0.7 ns for this library and
> CSharpFunctionalExtensions: that is the loop of the benchmark itself, and a difference of 0.2 ns there says nothing. With a value this library is the fastest.

### A completed `Task<Maybe<T>>` as the receiver

The receiver is a task that is already completed and every delegate is synchronous.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 35.4 ns, 144 B | 22.2 ns, 160 B |
| CSharpFunctionalExtensions 3.7.0 | 44.2 ns, 216 B (1.3×) | 43.4 ns, 240 B (2.0×) |
| LanguageExt 4.4.9 `Option<A>` | 11.8 ns, 0 B (0.3×) | 17.4 ns, 0 B (0.8×) |
| Funcky 3.6.1 `Option<T>` | 22.7 ns, 24 B (0.6×) | 33.5 ns, 24 B (1.5×) |
| Optional 4.0.0 `Option<T>` | 27.4 ns, 24 B (0.8×) | 17.6 ns, 24 B (0.8×) |
| OneOf 3.0.271 `OneOf<T, None>` | 12.5 ns, 0 B (0.4×) | 15.7 ns, 0 B (0.7×) |
| Wyzely, awaiting first and then running the synchronous chain | 10.2 ns, 0 B (0.3×) | 11.1 ns, 0 B (0.5×) |

> **Why this library is not the fastest here.** A method that returns a `Task<Maybe<T>>` has to allocate a task for every step that changes the maybe, so a chain
> of two such steps is 144 to 160 B whatever the library does. LanguageExt, Optional, OneOf and Funcky are faster or allocate less only because they have no
> such methods, so their users await first, and so can you: the last row is as fast as theirs (10 to 11 ns, 0 B). Among the libraries that offer `Task`
> methods this one is the fastest and allocates the least. The `int` chain is slower than the `Customer` chain (35 against 22 ns) in every run, with fewer bytes
> allocated; a single step costs about the same for both, and we did not find the cause.

### A completed `ValueTask<Maybe<T>>` as the receiver

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 18.4 ns, 0 B | 15.5 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 64.4 ns, 0 B (3.5×) | 86.7 ns, 0 B (5.6×) |
| LanguageExt 4.4.9 `Option<A>` | 15.4 ns, 0 B (0.8×) | 24.8 ns, 0 B (1.6×) |
| Funcky 3.6.1 `Option<T>` | 27.4 ns, 24 B (1.5×) | 38.6 ns, 24 B (2.5×) |
| Optional 4.0.0 `Option<T>` | 26.9 ns, 24 B (1.5×) | 24.8 ns, 24 B (1.6×) |
| OneOf 3.0.271 `OneOf<T, None>` | 15.9 ns, 0 B (0.9×) | 15.0 ns, 0 B (1.0×) |
| Wyzely, awaiting first and then running the synchronous chain | 14.9 ns, 0 B (0.8×) | 17.1 ns, 0 B (1.1×) |

> **Why this library is not the fastest here.** For the `int` LanguageExt (which awaits first) took 15.4 ns against 18.4 ns here, and OneOf 15.9 ns. Most of each number is the
> `await` of the benchmark method itself (about 10 ns), and every step of a chain on a `ValueTask` costs about 2 ns more than the same step on a plain maybe, as
> a `ValueTask<Maybe<int>>` is a 24-byte struct that is passed along and created again by every step. Awaiting first and using the synchronous chain is the way to
> avoid that, and the last row is as fast as the fastest. Among the libraries that offer `ValueTask` methods this one is the fastest.

### A synchronous maybe followed by an asynchronous step

The receiver is a plain maybe, the first step is an asynchronous delegate that returns an already completed task, and a `Map` follows.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 15.3 ns, 72 B | 15.7 ns, 72 B |
| CSharpFunctionalExtensions 3.7.0 | 20.0 ns, 72 B (1.3×) | 23.1 ns, 72 B (1.5×) |
| LanguageExt 4.4.9 `Option<A>` | 9.56 ns, 0 B (0.6×) | 8.89 ns, 0 B (0.6×) |
| Funcky 3.6.1 `Option<T>` | 19.6 ns, 24 B (1.3×) | 9.16 ns, 0 B (0.6×) |
| Optional 4.0.0 `Option<T>` | 12.7 ns, 24 B (0.8×) | 12.2 ns, 24 B (0.8×) |
| OneOf 3.0.271 `OneOf<T, None>` | 7.85 ns, 0 B (0.5×) | 8.73 ns, 0 B (0.6×) |
| Wyzely, awaiting first and then running the synchronous chain | 8.52 ns, 0 B (0.6×) | 8.56 ns, 0 B (0.5×) |

> **Why this library is not the fastest here.** The `Map` that follows the asynchronous step has to return a task with a new value, which is the 72 B. The libraries in the
> table that are faster are measured with hand-written code (check the maybe, await the step), which has no task to create; the last row is the same thing
> written with this library (8.5 to 8.6 ns, 0 B), and that is within 1 ns of the fastest of them (7.9 to 8.7 ns). Among the libraries that offer a `Bind` with an
> asynchronous delegate this one is the fastest.

### Asking whether the value satisfies a condition

`Any` or its nearest equivalent on a maybe that is asked once. LanguageExt and Optional have `Exists`. CSharpFunctionalExtensions and Funcky have no such method: `Where` followed by
`HasValue` or `Match` is measured. OneOf has only `Match`. Every library gives the same answer.

| Library | `int`, has a value | `int`, none | `string`, has a value | `string`, none |
|---|---|---|---|---|
| **Wyzely.Functional.Maybe** | 0.527 ns, 0 B | 0.138 ns, 0 B | 0.729 ns, 0 B | 0.116 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 1.27 ns, 0 B (2.4×) | 0.225 ns, 0 B | 1.26 ns, 0 B (1.7×) | 0.625 ns, 0 B |
| LanguageExt 4.4.9 `Option<A>` | 0.843 ns, 0 B (1.6×) | 0.492 ns, 0 B | 1.43 ns, 0 B (2.0×) | 0.282 ns, 0 B |
| Funcky 3.6.1 `Option<T>` | 2.47 ns, 0 B (4.7×) | 0.418 ns, 0 B | 2.88 ns, 0 B (4.0×) | 0.743 ns, 0 B |
| Optional 4.0.0 `Option<T>` | 1.00 ns, 0 B (1.9×) | 0.316 ns, 0 B | 0.784 ns, 0 B (1.1×) | 0.272 ns, 0 B |
| OneOf 3.0.271 `OneOf<T, None>` | 0.695 ns, 0 B (1.3×) | 0.0204 ns, 0 B | 0.423 ns, 0 B (0.6×) | 0.384 ns, 0 B |

> **Do not read a rank from this table.** Every cell is below 3 ns, and half of them below 0.5 ns, which is the loop of the benchmark itself: the JIT inlines the whole call and
> the none columns come close to the useful resolution of the timer. What can be read is that nothing allocates, and that the libraries that need two steps for the question
> (CSharpFunctionalExtensions, Funcky) took 1.3 to 2.9 ns where one step took 0.4 to 1.4 ns.

### Collecting the values of a sequence of maybes

Eight maybes, every second one is none; the values are collected and added up. CSharpFunctionalExtensions has `Choose`, LanguageExt `Somes`, Optional `Values`. Funcky has none, so every
option is turned into a sequence of one and joined with `SelectMany`; OneOf collects with a loop. Every library computes the same sum.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 30.4 ns, 88 B | 46.2 ns, 88 B |
| CSharpFunctionalExtensions 3.7.0 | 32.7 ns, 88 B (1.1×) | 37.7 ns, 88 B (0.8×) |
| LanguageExt 4.4.9 `Option<A>` | 41.3 ns, 88 B (1.4×) | 44.1 ns, 88 B (1.0×) |
| Funcky 3.6.1 `Option<T>` | 125 ns, 448 B (4.1×) | 171 ns, 448 B (3.7×) |
| Optional 4.0.0 `Option<T>` | 29.0 ns, 88 B (1.0×) | 51.0 ns, 88 B (1.1×) |
| OneOf 3.0.271 `OneOf<T, None>` | 37.8 ns, 128 B (1.2×) | 49.3 ns, 160 B (1.1×) |

> **No meaningful difference, except Funcky.** The libraries are within 30 to 51 ns of each other, and the 88 B are the list or sequence that holds the four values. Funcky is about four
> times slower and allocates 448 B, which is what its workaround costs; it does not say how Funcky's own methods would perform. For the `Customer`
> CSharpFunctionalExtensions (37.7 ns) and LanguageExt are ahead of this library (46.2 ns), and Optional is not; we did not investigate the gap, and the caveats below say how much runs differ.

### What the numbers say

- **On synchronous chains this library is the fastest, and LanguageExt, CSharpFunctionalExtensions and OneOf are close for an `int`.** The four-step chain
  took 4.1 ns here and 5.3 to 6.0 ns for them, without allocating; the differences are inside the run-to-run noise. With a reference type
  CSharpFunctionalExtensions and LanguageExt took about 2 times as long (11 to 13 ns) and OneOf 1.1 times. Funcky (14 to 25 ns) and Optional (11 to 17 ns)
  allocated 24 B per chain and took 2 to 4 times as long. The eight-step chain shows the same picture.
- **When the filter rejects, this library, CSharpFunctionalExtensions and LanguageExt are equal** at 4 to 5.5 ns, OneOf took 7 ns, and Funcky and Optional 11 to 18 ns
  with 24 B.
- **Making a maybe from a nullable** is 0.4 ns (an `int?`) and 1.4 ns (a `string`) for this library with a value, and the others took 1.7 to 3.2 ns. With
  nothing in it every library was between 0.3 and 1.9 ns, which is the loop of the benchmark.
- **Chaining on a completed `Task` allocates, and awaiting first is cheaper.** A `Task<Maybe<T>>` chain in this library allocated 144 to 160 B and took
  22 to 35 ns; the `Filter` step hands back the task it received when the maybe is left as it was, as do `Tap`, `TapNone` and `Or`. Awaiting the task and then
  running the synchronous chain allocated nothing and took 10 to 11 ns. The same holds for the other libraries: their await-first chains took 12 to 34 ns.
  **If you can await, await, and chain on the result.** The `Task` extension methods are for when you want the chain to read as one expression, or when the
  maybe has to stay in a task. CSharpFunctionalExtensions allocated 216 to 240 B.
- **`ValueTask`:** this library allocated nothing and took 15 to 18 ns, where CSharpFunctionalExtensions, the only other one with `ValueTask`
  extension methods, took 64 to 87 ns (3.5 to 5.6 times as long). Awaiting first was as cheap as the extension methods.
- **A synchronous maybe followed by an asynchronous step** allocated 72 B in this library and CSharpFunctionalExtensions; checking the maybe and
  awaiting by hand allocated nothing and was about 45 % faster.
- **Asking a maybe a question and collecting the values of several** (`Any`, `Choose`) show no difference that is worth a decision: the questions took under 3 ns for every library,
  and `Choose` 29 to 51 ns for every library except Funcky, which has no `Choose` and took about four times as long with the workaround we measured.
- **Value type and reference type behave alike.** The struct libraries allocate nothing in either, apart from Funcky and Optional. The reference
  versions are slower in CSharpFunctionalExtensions and LanguageExt, which we did not investigate.

### Caveats

- **One machine, one configuration.** The numbers are from one Windows machine and .NET 10. A second full run of the same code gave a median
  difference of 5 % per benchmark (81 compared, in the scenarios whose code did not change between the runs), with the worst at 25 %. The
  runtime's tiered profile-guided optimization changes how well it inlines delegate calls. Do not read a rank from a 10 % difference.
- **The scenarios are small on purpose.** They isolate the maybe type, with `static` lambdas and no I/O. Real code spends its time elsewhere.
- **Equivalents are our choice**, and a library may have a faster way to do the same that we did not find.
- **No other runtime or platform.** Not the `netstandard2.0` builds, not Native AOT, not Linux or arm64. LanguageExt 5 (beta) is not included.
- **Delegates that capture variables allocate a closure** like in any C# code; use `static` lambdas on hot paths, as these benchmarks do.

Run them yourself (the whole suite takes about nine minutes; results differ by machine and runtime):

```shell
dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*SyncSuccess*"
```
