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
| `stream.Combine(ct)` / `stream.Partition(ct)` | the same for an `IAsyncEnumerable<Result<T>>` (`net8.0` and later): the stream is read once, in order, and the value task completes when it ends |
| `found.SelectEach(op)` | turns the result of a list (`Result<IReadOnlyList<T>>` or `Result<IEnumerable<T>>`) into a stream with one result per item, or a single failure when the list could not be read; `op` may be asynchronous |
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

A stream of results does not need a list in between. `SelectEach` makes one from the result of a list, running the operation for an item when the stream is read up to it, and
`Combine` or `Partition` collects a stream (they stop when the cancellation token is cancelled):

```csharp
var found = Result.Success<IReadOnlyList<string>>(["a.txt", "bb.txt"]);

var lengths = await found
    .SelectEach(static async name =>
    {
        await Task.Yield();

        return Result.Success(name.Length);
    })
    .Combine(cancellationToken);                  // Result<IReadOnlyList<int>> with 5, 6
```

`SelectInParallel` and `ForEachInParallel` accept an `IAsyncEnumerable<T>` as well. They read the stream to the end first and then run the operations,
as they do for a sequence that is enumerated before any operation starts; they do not feed work to the operations while the stream is still being read.

The parallel helpers always attempt **every** item (a failure does not hide the failures of the other items), aggregate the errors in
source order rather than completion order, and never expose an unassigned slot. They replace the "array of results filled in a loop"
pattern. An exception thrown by the operation escapes (wrap the operation in `Result.Try` to turn it into a failure);
cancellation stops items that have not started and surfaces as `OperationCanceledException`.

