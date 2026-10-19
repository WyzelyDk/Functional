using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class ValueTaskReceiver
{
    private readonly ValueTask<Option<int>> _funckyTask = new(Option.Some(Data.Number));
    private readonly ValueTask<Option<Customer>> _funckyTaskRef = new(Option.Some(Data.Customer));

    // Funcky has no extension methods for ValueTask<Option<T>>: the value task is awaited and the synchronous chain runs on the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> FunckyLib() => (await _funckyTask)
        .Select(static x => x + 1)
        .Where(static x => x > 0)
        .SelectMany(static x => Option.Some(x * 2))
        .Match(none: static () => -1, some: static x => x);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FunckyRef() => (await _funckyTaskRef)
        .Select(static c => c.Name)
        .Where(static n => n.Length > 0)
        .SelectMany(static n => Option.Some(n))
        .Match(none: static () => -1, some: static n => n.Length);
}
