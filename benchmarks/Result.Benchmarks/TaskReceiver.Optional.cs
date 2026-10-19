using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private readonly Task<Option<int, string>> _optionalTask = Task.FromResult(Option.Some<int, string>(Data.Number));
    private readonly Task<Option<Customer, string>> _optionalTaskRef = Task.FromResult(Option.Some<Customer, string>(Data.Customer));

    // Optional has no extension methods for tasks: the task is awaited and the synchronous chain runs on the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OptionalLib() => (await _optionalTask)
        .Map(static x => x + 1)
        .Filter(static x => x > 0, "Rejected.")
        .FlatMap(static x => Option.Some<int, string>(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OptionalRef() => (await _optionalTaskRef)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0, "Rejected.")
        .FlatMap(static n => Option.Some<string, string>(n))
        .Match(static n => n.Length, static _ => -1);
}
