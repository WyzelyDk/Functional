# Many results: Combine, Partition and parallel helpers

[Back to the Result overview](../../src/Result/README.md)

```csharp
Result[] checks = [Validate.Name(user), Validate.Email(user), Validate.Age(user)];
var combined = Result.Combine(checks);
// success when all succeeded; otherwise ONE failure whose Error aggregates the errors of all failed checks, in order
```

| API | |
|---|---|
| `Result.Combine(results)` / `results.Combine()` | many `Result` into one; many `Result<T>` into one `Result<IReadOnlyList<T>>` (values in order). Looks at **every** result, no short-circuit. |
| `Result.Combine(tasks)` | the same for `IEnumerable<Task<Result>>`, `IEnumerable<Task<Result<T>>>` and the `ValueTask` equivalents: waits for all, observes all |
| `results.Partition()` | `(values, errors)` of the successes and the failures; keeps the successes when something failed |
| `items.SelectInParallel(op, maxDegreeOfParallelism, ct)` | runs `op` for every item with bounded parallelism; values come back in **source order** |
| `items.ForEachInParallel(op, maxDegreeOfParallelism, ct)` | the same for operations that return no value |

```csharp
var lengths = await new[] { "a", "bb", "ccc" }.SelectInParallel(
    async (text, token) =>
    {
        await Task.Delay(1, token);
        return Result.Success(text.Length);
    },
    maxDegreeOfParallelism: 2);                   // Result<IReadOnlyList<int>> with 1, 2, 3
```

The parallel helpers always attempt **every** item (a failure does not hide the failures of the other items), aggregate the errors in
source order rather than completion order, and never expose an unassigned slot. They replace the "array of results filled in a loop"
pattern. An exception thrown by the operation escapes (wrap the operation in `Result.Try` to turn it into a failure);
cancellation stops items that have not started and surfaces as `OperationCanceledException`.

