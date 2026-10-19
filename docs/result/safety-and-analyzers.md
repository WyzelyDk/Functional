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

The package ships analyzers; all are warnings and enabled by default:

| Id | Reports | Fix | Code fix |
|---|---|---|---|
| [`WYZ0101`](analyzers/WYZ0101.md) | a `Result`, `Task<Result>` or `ValueTask<Result>` that is thrown away | handle it, return it, or write `_ = ...` | yes: `_ =` |
| [`WYZ0102`](analyzers/WYZ0102.md) | `GetError()` or `GetValue()` that is not guarded by a check of the same result | check `IsFailure` / `IsSuccess` first, or use `TryGetError`, `TryGetValue`, `ValueOr` or `Match` | no (see the page for why) |

Change a severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.WYZ0101.severity = error
```

