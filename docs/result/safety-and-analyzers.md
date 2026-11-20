# Safety and analyzers

[Back to the Result overview](../../src/Result/README.md)

```csharp
Result unfilled = default;
unfilled.IsSuccess;       // true: a default result is a success, the same as Result.Success()
unfilled.ToString();      // "Success"
```

A struct always has a default value, and for a result it is a success, so there is no third state to check for.
The consequence is that a forgotten field or an array slot that nobody assigned counts as "it worked": collect results with `Combine`,
`Partition` or `SelectInParallel`, which never expose an unassigned slot, instead of filling an array yourself.

`var (value, error) = result;` is the unchecked route: it reads both parts without a check and never throws. For a success `error` is `null`,
for a failure `value` is `default`. No analyzer reports it, because nothing can go wrong on that line; the check moves to where `value` and `error`
are used, and both are nullable to the compiler. Use it when you want both parts at once, and `TryGetValue`, `Match` or `ValueOr` when you want the
compiler to see the check.

The package ships analyzers, all enabled by default: `WYZ0101`, `WYZ0102` and `WYZ0111` are warnings,
and the other rules are suggestions (info) that point at the method of the library that says what a piece of code does:

| Id | Reports | Fix | Code fix |
|---|---|---|---|
| [`WYZ0101`](analyzers/WYZ0101.md) | a `Result`, `Task<Result>` or `ValueTask<Result>` that is thrown away | handle it, return it, or write `_ = ...` | yes: `_ =` |
| [`WYZ0102`](analyzers/WYZ0102.md) | `GetError()`, `GetValue()` or `ConvertFailure()` that is not guarded by a check of the same result | check `IsFailure` / `IsSuccess` first, or use `TryGetError`, `TryGetValue`, `ValueOr` or `Match` | for `GetValue()` in a method that returns a result: a guard clause (see the page) |
| [`WYZ0103`](analyzers/WYZ0103.md) | a condition inside `Bind` that returns the value unchanged | `BindIf` | yes |
| [`WYZ0104`](analyzers/WYZ0104.md) | a condition inside `Map` that returns the value unchanged | `MapIf` | yes |
| [`WYZ0105`](analyzers/WYZ0105.md) | `Ensure` with a negated condition | `EnsureNot` | yes |
| [`WYZ0106`](analyzers/WYZ0106.md) | a null check inside `Bind` | `EnsureNotNull` | yes |
| [`WYZ0107`](analyzers/WYZ0107.md) | `Bind` then `Map` into a tuple of both values | `BindZip` | yes |
| [`WYZ0108`](analyzers/WYZ0108.md) | `Result.Failure<T>(r.GetError())` | `r.ConvertFailure<T>()` | yes |
| [`WYZ0109`](analyzers/WYZ0109.md) | a conditional between `Result.Success()` and `Result.Failure(e)` | `SuccessIf` or `FailureIf` | yes |
| [`WYZ0110`](analyzers/WYZ0110.md) | `Result.Try` inside `Bind` | `MapTry` | yes |

`WYZ0111` is a warning again, because it is about a mistake and not about style:

| Id | Reports | Fix | Code fix |
|---|---|---|---|
| [`WYZ0111`](analyzers/WYZ0111.md) | a value interpolated into the message of an error, as in `Error.Create($"Order {id} not found.")` | a message template with the value as a detail | yes, when each value is a name or a member access |

Nine more suggestions (info) point at code that does more than it has to. A rewrite must give the same result and type, so a rule reports only the exact shape:

| Id | Reports | Fix | Code fix |
|---|---|---|---|
| [`WYZ0112`](analyzers/WYZ0112.md) | `Bind(x => Result.Success(f(x)))` | `Map(x => f(x))` | yes |
| [`WYZ0113`](analyzers/WYZ0113.md) | `Map(x => { Use(x); return x; })` | `Tap(x => { Use(x); })` | yes |
| [`WYZ0114`](analyzers/WYZ0114.md) | `Match(v => v, onFailure)` on a result, or on a `Task` or `ValueTask` of one | `ValueOr(fallback)` for a cheap fallback that ignores the error, otherwise `ValueOr(onFailure)` | yes |
| [`WYZ0115`](analyzers/WYZ0115.md) | `Map(x => x)`, `Bind(x => Result.Success(x))`, `MapError(e => e)` and an `OrElse` that returns its error unchanged | remove the step | yes |
| [`WYZ0116`](analyzers/WYZ0116.md) | `Result.Failure(Error.Create(message))` | `Result.Failure(message)` | yes |
| [`WYZ0117`](analyzers/WYZ0117.md) | `Bind(x => x)` on a result of a result | `Flatten()` | yes |
| [`WYZ0118`](analyzers/WYZ0118.md) | `OrElse(e => error)`, which only replaces the error | `MapError(e => error)` | yes |
| [`WYZ0119`](analyzers/WYZ0119.md) | `r.IsSuccess ? r.GetValue() : fallback` | `r.ValueOr(fallback)`, as a function when the fallback is not cheap | yes |
| [`WYZ0120`](analyzers/WYZ0120.md) | `r.ValueOr(Compute())`, whose fallback is evaluated also for a success | `r.ValueOr(() => Compute())` | yes |

Change a severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.WYZ0101.severity = error
```

## Serialization

`System.Text.Json` refuses `Result`, `Result<T>`, `Error`, `ErrorDetail`, `ErrorFrame` and `ErrorKind`: serializing or deserializing one throws a
`NotSupportedException` that points here. It is deliberate, because a serializer that is not told about these types does harm without saying so:

- A result is written without its value, and a result that is read back is a **success** (`default` is a success), also when it was a failure.
- An `Error` carries the values of its details, and the details marked sensitive are meant never to leave the process. A serializer would write
  them, together with the message and the context, in plain text.

Map the result to a type of your own at the edge instead (an HTTP response, a message, a ProblemDetails of your own making), and decide what is written:

```csharp
using System.Text.Json;
using Wyzely.Functional;

Result<int> created = Result.Success(42);

var response = created.Match(
    static id => new OrderResponse(id, null),
    static error => new OrderResponse(null, error.Message));

var json = JsonSerializer.Serialize(response);      // {"OrderId":42,"Problem":null}

public sealed record OrderResponse(int? OrderId, string? Problem);
```

A converter that you register in the `JsonSerializerOptions` takes the place of the guard, so a form that you have chosen is still possible.
The guard also holds with a source-generated `JsonSerializerContext` and in a Native AOT app (the sample `samples/Aot.Samples` publishes and runs it).
The converters are public classes in the namespace `Wyzely.Functional.Internal` only so that the source generator can reach them; do not use them yourself.
The guard covers `System.Text.Json` only. Newtonsoft.Json is not guarded, because that would need a dependency, and it writes these types silently
and loses the value.

