using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    private static readonly Error ErrorOrRejected = Error.Failure("Rejected", "Rejected.");

    // ErrorOr has no conditional methods: the conditions are conditional expressions inside Then. FailIf is its check.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int ErrorOrLib()
    {
        ErrorOr<int> start = _number;

        return start
            .Then(static x => x > 0 ? x + 1 : x)
            .Then(static x => x > 1 ? ToErrorOr(x * 2) : ToErrorOr(x))
            .FailIf(static x => x <= 0, ErrorOrRejected)
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ErrorOrRef()
    {
        ErrorOr<string> start = _name;

        return start
            .Then(static n => n.Length > 0 ? n.Trim() : n)
            .Then(static n => n.Length > 1 ? ToErrorOr(n.TrimEnd()) : ToErrorOr(n))
            .FailIf(static n => n.Length <= 0, ErrorOrRejected)
            .Match(static n => n.Length, static _ => -1);
    }

    private static ErrorOr<T> ToErrorOr<T>(T value) => value;
}
