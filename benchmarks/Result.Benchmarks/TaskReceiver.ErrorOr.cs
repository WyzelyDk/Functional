using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private static readonly Error ErrorOrRejected = Error.Failure("Rejected", "Rejected.");

    private readonly Task<ErrorOr<int>> _errorOrTask = Task.FromResult<ErrorOr<int>>(Data.Number);
    private readonly Task<ErrorOr<Customer>> _errorOrTaskRef = Task.FromResult<ErrorOr<Customer>>(Data.Customer);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> ErrorOrLib()
    {
        var result = await _errorOrTask
            .Then(static x => x + 1)
            .FailIf(static x => x <= 0, ErrorOrRejected)
            .Then(static x => ToErrorOr(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> ErrorOrRef()
    {
        var result = await _errorOrTaskRef
            .Then(static c => c.Name)
            .FailIf(static n => n.Length <= 0, ErrorOrRejected)
            .Then(static n => ToErrorOr(n));

        return result.Value.Length;
    }

    private static ErrorOr<T> ToErrorOr<T>(T value) => value;
}
