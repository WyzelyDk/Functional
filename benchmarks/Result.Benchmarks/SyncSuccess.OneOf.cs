using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class SyncSuccess
{
    // OneOf is a union, not a railway library: only MapT0 and Match exist, so Ensure and Bind are written with Match.
    // A union of the value and Error<string> stands for the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib()
    {
        OneOf<int, Error<string>> start = _number;

        return start
            .MapT0(static x => x + 1)
            .Match(static x => x > 0 ? OneOfValue(x) : OneOfRejected<int>(), static e => e)
            .Match(static x => OneOfValue(x * 2), static e => e)
            .Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef()
    {
        OneOf<Customer, Error<string>> start = _customer;

        return start
            .MapT0(static c => c.Name)
            .Match(static n => n.Length > 0 ? OneOfValue(n) : OneOfRejected<string>(), static e => e)
            .Match(static n => OneOfValue(n), static e => e)
            .Match(static n => n.Length, static _ => -1);
    }

    private static OneOf<T, Error<string>> OneOfValue<T>(T value) => value;

    private static OneOf<T, Error<string>> OneOfRejected<T>() => new Error<string>("Rejected.");
}
