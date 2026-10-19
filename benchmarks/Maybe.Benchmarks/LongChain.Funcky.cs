using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class LongChain
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => Option.Some(_number)
        .Select(static x => x + 1)
        .Where(static x => x > 0)
        .SelectMany(static x => Option.Some(x * 2))
        .Select(static x => x - 3)
        .Where(static x => x != 0)
        .SelectMany(static x => Option.Some(x + 10))
        .Select(static x => x * 2)
        .Match(none: static () => -1, some: static x => x);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyRef() => Option.Some(_customer)
        .Select(static c => c.Name)
        .Where(static n => n.Length > 0)
        .SelectMany(static n => Option.Some(n))
        .Select(static n => n)
        .Where(static n => n[0] != ' ')
        .SelectMany(static n => Option.Some(n))
        .Select(static n => n)
        .Match(none: static () => -1, some: static n => n.Length);
}
