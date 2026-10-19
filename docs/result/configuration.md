# Configuration

[Back to the Result overview](../../src/Result/README.md)

Everything that can reasonably differ between applications is a setting with a sensible default, so nothing has to be configured.
Change settings once, at start-up:

```csharp
ResultConfiguration.Configure(options =>
{
    options.ChildSeparator = " | ";                       // how an aggregate error is shown as one message
    options.ShouldCatch = exception => true;              // let Result.Try turn cancellation into a failure too
    options.ExceptionMapper = exception => MyErrors.From(exception);   // how an exception becomes an Error when no handler is given
    options.DefaultMaxDegreeOfParallelism = 8;            // for SelectInParallel / ForEachInParallel without a limit
});
```

| Setting | Default | Affects |
|---|---|---|
| `AggregateMessage` | `One or more errors occurred.` | the template of an error made by `Error.Aggregate` |
| `ChildSeparator` | `, ` | the message of an aggregate error |
| `ContextSeparator` | `: ` | `Error.ToString()` and `ToRedactedString()`, between the context frames and the message |
| `CancelledMessage` | `The operation was cancelled.` | `Error.Cancelled()` without a message |
| `RedactedValue` | `***` | the value of a sensitive detail in redacted output |
| `ExceptionMapper` | `Error.FromException` | `Result.Try` and `Result.FromException` when you pass no handler |
| `ShouldCatch` | everything except `OperationCanceledException` | which exceptions `Result.Try` turns into failures |
| `DefaultMaxDegreeOfParallelism` | `-1` (no limit) | the parallel helpers when the caller passes no limit |
| `ContinueOnCapturedContext` | `false` | whether the library's awaits continue on the captured synchronization context |

The configuration is process-wide, because results and errors are created everywhere and passing an options object to every call would
defeat a lightweight result type. A change is applied atomically, and `Configure` hands your action a copy, so a reference that you keep
cannot change the active settings later. Text that an `Error` has already rendered is not rendered again, so configure before the first
error is created. `ResultConfiguration.Reset()` restores the defaults (useful in tests that change them).

