using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncFailure
{
    // Funcky calls Map Select, Filter Where and Bind SelectMany, and puts the none branch first in Match.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => Option.Some(_number)
        .Select(static x => x + 1)
        .Where(static x => x < 0)
        .SelectMany(static x => Option.Some(x * 2))
        .Match(none: static () => -1, some: static x => x);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyRef() => Option.Some(_customer)
        .Select(static c => c.Name)
        .Where(static n => n.Length > 100)
        .SelectMany(static n => Option.Some(n))
        .Match(none: static () => -1, some: static n => n.Length);
}
