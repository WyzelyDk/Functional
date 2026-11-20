# Performance

[Back to the Maybe overview](../../src/Maybe/README.md)

These facts are checked by tests (`PerformanceTests` in `test/Maybe.Tests`), not just stated:

- `Maybe<int>` is **8 bytes**: the `int` and the flag.
- `Maybe<string>` is **two references wide**: the reference and the flag, padded to the size of a reference.
- A **synchronous chain that succeeds allocates 0 bytes**. The tested chain is `Some`, `Map`, `Filter`, `Bind` and `Match`, with `static` lambdas,
  measured with `GC.GetAllocatedBytesForCurrentThread` over 1000 iterations after a warm-up.
- A **synchronous chain that ends in none allocates 0 bytes** either. The tested chain is `Some`, `Filter` (which turns it into none),
  `Map` and `ValueOr`.

- A **`Task` chain over a reference type that stays none allocates 0 bytes.** Two tests cover it: `Map`, `Filter` and `Bind` on an already completed
  `Task<Maybe<string>>` that holds none, and `Bind` of a synchronous none with a `Task` delegate. The generated fast paths return one cached
  completed task per type for none (`MaybeTasks<T>.None`, internal). Before the cache, `Task.FromResult(default)` of a `Maybe<string>` allocated
  80 bytes (measured with `GC.GetAllocatedBytesForCurrentThread`); for `Maybe<int>` the runtime already caches it, so there was never anything to fix.

- On an already completed `Task`, a step that leaves the maybe as it was hands back the task it received and allocates nothing: `Filter` that
  keeps the value or meets none, `Tap`, `TapNone` and `OrElse` (`CompletedTaskReuseTests`). `Map` and `Bind` never do, because they can change the value.

Limits of what is claimed:

- The tests run on `net10.0` and `net11.0`.
- The size claims are for the layout on a 64-bit runtime as the tests ran it; the test for a reference compares with `2 * nint.Size`.
- Delegates that capture variables allocate a closure like in any C# code; use `static` lambdas on hot paths, as the tested chains do.
- Asynchronous chains allocate what `Task` and `ValueTask` allocate. **We make no allocation claim for them**, apart from the none case above,
  and none for the `Task`/`ValueTask` fast paths beyond what the code does (an already completed receiver is read without a state machine).
- The none cache is covered for the two cases named above only; the other generated variants use it but have no test of their own.
- **Timings are measurements, not guarantees.**
  They are in the [benchmarks](#benchmarks-against-the-other-libraries) below, with their caveats; only the size and allocation claims above are tests.

## Benchmarks against the other libraries

`benchmarks/Maybe.Benchmarks` runs the same scenarios against five other libraries, with this library as the baseline in every table:
CSharpFunctionalExtensions 3.7.0 (`Maybe<T>`), LanguageExt 4.4.9 (`Option<A>`), Funcky 3.8.0 (`Option<T>`),
Optional 4.0.0 (`Option<T>`) and OneOf 3.0.271 (a union, written as `OneOf<T, None>`). The libraries are described in the [comparison](comparison.md).
**Treat the numbers as indicative, not as a verdict.** The caveats are at the end of this section.

Every scenario exists twice: with a **value type** (`int`) and with a **reference type** (a `Customer` class, from which the chain takes the name, a `string`,
and carries it on). The chains never allocate a new payload, so the allocation columns show what the maybe type costs and not what the payload costs.

**How to read a cell:** `5.71 ns, 0 B (1.3×)` is the mean time per operation, the bytes allocated per operation,
and the time relative to this library (smaller than `1.0×` means faster than this library). The ratio is left out when this library's own time is below 0.5 ns.
The setup: BenchmarkDotNet 0.15.8, .NET 10.0.15, an Intel Core Ultra 7 265H, Windows 11, the in-process toolchain,
5 warm-up and 10 measured iterations of 100 ms per benchmark, one full run on 2026-11-20.

**What "nearest equivalent" means.** The chains are written the way a user of each library would write them:

- `Filter` is `Where` in CSharpFunctionalExtensions, Funcky and the LINQ-style names, `Bind` is `SelectMany` or `FlatMap`,
  and Funcky puts the none branch first in `Match`. OneOf has only `MapT0` and `Match`, so its chains are written with `Match`.
- A library without extension methods for `Task<Maybe<T>>` or `ValueTask<Maybe<T>>` is measured the way its user must write it: **await first,
  then run the synchronous chain** (LanguageExt, whose asynchronous work goes through a separate `OptionAsync<A>` type that is not measured here,
  Funcky, Optional and OneOf). The same style is shown for this library as an extra row, so the two ways can be compared.
  In the scenario with an asynchronous step after a synchronous maybe, every library except this one and CSharpFunctionalExtensions is measured that way.
- Delegates are `static` lambdas.

The code is in the repository. If you think a chain is written unfairly for a library, say so: it is a judgment, not a measurement.

### Synchronous chain, every step keeps its value

Four steps: `Map`, `Filter`, `Bind`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 4.49 ns, 0 B | 5.65 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 5.71 ns, 0 B (1.3×) | 12.2 ns, 0 B (2.2×) |
| LanguageExt 4.4.9 `Option<A>` | 4.95 ns, 0 B (1.1×) | 11.2 ns, 0 B (2.0×) |
| Funcky 3.8.0 `Option<T>` | 15.2 ns, 24 B (3.4×) | 28.4 ns, 24 B (5.0×) |
| Optional 4.0.0 `Option<T>` | 20.7 ns, 24 B (4.6×) | 11.0 ns, 24 B (1.9×) |
| OneOf 3.0.271 `OneOf<T, None>` | 5.74 ns, 0 B (1.3×) | 6.70 ns, 0 B (1.2×) |

### Synchronous chain, the filter rejects the value

The same chain, but the filter rejects the value, so the later steps are skipped and the none branch of `Match` runs.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 3.86 ns, 0 B | 4.43 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 4.64 ns, 0 B (1.2×) | 4.61 ns, 0 B (1.0×) |
| LanguageExt 4.4.9 `Option<A>` | 4.67 ns, 0 B (1.2×) | 4.51 ns, 0 B (1.0×) |
| Funcky 3.8.0 `Option<T>` | 14.5 ns, 24 B (3.8×) | 18.2 ns, 24 B (4.1×) |
| Optional 4.0.0 `Option<T>` | 13.9 ns, 24 B (3.6×) | 11.0 ns, 24 B (2.5×) |
| OneOf 3.0.271 `OneOf<T, None>` | 5.93 ns, 0 B (1.5×) | 6.46 ns, 0 B (1.5×) |

### Synchronous chain of eight steps

`Map`, `Filter`, `Bind`, `Map`, `Filter`, `Bind`, `Map`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 5.98 ns, 0 B | 8.13 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 7.81 ns, 0 B (1.3×) | 21.4 ns, 0 B (2.6×) |
| LanguageExt 4.4.9 `Option<A>` | 6.87 ns, 0 B (1.1×) | 20.1 ns, 0 B (2.5×) |
| Funcky 3.8.0 `Option<T>` | 40.9 ns, 72 B (6.8×) | 67.9 ns, 72 B (8.4×) |
| Optional 4.0.0 `Option<T>` | 51.0 ns, 72 B (8.5×) | 25.9 ns, 72 B (3.2×) |
| OneOf 3.0.271 `OneOf<T, None>` | 31.8 ns, 0 B (5.3×) | 38.2 ns, 0 B (4.7×) |

### Returning a maybe from a method

A method that is not inlined creates a maybe that holds a value and returns it, and the caller reads the value.
(Inlined, the compiler removes the maybe type of the struct libraries altogether.)

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 1.01 ns, 0 B | 1.96 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 1.00 ns, 0 B (1.0×) | 2.18 ns, 0 B (1.1×) |
| LanguageExt 4.4.9 `Option<A>` | 1.11 ns, 0 B (1.1×) | 2.05 ns, 0 B (1.0×) |
| Funcky 3.8.0 `Option<T>` | 1.31 ns, 0 B (1.3×) | 2.53 ns, 0 B (1.3×) |
| Optional 4.0.0 `Option<T>` | 0.920 ns, 0 B (0.9×) | 2.23 ns, 0 B (1.1×) |
| OneOf 3.0.271 `OneOf<T, None>` | 1.56 ns, 0 B (1.5×) | 2.03 ns, 0 B (1.0×) |

> **Why this library is not the fastest for the `int`, and is not clearly ahead for the `Customer`.**
> For the `int` Optional (0.920 ns) is 0.1 ns ahead of this library (1.01 ns) and CSharpFunctionalExtensions (1.00 ns) is level with it: within noise.
> For the `Customer` this library is the first (1.96 ns), but OneOf (2.03 ns) and LanguageExt (2.05 ns) are within 0.1 ns of it, which is also noise;
> the others are between 2.2 and 2.5 ns.

### From a value that may be missing

An `int?` or a `string` that may be `null` becomes a maybe, which is then read. OneOf has no counterpart.

| Library | `int?` with a value | `int?` that is null | `string` with a value | `string` that is null |
|---|---|---|---|---|
| **Wyzely.Functional.Maybe** | 1.21 ns, 0 B | 0.518 ns, 0 B | 1.20 ns, 0 B | 0.493 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 2.28 ns, 0 B (1.9×) | 0.292 ns, 0 B (0.6×) | 2.15 ns, 0 B (1.8×) | 0.963 ns, 0 B |
| LanguageExt 4.4.9 `Option<A>` | 2.74 ns, 0 B (2.3×) | 1.76 ns, 0 B (3.4×) | 2.25 ns, 0 B (1.9×) | 1.73 ns, 0 B |
| Funcky 3.8.0 `Option<T>` | 3.19 ns, 0 B (2.6×) | 1.67 ns, 0 B (3.2×) | 2.26 ns, 0 B (1.9×) | 1.09 ns, 0 B |
| Optional 4.0.0 `Option<T>` | 2.66 ns, 0 B (2.2×) | 0.540 ns, 0 B (1.0×) | 2.28 ns, 0 B (1.9×) | 0.916 ns, 0 B |

> **Why this library is not the fastest in every column.**
> Where a value is missing the work is a single test, and the times are 0.3 to 1.0 ns for this library, CSharpFunctionalExtensions and Optional:
> that is the loop of the benchmark itself, and a difference of a few tenths of a nanosecond there says nothing
> (the `int?` that is null took 0.254 ns here in the run of 2026-10-25 and 0.518 ns in this one). With a value this library is the fastest.

### A completed `Task<Maybe<T>>` as the receiver

The receiver is a task that is already completed and every delegate is synchronous.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 37.5 ns, 144 B | 25.1 ns, 160 B |
| CSharpFunctionalExtensions 3.7.0 | 51.8 ns, 216 B (1.4×) | 45.9 ns, 240 B (1.8×) |
| LanguageExt 4.4.9 `Option<A>` | 11.5 ns, 0 B (0.3×) | 19.4 ns, 0 B (0.8×) |
| Funcky 3.8.0 `Option<T>` | 23.8 ns, 24 B (0.6×) | 34.7 ns, 24 B (1.4×) |
| Optional 4.0.0 `Option<T>` | 28.7 ns, 24 B (0.8×) | 19.3 ns, 24 B (0.8×) |
| OneOf 3.0.271 `OneOf<T, None>` | 13.5 ns, 0 B (0.4×) | 18.1 ns, 0 B (0.7×) |
| Wyzely, awaiting first and then running the synchronous chain | 11.2 ns, 0 B (0.3×) | 11.6 ns, 0 B (0.5×) |

> **Why this library is not the fastest here.** A method that returns a `Task<Maybe<T>>` has to allocate a task for every step that changes the maybe,
> so a chain of two such steps is 144 to 160 B whatever the library does.
> LanguageExt, Optional, OneOf and Funcky are faster or allocate less only because they have no such methods, so their users await first, and so can you:
> the last row is as fast as theirs or faster (11 to 12 ns, 0 B, against 11 to 19 ns for LanguageExt and OneOf).
> Among the libraries that offer `Task` methods this one is the fastest and allocates the least.
> The `int` chain is slower than the `Customer` chain (38 against 25 ns) in every run, with fewer bytes allocated; a single step costs about the same for both,
> and we did not find the cause.

### A completed `ValueTask<Maybe<T>>` as the receiver

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 20.0 ns, 0 B | 15.6 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 67.0 ns, 0 B (3.4×) | 91.8 ns, 0 B (5.9×) |
| LanguageExt 4.4.9 `Option<A>` | 18.2 ns, 0 B (0.9×) | 27.2 ns, 0 B (1.7×) |
| Funcky 3.8.0 `Option<T>` | 29.6 ns, 24 B (1.5×) | 43.2 ns, 24 B (2.8×) |
| Optional 4.0.0 `Option<T>` | 38.3 ns, 24 B (1.9×) | 25.4 ns, 24 B (1.6×) |
| OneOf 3.0.271 `OneOf<T, None>` | 18.0 ns, 0 B (0.9×) | 15.3 ns, 0 B (1.0×) |
| Wyzely, awaiting first and then running the synchronous chain | 17.7 ns, 0 B (0.9×) | 18.6 ns, 0 B (1.2×) |

> **Why this library is not the fastest here.** For the `int` LanguageExt (which awaits first) took 18.2 ns against 20.0 ns here, and OneOf 18.0 ns.
> Most of each number is the `await` of the benchmark method itself (about 10 ns),
> and every step of a chain on a `ValueTask` costs about 2 ns more than the same step on a plain maybe,
> as a `ValueTask<Maybe<int>>` is a 24-byte struct that is passed along and created again by every step.
> Awaiting first and using the synchronous chain is the way to avoid that: the last row is as fast as the fastest for the `int` (17.7 ns),
> and 3 ns behind OneOf for the `Customer` (18.6 against 15.3 ns).
> Among the libraries that offer `ValueTask` methods this one is the fastest.

### A synchronous maybe followed by an asynchronous step

The receiver is a plain maybe, the first step is an asynchronous delegate that returns an already completed task, and a `Map` follows.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 18.9 ns, 72 B | 19.9 ns, 72 B |
| CSharpFunctionalExtensions 3.7.0 | 26.0 ns, 72 B (1.4×) | 25.4 ns, 72 B (1.3×) |
| LanguageExt 4.4.9 `Option<A>` | 10.3 ns, 0 B (0.5×) | 8.88 ns, 0 B (0.4×) |
| Funcky 3.8.0 `Option<T>` | 22.6 ns, 24 B (1.2×) | 9.87 ns, 0 B (0.5×) |
| Optional 4.0.0 `Option<T>` | 14.6 ns, 24 B (0.8×) | 13.3 ns, 24 B (0.7×) |
| OneOf 3.0.271 `OneOf<T, None>` | 9.05 ns, 0 B (0.5×) | 9.13 ns, 0 B (0.5×) |
| Wyzely, awaiting first and then running the synchronous chain | 8.86 ns, 0 B (0.5×) | 10.5 ns, 0 B (0.5×) |

> **Why this library is not the fastest here.** The `Map` that follows the asynchronous step has to return a task with a new value, which is the 72 B.
> The libraries in the table that are faster are measured with hand-written code (check the maybe, await the step), which has no task to create;
> the last row is the same thing written with this library (8.9 and 10.5 ns, 0 B): level with the fastest of them for the `int` (9.1 ns),
> and 1.6 ns behind the fastest for the `Customer` (8.9 ns).
> Among the libraries that offer a `Bind` with an asynchronous delegate this one is the fastest.

### Asking whether the value satisfies a condition

`Any` or its nearest equivalent on a maybe that is asked once. LanguageExt and Optional have `Exists`.
CSharpFunctionalExtensions and Funcky have no such method: `Where` followed by `HasValue` or `Match` is measured. OneOf has only `Match`.
Every library gives the same answer.

| Library | `int`, has a value | `int`, none | `string`, has a value | `string`, none |
|---|---|---|---|---|
| **Wyzely.Functional.Maybe** | 0.512 ns, 0 B | 0.113 ns, 0 B | 0.553 ns, 0 B | 0.0341 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 1.81 ns, 0 B (3.5×) | 0.273 ns, 0 B | 1.53 ns, 0 B (2.8×) | 0.482 ns, 0 B |
| LanguageExt 4.4.9 `Option<A>` | 0.810 ns, 0 B (1.6×) | 0.0250 ns, 0 B | 0.808 ns, 0 B (1.5×) | 0.0308 ns, 0 B |
| Funcky 3.8.0 `Option<T>` | 3.15 ns, 0 B (6.2×) | 0.349 ns, 0 B | 2.51 ns, 0 B (4.5×) | 0.449 ns, 0 B |
| Optional 4.0.0 `Option<T>` | 0.892 ns, 0 B (1.7×) | 0.186 ns, 0 B | 0.762 ns, 0 B (1.4×) | 0.440 ns, 0 B |
| OneOf 3.0.271 `OneOf<T, None>` | 0.519 ns, 0 B (1.0×) | 0.179 ns, 0 B | 0.554 ns, 0 B (1.0×) | 0.0105 ns, 0 B |

> **Do not read a rank from this table.** Every cell is 3.2 ns or less, and half of them (12 of 24) are below 0.5 ns, which is the loop of the benchmark itself:
> the JIT inlines the whole call and the none columns come close to the useful resolution of the timer. What can be read is that nothing allocates,
> and that the libraries that need two steps for the question (CSharpFunctionalExtensions, Funcky) took 1.5 to 3.2 ns where one step took 0.5 to 0.9 ns.

### Collecting the values of a sequence of maybes

Eight maybes, every second one is none; the values are collected and added up. CSharpFunctionalExtensions has `Choose`, LanguageExt `Somes`, Optional `Values`.
Funcky has none, so every option is turned into a sequence of one and joined with `SelectMany`; OneOf collects with a loop. Every library computes the same sum.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 33.9 ns, 88 B | 44.8 ns, 88 B |
| CSharpFunctionalExtensions 3.7.0 | 31.7 ns, 88 B (0.9×) | 35.9 ns, 88 B (0.8×) |
| LanguageExt 4.4.9 `Option<A>` | 32.1 ns, 88 B (0.9×) | 51.8 ns, 88 B (1.2×) |
| Funcky 3.8.0 `Option<T>` | 103 ns, 448 B (3.0×) | 145 ns, 448 B (3.2×) |
| Optional 4.0.0 `Option<T>` | 31.5 ns, 88 B (0.9×) | 49.4 ns, 88 B (1.1×) |
| OneOf 3.0.271 `OneOf<T, None>` | 36.8 ns, 128 B (1.1×) | 47.3 ns, 160 B (1.1×) |

> **No meaningful difference, except Funcky.**
> The libraries are within 32 to 52 ns of each other, and the 88 B are the list or sequence that holds the four values.
> Funcky is about three times slower and allocates 448 B, which is what its workaround costs; it does not say how Funcky's own methods would perform.
> For the `int` CSharpFunctionalExtensions, LanguageExt and Optional (31.5 to 32.1 ns) are 2 ns ahead of this library (33.9 ns).
> For the `Customer` CSharpFunctionalExtensions (35.9 ns) is ahead of this library (44.8 ns), and LanguageExt (51.8 ns) and Optional (49.4 ns) are not;
> we did not investigate the gap, and the caveats below say how much runs differ.

### What the numbers say

- **On synchronous chains this library is the fastest, and LanguageExt, CSharpFunctionalExtensions and OneOf are close for an `int`.**
  The four-step chain took 4.5 ns here and 5.0 to 5.7 ns for them, without allocating; the differences are inside the run-to-run noise.
  With a reference type CSharpFunctionalExtensions and LanguageExt took about 2 times as long (11 to 12 ns) and OneOf 1.2 times.
  Funcky (15 to 28 ns) and Optional (11 to 21 ns) allocated 24 B per chain and took 2 to 5 times as long. The eight-step chain shows the same picture.
- **When the filter rejects, this library, CSharpFunctionalExtensions and LanguageExt are close** at 3.9 to 4.7 ns, OneOf took 6 to 6.5 ns,
  and Funcky and Optional 11 to 18 ns with 24 B.
- **Making a maybe from a nullable** is 1.2 ns (an `int?` and a `string`) for this library with a value, and the others took 2.2 to 3.2 ns.
  With nothing in it every library was between 0.3 and 1.8 ns, which is the loop of the benchmark.
- **Chaining on a completed `Task` allocates, and awaiting first is cheaper.** A `Task<Maybe<T>>` chain in this library allocated 144 to 160 B and took
  25 to 38 ns; the `Filter` step hands back the task it received when the maybe is left as it was, as do `Tap`, `TapNone` and `OrElse`.
  Awaiting the task and then running the synchronous chain allocated nothing and took 11 to 12 ns.
  The same holds for the other libraries: their await-first chains took 12 to 35 ns.
  **If you can await, await, and chain on the result.** The `Task` extension methods are for when you want the chain to read as one expression, or when the
  maybe has to stay in a task. CSharpFunctionalExtensions allocated 216 to 240 B.
- **`ValueTask`:** this library allocated nothing and took 16 to 20 ns, where CSharpFunctionalExtensions, the only other one with `ValueTask` extension methods,
  took 67 to 92 ns (3.4 to 5.9 times as long). Awaiting first was as cheap as the extension methods.
- **A synchronous maybe followed by an asynchronous step** allocated 72 B in this library and CSharpFunctionalExtensions;
  checking the maybe and awaiting by hand allocated nothing and was about 50 % faster.
- **Asking a maybe a question and collecting the values of several** (`Any`, `Choose`) show no difference that is worth a decision:
  the questions took 3.2 ns or less for every library, and `Choose` 32 to 52 ns for every library except Funcky,
  which has no `Choose` and took about three times as long with the workaround we measured.
- **Value type and reference type behave alike.** The struct libraries allocate nothing in either, apart from Funcky and Optional.
  The reference versions are slower in CSharpFunctionalExtensions and LanguageExt, which we did not investigate.

### Caveats

- **One machine, one configuration.** The numbers are from one Windows machine and .NET 10.
  The previous full run (2026-10-25, .NET 10.0.13, the same machine and settings) differs from this one by a median of 11 % per cell
  (122 timings compared, without Funcky, whose version changed); for the 82 timings of 2 ns or more the median is 10 % and the worst 42 %,
  and for the cells under 2 ns the worst is 777 % (OneOf, `int`, none: 0.0204 against 0.179 ns; for this library the `int?` with a value
  went from 0.432 to 1.21 ns).
  The library code and the .NET patch version changed between the two runs, so this is not a pure repeat.
  The runtime's tiered profile-guided optimization changes how well it inlines delegate calls. Do not read a rank from a 10 % difference.
- **The scenarios are small on purpose.** They isolate the maybe type, with `static` lambdas and no I/O. Real code spends its time elsewhere.
- **Equivalents are our choice**, and a library may have a faster way to do the same that we did not find.
- **No other runtime or platform.** Not Native AOT, not Linux or arm64. LanguageExt 5 (beta) is not included.
- **Delegates that capture variables allocate a closure** like in any C# code; use `static` lambdas on hot paths, as these benchmarks do.

Run them yourself (the whole suite takes about nine minutes; results differ by machine and runtime):

```shell
dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*SyncSuccess*"
```
