using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class SyncSuccess
{
    // Option<T, TException> is the result-like type of Optional; the error type here is a string.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OptionalLib() => Option.Some<int, string>(_number)
        .Map(static x => x + 1)
        .Filter(static x => x > 0, "Rejected.")
        .FlatMap(static x => Option.Some<int, string>(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OptionalRef() => Option.Some<Customer, string>(_customer)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0, "Rejected.")
        .FlatMap(static n => Option.Some<string, string>(n))
        .Match(static n => n.Length, static _ => -1);
}
