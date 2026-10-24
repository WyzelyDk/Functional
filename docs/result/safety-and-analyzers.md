# Safety and analyzers

[Back to the Result overview](../../src/Result/README.md)

```csharp
Result unfilled = default;
unfilled.IsSuccess;       // true: a default result is a success, the same as Result.Success()
unfilled.ToString();      // "Success"
```

A struct always has a default value, and for a result it is a success, so there is no third state to check for. The consequence is that a
forgotten field or an array slot that nobody assigned counts as "it worked": collect results with `Combine`, `Partition` or `SelectInParallel`,
which never expose an unassigned slot, instead of filling an array yourself.

The package ships analyzers, all enabled by default: `WYZ0101`, `WYZ0102` and `WYZ0111` are warnings, and the other rules from `WYZ0103` on are suggestions (info) that point at the method of the library that says what a piece of code does:

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

Six more suggestions (info) point at code that does more than it has to. A rewrite must give the same result and type, so a rule reports only the exact shape:

| Id | Reports | Fix | Code fix |
|---|---|---|---|
| [`WYZ0112`](analyzers/WYZ0112.md) | `Bind(x => Result.Success(f(x)))` | `Map(x => f(x))` | yes |
| [`WYZ0113`](analyzers/WYZ0113.md) | `Map(x => { Use(x); return x; })` | `Tap(x => { Use(x); })` | yes |
| [`WYZ0114`](analyzers/WYZ0114.md) | `Match(v => v, _ => fallback)` with a cheap fallback | `ValueOr(fallback)` | yes |
| [`WYZ0115`](analyzers/WYZ0115.md) | `Map(x => x)` and `Bind(x => Result.Success(x))` | remove the step | yes |
| [`WYZ0116`](analyzers/WYZ0116.md) | `Result.Failure(Error.Create(message))` | `Result.Failure(message)` | yes |
| [`WYZ0117`](analyzers/WYZ0117.md) | `Bind(x => x)` on a result of a result | `Flatten()` | yes |

Change a severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.WYZ0101.severity = error
```

