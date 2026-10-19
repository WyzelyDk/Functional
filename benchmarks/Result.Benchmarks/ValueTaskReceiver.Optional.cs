using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    // Optional has no extension methods for tasks: await, then the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OptionalLib() => (await new ValueTask<Option<int, string>>(Option.Some<int, string>(_number)))
        .Map(static x => x + 1)
        .Filter(static x => x > 0, "Rejected.")
        .FlatMap(static x => Option.Some<int, string>(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OptionalRef() => (await new ValueTask<Option<Customer, string>>(Option.Some<Customer, string>(_customer)))
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0, "Rejected.")
        .FlatMap(static n => Option.Some<string, string>(n))
        .Match(static n => n.Length, static _ => -1);
}
