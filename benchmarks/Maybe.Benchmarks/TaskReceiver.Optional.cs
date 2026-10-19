using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class TaskReceiver
{
    private readonly Task<Option<int>> _optionalTask = Task.FromResult(Option.Some(Data.Number));
    private readonly Task<Option<Customer>> _optionalTaskRef = Task.FromResult(Option.Some(Data.Customer));

    // Optional has no extension methods for Task<Option<T>>: the task is awaited and the synchronous chain runs on the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OptionalLib() => (await _optionalTask)
        .Map(static x => x + 1)
        .Filter(static x => x > 0)
        .FlatMap(static x => Option.Some(x * 2))
        .Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OptionalRef() => (await _optionalTaskRef)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0)
        .FlatMap(static n => Option.Some(n))
        .Match(static n => n.Length, static () => -1);
}
