using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<OneOf<int, Error<string>>> OneOfDone = Task.FromResult<OneOf<int, Error<string>>>(43);
    private static readonly Task<OneOf<string, Error<string>>> OneOfDoneRef = Task.FromResult<OneOf<string, Error<string>>>(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OneOfLib()
    {
        OneOf<int, Error<string>> first = _number;
        var second = first.IsT0 ? await OneOfDone : new Error<string>("Rejected.");

        return second.Match(static x => x + 1, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OneOfRef()
    {
        OneOf<Customer, Error<string>> first = _customer;
        var second = first.IsT0 ? await OneOfDoneRef : new Error<string>("Rejected.");

        return second.Match(static n => n.Length, static _ => -1);
    }
}
