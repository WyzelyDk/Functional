using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private readonly Task<Result<int>> _ardalisTask = Task.FromResult(Result<int>.Success(Data.Number));
    private readonly Task<Result<Customer>> _ardalisTaskRef = Task.FromResult(Result<Customer>.Success(Data.Customer));

    // Ardalis.Result has no Ensure: the check is a Bind step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Ardalis()
    {
        var result = await _ardalisTask
            .MapAsync(static x => x + 1)
            .BindAsync(static x => x > 0 ? Result<int>.Success(x) : Result<int>.Error("Rejected."))
            .BindAsync(static x => Result<int>.Success(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> ArdalisRef()
    {
        var result = await _ardalisTaskRef
            .MapAsync(static c => c.Name)
            .BindAsync(static n => n.Length > 0 ? Result<string>.Success(n) : Result<string>.Error("Rejected."))
            .BindAsync(static n => Result<string>.Success(n));

        return result.Value.Length;
    }
}
