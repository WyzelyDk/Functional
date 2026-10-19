using BenchmarkDotNet.Attributes;
using FluentResults;
using FluentResults.Extensions;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private readonly Task<Result<int>> _fluentTask = Task.FromResult(Result.Ok(Data.Number));
    private readonly Task<Result<Customer>> _fluentTaskRef = Task.FromResult(Result.Ok(Data.Customer));

    // FluentResults has no Ensure: the check is a Bind step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Fluent()
    {
        var result = await _fluentTask
            .Map(static x => x + 1)
            .Bind(static x => x > 0 ? Result.Ok(x) : Result.Fail<int>("Rejected."))
            .Bind(static x => Result.Ok(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FluentRef()
    {
        var result = await _fluentTaskRef
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 0 ? Result.Ok(n) : Result.Fail<string>("Rejected."))
            .Bind(static n => Result.Ok(n));

        return result.Value.Length;
    }
}
