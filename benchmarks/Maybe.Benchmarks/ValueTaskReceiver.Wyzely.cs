using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class ValueTaskReceiver
{
    private readonly ValueTask<Maybe<int>> _wyzelyTask = new(Maybe.Some(Data.Number));
    private readonly ValueTask<Maybe<Customer>> _wyzelyTaskRef = new(Maybe.Some(Data.Customer));

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Wyzely()
    {
        var maybe = await _wyzelyTask
            .Map(static x => x + 1)
            .Filter(static x => x > 0)
            .Bind(static x => Maybe.Some(x * 2));

        return maybe.GetValue();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRef()
    {
        var maybe = await _wyzelyTaskRef
            .Map(static c => c.Name)
            .Filter(static n => n.Length > 0)
            .Bind(static n => Maybe.Some(n));

        return maybe.GetValue().Length;
    }

    // The alternative to chaining on the value task: await it first, then run the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> WyzelyAwaited() => (await _wyzelyTask)
        .Map(static x => x + 1)
        .Filter(static x => x > 0)
        .Bind(static x => Maybe.Some(x * 2))
        .Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRefAwaited() => (await _wyzelyTaskRef)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0)
        .Bind(static n => Maybe.Some(n))
        .Match(static n => n.Length, static () => -1);
}
