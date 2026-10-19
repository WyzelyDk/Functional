using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<ErrorOr<int>> ErrorOrDone = Task.FromResult<ErrorOr<int>>(43);
    private static readonly Task<ErrorOr<string>> ErrorOrDoneRef = Task.FromResult<ErrorOr<string>>(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> ErrorOrLib()
    {
        ErrorOr<int> start = _number;
        var result = await start
            .ThenAsync(static x => ErrorOrDone)
            .Then(static x => x + 1);

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> ErrorOrRef()
    {
        ErrorOr<Customer> start = _customer;
        var result = await start
            .ThenAsync(static c => ErrorOrDoneRef)
            .Then(static n => n.Length);

        return result.Value;
    }
}
