# Design decisions and roadmap

[Back to the Result overview](../../src/Result/README.md)

- **One aggregating `Error`, no error list on the result.** The result stays one reference wide; many failures are one `Error` with `Children`.
- **`Error` is a class, `Result` is a struct.**
  An `Error` has many fields and would make the result large; one reference keeps `Result` at one pointer, and the allocation happens only on the failure path.
- **Functional names, no `Async` suffix.** `Map`, `Bind`, `Tap`, ... for every receiver and delegate kind. The price is the async-lambda note above.
- **`default` is a success.** A struct always has a default value, and this is the one that needs no extra state: `default(Result)` is `Result.Success()` and
  `default(Result<T>)` is `Result.Success(default(T))`. A result is one nullable `Error` (`null` is a success), so there is no third "uninitialized"
  state that every member would have to check for and that analyzers would have to chase. The price is that a forgotten field or an array slot counts as
  a success, as in CSharpFunctionalExtensions; `Combine`, `Partition` and `SelectInParallel` exist so that you do not fill an array yourself.
- **Guarded readers.**
  `GetValue()`/`GetError()` are methods that an analyzer checks, not properties that throw.
- **`Try` does not catch cancellation by default.** Cancellation is control flow, not a failure. `ShouldCatch` changes that.
- **Message templates, not codes only.** The template is the stable identity, the details are the data, and `Message` is what humans read.
- **Configurable defaults, one global switch.**
  Anything that is a matter of taste or environment is a [setting](configuration.md) with a default; the settings are process-wide on purpose.

## Roadmap

- Re-check the C# 15 union layer after the final .NET 11 release.
- More analyzers (exceptions thrown inside `Map`/`Bind` lambdas, swallowed causes).
- Adapters in separate packages: ASP.NET Core / ProblemDetails, `ILogger` and OpenTelemetry, test helpers.
- A public API analyzer and package validation once the API stabilizes.

## Things we measured and changed

Decisions that were revised after a measurement, so that the reasoning is visible.

- **Receivers by value, not `in`.** Passing a `Result<T>` by `in` looked like a free win for large payloads,
  and a microbenchmark that read a result from a field agreed. A chain, however, passes the result of the previous call, a temporary,
  and taking its address forces it into memory: a four-step chain measured several times slower with `in`, and a chain on a 72-byte payload was no faster.
  Receivers are by value, and a test keeps it that way.
- **Argument checks stay.** Each method checks its delegate and `Task` arguments up front. Measured with and without, that costs about 0.5 ns per call;
  without them a `null` delegate throws late inside a returned task, or never, depending on the data.
- **Implicit conversions follow C# 15 unions on every target.**
  A `T` converts to `Result<T>` (a success; a `null` reference is a success that holds `null`),
  an `Error` converts to `Result<T>` and to `Result` (a failure; a `null` error throws `ArgumentNullException`),
  and `Result<T>` converts to `Result`, which keeps the state and drops the value. Nothing converts out of a result.
  The reasons: it matches what the C# 15 compiler gives a union on `net11.0`, so people who know unions get the same experience;
  and the operators exist in every asset, because a library compiled against the `net8.0` or `net10.0` asset that used the conversion would
  otherwise fail with `MissingMethodException` when an app targeting `net11.0` picks the `net11.0` asset
  (reproduced with a scratch library, not yet with the real package).
  On `net11.0` the library's own operators take precedence and behave the same as the compiler's.
  The trade-off: `Result<bool> IsActive() => false;` compiles and is a **success** holding `false`, and a bare `string` returned from a method
  that returns `Result<string>` is a success holding that text, not a failure message.
  Use `Result.Success(...)`, `Result.Failure(...)` or `Error.Create(...)` when the intent should be visible; `return error;` is a failure.
  A bare value inside a chain is still a compile error (`Bind(x => x + 1)`), because the lambda must return a result.
  `Result<Error>` cannot be converted to implicitly (ambiguous, CS0457). Converting allocates nothing.
- **Parallel helpers use `Parallel.ForEachAsync` with an explicit limit.**
  Its default of `-1` means "the number of processors", which would have silently throttled I/O-bound work that was asked to run without a limit.
