# Errors in depth

[Back to the Result overview](../../src/Result/README.md)

```csharp
var error = Error.NotFound("Order {OrderId} of {Customer} was not found.")
    .WithDetail("OrderId", 7)
    .WithDetail("Customer", "anna@example.com", isSensitive: true)
    .WithCode("ORDER_NOT_FOUND")
    .WithContext("Checkout");

error.ToString();          // "Checkout: Order 7 of anna@example.com was not found."
error.ToRedactedString();  // "Checkout: Order 7 of *** was not found."   <- safe to log
error.Template;            // "Order {OrderId} of {Customer} was not found."   <- stable identity for grouping
error.Kind;                // ErrorKind.NotFound
```

An `Error` is **immutable**: every `With...` method returns a modified copy.

| Member | Purpose |
|---|---|
| `Template`, `Message` | the message is a *template*; `{Name}` placeholders are filled from the `Details` (`{{` and `}}` give a single brace; an unknown placeholder stays as written) |
| `Details` | structured name/value pairs for logs and telemetry; mark secrets with `isSensitive: true` |
| `RedactedMessage`, `ToRedactedString()` | the same text with sensitive values replaced by `***` |
| `Code` | an optional, stable, machine-readable code |
| `Kind` | an open set: `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Unavailable`, `Timeout`, `Cancelled`, `Unexpected`, or your own `new ErrorKind("RateLimited")` |
| `IsTransient` | retrying might help; set by `Error.Unavailable` / `Error.Timeout` |
| `Exception` | the cause, kept by `Error.FromException` and `Result.Try` |
| `Context`, `WithContext(...)` | frames added as the error passes layers; `ToString()` prints them outermost first |
| `Children`, `Error.Aggregate(errors)` | one error that combines many (see below) |
| structural equality | two errors are equal when template, code, kind, flag, details, context and children are equal |

## One error for many failures

A result carries **one** `Error`. `Error.Aggregate` (used by `Combine`, `Zip`, `Partition` and the parallel helpers) combines several
into one whose `Children` are the individual errors; nested aggregates are flattened, a single error is returned as it is, and
`WithContext` on an aggregate adds the frame to every child. `Flatten()` always gives you the individual errors.

```csharp
var combined = Result.Combine([Result.Success(), Result.Failure("Name is missing."), Result.Failure("Email is invalid.")]);
combined.GetError().Message;   // "Name is missing., Email is invalid."
combined.GetError().Children;  // the two individual errors
```

