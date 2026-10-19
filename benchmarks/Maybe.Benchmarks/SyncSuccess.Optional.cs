using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncSuccess
{
    // Optional calls Bind FlatMap.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OptionalLib() => Option.Some(_number)
        .Map(static x => x + 1)
        .Filter(static x => x > 0)
        .FlatMap(static x => Option.Some(x * 2))
        .Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OptionalRef() => Option.Some(_customer)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0)
        .FlatMap(static n => Option.Some(n))
        .Match(static n => n.Length, static () => -1);
}
