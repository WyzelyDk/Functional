using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    private static readonly Error<string> OneOfRejected = new("Rejected.");

    // OneOf has no extension methods for tasks: await, then the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OneOfLib() => (await new ValueTask<OneOf<int, Error<string>>>(_number))
        .MapT0(static x => x + 1)
        .Match(static x => x > 0 ? OneOfValue(x) : OneOfRejected, static e => e)
        .Match(static x => OneOfValue(x * 2), static e => e)
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OneOfRef() => (await new ValueTask<OneOf<Customer, Error<string>>>(_customer))
        .MapT0(static c => c.Name)
        .Match(static n => n.Length > 0 ? OneOfValue(n) : OneOfRejected, static e => e)
        .Match(static n => OneOfValue(n), static e => e)
        .Match(static n => n.Length, static _ => -1);

    private static OneOf<T, Error<string>> OneOfValue<T>(T value) => value;
}
