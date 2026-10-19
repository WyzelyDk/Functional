# Design decisions and roadmap

[Back to the Result overview](../../src/Result/README.md)

- **One aggregating `Error`, no error list on the result.** The result stays one reference wide; many failures are one `Error` with `Children`.
- **`Error` is a class, `Result` is a struct.** An `Error` has many fields and would make the result large; one reference keeps `Result` at one pointer, and the allocation happens only on the failure path.
- **Functional names, no `Async` suffix.** `Map`, `Bind`, `Tap`, ... for every receiver and delegate kind. The price is the async-lambda note above.
- **`default` is a success.** A struct always has a default value, and this is the one that needs no extra state: `default(Result)` is `Result.Success()` and
  `default(Result<T>)` is `Result.Success(default(T))`. A result is one nullable `Error` (`null` is a success), so there is no third "uninitialized"
  state that every member would have to check for and that analyzers would have to chase. The price is that a forgotten field or an array slot counts as
  a success, as in CSharpFunctionalExtensions; `Combine`, `Partition` and `SelectInParallel` exist so that you do not fill an array yourself.
- **Explicit, not implicit.** No conversion creates a result from a value or an error, and `GetValue()`/`GetError()` are methods that an analyzer checks, not properties that throw.
- **`Try` does not catch cancellation by default.** Cancellation is control flow, not a failure. `ShouldCatch` changes that.
- **Message templates, not codes only.** The template is the stable identity, the details are the data, and `Message` is what humans read.
- **Configurable defaults, one global switch.** Anything that is a matter of taste or environment is a [setting](configuration.md) with a default; the settings are process-wide on purpose.

## Roadmap

- Re-check the C# 15 union layer after the final .NET 11 release.
- More analyzers (exceptions thrown inside `Map`/`Bind` lambdas, swallowed causes).
- Adapters in separate packages: ASP.NET Core / ProblemDetails, `ILogger` and OpenTelemetry, test helpers.
- A public API analyzer and package validation once the API stabilizes.

## Things we measured and changed

Decisions that were revised after a measurement, so that the reasoning is visible.

- **Receivers by value, not `in`.** Passing a `Result<T>` by `in` looked like a free win for large payloads, and a microbenchmark that read a
  result from a field agreed. A chain, however, passes the result of the previous call, a temporary, and taking its address forces it into
  memory: a four-step chain measured several times slower with `in`, and a chain on a 72-byte payload was no faster. Receivers are by value,
  and a test keeps it that way.
- **Argument checks stay.** Each method checks its delegate and `Task` arguments up front. Measured with and without, that costs about
  0.5 ns per call; without them a `null` delegate throws late inside a returned task, or never, depending on the data.
- **No implicit conversions.** `return false;` in a method returning `Result<bool>` would compile as a success holding `false`. Results are
  created with `Result.Success(...)` and `Result.Failure(...)`.
- **Parallel helpers use `Parallel.ForEachAsync` with an explicit limit.** Its default of `-1` means "the number of processors", which would
  have silently throttled I/O-bound work that was asked to run without a limit.
