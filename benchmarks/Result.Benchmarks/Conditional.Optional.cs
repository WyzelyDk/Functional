using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    // Option<T, TException> has no conditional methods: the conditions are conditional expressions, and the check is Filter with the error.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OptionalLib() => Option.Some<int, string>(_number)
        .Map(static x => x > 0 ? x + 1 : x)
        .FlatMap(static x => x > 1 ? Option.Some<int, string>(x * 2) : Option.Some<int, string>(x))
        .Filter(static x => x > 0, "Rejected.")
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OptionalRef() => Option.Some<string, string>(_name)
        .Map(static n => n.Length > 0 ? n.Trim() : n)
        .FlatMap(static n => n.Length > 1 ? Option.Some<string, string>(n.TrimEnd()) : Option.Some<string, string>(n))
        .Filter(static n => n.Length > 0, "Rejected.")
        .Match(static n => n.Length, static _ => -1);
}
