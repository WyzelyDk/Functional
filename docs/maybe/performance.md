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
BenchmarkDotNet 0.15.8, .NET 10.0.12, an Intel Core Ultra 7 265H, Windows 11, the in-process toolchain, 5 warm-up and 10 measured iterations
of 100 ms per benchmark, one full run on 2026-10-19.

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
| **Wyzely.Functional.Maybe** | 4.37 ns, 0 B | 5.18 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 5.89 ns, 0 B (1.3×) | 12.8 ns, 0 B (2.5×) |
| LanguageExt 4.4.9 `Option<A>` | 5.17 ns, 0 B (1.2×) | 11.6 ns, 0 B (2.2×) |
| Funcky 3.6.1 `Option<T>` | 14.2 ns, 24 B (3.2×) | 25.8 ns, 24 B (5.0×) |
| Optional 4.0.0 `Option<T>` | 18.2 ns, 24 B (4.2×) | 10.8 ns, 24 B (2.1×) |
| OneOf 3.0.271 `OneOf<T, None>` | 6.09 ns, 0 B (1.4×) | 7.50 ns, 0 B (1.4×) |

### Synchronous chain, the filter rejects the value

The same chain, but the filter rejects the value, so the later steps are skipped and the none branch of `Match` runs.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 3.96 ns, 0 B | 4.29 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 4.88 ns, 0 B (1.2×) | 4.79 ns, 0 B (1.1×) |
| LanguageExt 4.4.9 `Option<A>` | 4.59 ns, 0 B (1.2×) | 4.66 ns, 0 B (1.1×) |
| Funcky 3.6.1 `Option<T>` | 12.7 ns, 24 B (3.2×) | 16.4 ns, 24 B (3.8×) |
| Optional 4.0.0 `Option<T>` | 15.4 ns, 24 B (3.9×) | 9.82 ns, 24 B (2.3×) |
| OneOf 3.0.271 `OneOf<T, None>` | 6.62 ns, 0 B (1.7×) | 6.05 ns, 0 B (1.4×) |

### Synchronous chain of eight steps

`Map`, `Filter`, `Bind`, `Map`, `Filter`, `Bind`, `Map`, `Match`.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 6.02 ns, 0 B | 8.05 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 8.41 ns, 0 B (1.4×) | 20.3 ns, 0 B (2.5×) |
| LanguageExt 4.4.9 `Option<A>` | 7.11 ns, 0 B (1.2×) | 18.6 ns, 0 B (2.3×) |
| Funcky 3.6.1 `Option<T>` | 38.1 ns, 72 B (6.3×) | 63.5 ns, 72 B (7.9×) |
| Optional 4.0.0 `Option<T>` | 44.8 ns, 72 B (7.4×) | 24.7 ns, 72 B (3.1×) |
| OneOf 3.0.271 `OneOf<T, None>` | 25.0 ns, 0 B (4.2×) | 34.8 ns, 0 B (4.3×) |

### Returning a maybe from a method

A method that is not inlined creates a maybe that holds a value and returns it, and the caller reads the value. (Inlined, the compiler
removes the maybe type of the struct libraries altogether.)

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 0.96 ns, 0 B | 1.74 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 0.98 ns, 0 B (1.0×) | 1.79 ns, 0 B (1.0×) |
| LanguageExt 4.4.9 `Option<A>` | 1.28 ns, 0 B (1.3×) | 2.79 ns, 0 B (1.6×) |
| Funcky 3.6.1 `Option<T>` | 1.18 ns, 0 B (1.2×) | 1.84 ns, 0 B (1.1×) |
| Optional 4.0.0 `Option<T>` | 0.91 ns, 0 B (0.9×) | 2.20 ns, 0 B (1.3×) |
| OneOf 3.0.271 `OneOf<T, None>` | 1.35 ns, 0 B (1.4×) | 1.79 ns, 0 B (1.0×) |

> **Why this library is not the fastest here.** Optional took 0.91 ns against 0.96 ns for the `int`: the same within noise. For the `Customer` the libraries are
> between 1.7 and 2.8 ns, and this one is the fastest.

### From a value that may be missing

An `int?` or a `string` that may be `null` becomes a maybe, which is then read. OneOf has no counterpart.

| Library | `int?` with a value | `int?` that is null | `string` with a value | `string` that is null |
|---|---|---|---|---|
| **Wyzely.Functional.Maybe** | 1.22 ns, 0 B | 0.47 ns, 0 B | 1.38 ns, 0 B | 0.46 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 2.20 ns, 0 B (1.8×) | 0.31 ns, 0 B | 2.31 ns, 0 B (1.7×) | 0.71 ns, 0 B |
| LanguageExt 4.4.9 `Option<A>` | 2.75 ns, 0 B (2.3×) | 1.71 ns, 0 B | 2.14 ns, 0 B (1.5×) | 1.83 ns, 0 B |
| Funcky 3.6.1 `Option<T>` | 3.13 ns, 0 B (2.6×) | 1.69 ns, 0 B | 2.29 ns, 0 B (1.7×) | 1.09 ns, 0 B |
| Optional 4.0.0 `Option<T>` | 2.81 ns, 0 B (2.3×) | 0.75 ns, 0 B | 2.39 ns, 0 B (1.7×) | 0.88 ns, 0 B |

> **Why this library is not the fastest in every column.** Where a value is missing the work is a single test, and the times are 0.3 to 0.5 ns for this library and
> CSharpFunctionalExtensions: that is the loop of the benchmark itself, and a difference of 0.2 ns there says nothing. With a value this library is the fastest.

### A completed `Task<Maybe<T>>` as the receiver

The receiver is a task that is already completed and every delegate is synchronous.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 33.4 ns, 144 B | 20.6 ns, 160 B |
| CSharpFunctionalExtensions 3.7.0 | 50.4 ns, 216 B (1.5×) | 42.1 ns, 240 B (2.0×) |
| LanguageExt 4.4.9 `Option<A>` | 11.9 ns, 0 B (0.4×) | 18.4 ns, 0 B (0.9×) |
| Funcky 3.6.1 `Option<T>` | 22.0 ns, 24 B (0.7×) | 32.6 ns, 24 B (1.6×) |
| Optional 4.0.0 `Option<T>` | 26.8 ns, 24 B (0.8×) | 18.7 ns, 24 B (0.9×) |
| OneOf 3.0.271 `OneOf<T, None>` | 13.5 ns, 0 B (0.4×) | 16.2 ns, 0 B (0.8×) |
| Wyzely, awaiting first and then running the synchronous chain | 11.1 ns, 0 B (0.3×) | 12.2 ns, 0 B (0.6×) |

> **Why this library is not the fastest here.** A method that returns a `Task<Maybe<T>>` has to allocate a task for every step that changes the maybe, so a chain
> of two such steps is 144 to 160 B whatever the library does. LanguageExt, Optional, OneOf and Funcky are faster or allocate less only because they have no
> such methods, so their users await first, and so can you: the last row is as fast as theirs (11 to 12 ns, 0 B). Among the libraries that offer `Task`
> methods this one is the fastest and allocates the least. The `int` chain is slower than the `Customer` chain (33 against 21 ns) in every run, with fewer bytes
> allocated; a single step costs about the same for both, and we did not find the cause.

### A completed `ValueTask<Maybe<T>>` as the receiver

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 19.1 ns, 0 B | 15.5 ns, 0 B |
| CSharpFunctionalExtensions 3.7.0 | 65.0 ns, 0 B (3.4×) | 85.7 ns, 0 B (5.5×) |
| LanguageExt 4.4.9 `Option<A>` | 15.5 ns, 0 B (0.8×) | 25.0 ns, 0 B (1.6×) |
| Funcky 3.6.1 `Option<T>` | 26.7 ns, 24 B (1.4×) | 38.8 ns, 24 B (2.5×) |
| Optional 4.0.0 `Option<T>` | 32.8 ns, 24 B (1.7×) | 23.6 ns, 24 B (1.5×) |
| OneOf 3.0.271 `OneOf<T, None>` | 18.2 ns, 0 B (1.0×) | 15.3 ns, 0 B (1.0×) |
| Wyzely, awaiting first and then running the synchronous chain | 16.6 ns, 0 B (0.9×) | 18.2 ns, 0 B (1.2×) |

> **Why this library is not the fastest here.** For the `int` LanguageExt (which awaits first) took 15.5 ns against 19.1 ns here, and OneOf 18.2 ns. Most of each number is the
> `await` of the benchmark method itself (about 10 ns), and every step of a chain on a `ValueTask` costs about 2 ns more than the same step on a plain maybe, as
> a `ValueTask<Maybe<int>>` is a 24-byte struct that is passed along and created again by every step. Awaiting first and using the synchronous chain is the way to
> avoid that, and the last row is as fast as the fastest. Among the libraries that offer `ValueTask` methods this one is the fastest.

### A synchronous maybe followed by an asynchronous step

The receiver is a plain maybe, the first step is an asynchronous delegate that returns an already completed task, and a `Map` follows.

| Library | Value type (`int`) | Reference type (`Customer`) |
|---|---|---|
| **Wyzely.Functional.Maybe** | 18.9 ns, 72 B | 16.6 ns, 72 B |
| CSharpFunctionalExtensions 3.7.0 | 22.4 ns, 72 B (1.2×) | 22.4 ns, 72 B (1.3×) |
| LanguageExt 4.4.9 `Option<A>` | 9.92 ns, 0 B (0.5×) | 9.13 ns, 0 B (0.5×) |
| Funcky 3.6.1 `Option<T>` | 20.2 ns, 24 B (1.1×) | 9.20 ns, 0 B (0.6×) |
| Optional 4.0.0 `Option<T>` | 13.4 ns, 24 B (0.7×) | 12.7 ns, 24 B (0.8×) |
| OneOf 3.0.271 `OneOf<T, None>` | 8.56 ns, 0 B (0.5×) | 8.83 ns, 0 B (0.5×) |
| Wyzely, awaiting first and then running the synchronous chain | 9.61 ns, 0 B (0.5×) | 9.72 ns, 0 B (0.6×) |

> **Why this library is not the fastest here.** The `Map` that follows the asynchronous step has to return a task with a new value, which is the 72 B. The libraries in the
> table that are faster are measured with hand-written code (check the maybe, await the step), which has no task to create; the last row is the same thing
> written with this library (9.6 ns, 0 B), and that is as fast as the fastest of them (8.6 to 9.9 ns). Among the libraries that offer a `Bind` with an
> asynchronous delegate this one is the fastest.

### What the numbers say

- **On synchronous chains this library is the fastest, and LanguageExt, CSharpFunctionalExtensions and OneOf are close for an `int`.** The four-step chain
  took 4.4 ns here and 5.2 to 6.1 ns for them, without allocating; the differences are inside the run-to-run noise. With a reference type
  CSharpFunctionalExtensions and LanguageExt took about 2.4 times as long (12 to 13 ns) and OneOf 1.4 times. Funcky (14 to 26 ns) and Optional (11 to 18 ns)
  allocated 24 B per chain and took 2 to 5 times as long. The eight-step chain shows the same picture.
- **When the filter rejects, this library, CSharpFunctionalExtensions and LanguageExt are equal** at 4 to 5 ns, OneOf took 6 to 7 ns, and Funcky and Optional 10 to 16 ns
  with 24 B.
- **Making a maybe from a nullable** is 1.2 to 1.4 ns for this library with a value, and the others took 2.1 to 3.1 ns (1.5 to 2.6 times). With
  nothing in it every library was between 0.3 and 1.8 ns, which is the loop of the benchmark.
- **Chaining on a completed `Task` allocates, and awaiting first is cheaper.** A `Task<Maybe<T>>` chain in this library allocated 144 to 160 B and took
  21 to 33 ns; the `Filter` step hands back the task it received when the maybe is left as it was, as do `Tap`, `TapNone` and `Or`. Awaiting the task and then
  running the synchronous chain allocated nothing and took 11 to 12 ns. The same holds for the other libraries: their await-first chains took 12 to 27 ns.
  **If you can await, await, and chain on the result.** The `Task` extension methods are for when you want the chain to read as one expression, or when the
  maybe has to stay in a task. CSharpFunctionalExtensions allocated 216 to 240 B.
- **`ValueTask`:** this library allocated nothing and took 15 to 19 ns, where CSharpFunctionalExtensions, the only other one with `ValueTask`
  extension methods, took 65 to 86 ns (3.4 to 5.5 times as long). Awaiting first was as cheap as the extension methods.
- **A synchronous maybe followed by an asynchronous step** allocated 72 B in this library and CSharpFunctionalExtensions; checking the maybe and
  awaiting by hand allocated nothing and was about 45 % faster.
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

Run them yourself (the whole suite takes about six minutes; results differ by machine and runtime):

```shell
dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Maybe.Benchmarks -- --filter "*SyncSuccess*"
```
