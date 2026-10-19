using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class SyncSuccess
{
    private static readonly Error ErrorOrRejected = Error.Failure("Rejected", "Rejected.");

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int ErrorOrLib()
    {
        ErrorOr<int> start = _number;

        return start
            .Then(static x => x + 1)
            .FailIf(static x => x <= 0, ErrorOrRejected)
            .Then(static x => ToErrorOr(x * 2))
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ErrorOrRef()
    {
        ErrorOr<Customer> start = _customer;

        return start
            .Then(static c => c.Name)
            .FailIf(static n => n.Length <= 0, ErrorOrRejected)
            .Then(static n => ToErrorOr(n))
            .Match(static n => n.Length, static _ => -1);
    }

    private static ErrorOr<T> ToErrorOr<T>(T value) => value;
}
