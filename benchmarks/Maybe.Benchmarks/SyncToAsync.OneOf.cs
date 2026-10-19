using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncToAsync
{
    private static readonly Task<OneOf<int, None>> OneOfDone = Task.FromResult<OneOf<int, None>>(43);
    private static readonly Task<OneOf<string, None>> OneOfDoneRef = Task.FromResult<OneOf<string, None>>(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OneOfLib()
    {
        OneOf<int, None> first = _number;
        var second = first.IsT0 ? await OneOfDone : default(None);

        return second.Match(static x => x + 1, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OneOfRef()
    {
        OneOf<Customer, None> first = _customer;
        var second = first.IsT0 ? await OneOfDoneRef : default(None);

        return second.Match(static n => n.Length, static _ => -1);
    }
}
