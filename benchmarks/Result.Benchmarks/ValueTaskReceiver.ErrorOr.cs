using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    private static readonly Error ErrorOrRejected = Error.Failure("Rejected", "Rejected.");

    // ErrorOr has no extension methods for ValueTask<ErrorOr<T>>: await, then the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> ErrorOrLib() => (await new ValueTask<ErrorOr<int>>(_number))
        .Then(static x => x + 1)
        .FailIf(static x => x <= 0, ErrorOrRejected)
        .Then(static x => ToErrorOr(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> ErrorOrRef() => (await new ValueTask<ErrorOr<Customer>>(_customer))
        .Then(static c => c.Name)
        .FailIf(static n => n.Length <= 0, ErrorOrRejected)
        .Then(static n => ToErrorOr(n))
        .Match(static n => n.Length, static _ => -1);

    private static ErrorOr<T> ToErrorOr<T>(T value) => value;
}
