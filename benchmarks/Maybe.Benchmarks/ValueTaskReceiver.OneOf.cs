using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class ValueTaskReceiver
{
    private readonly ValueTask<OneOf<int, None>> _oneOfTask = new(Data.Number);
    private readonly ValueTask<OneOf<Customer, None>> _oneOfTaskRef = new(Data.Customer);

    // OneOf has no extension methods for tasks and value tasks: the value task is awaited and the synchronous chain runs on the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> OneOfLib() => (await _oneOfTask)
        .MapT0(static x => x + 1)
        .Match(static x => x > 0 ? OneOfValue(x) : default(None), static none => none)
        .Match(static x => OneOfValue(x * 2), static none => none)
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> OneOfRef() => (await _oneOfTaskRef)
        .MapT0(static c => c.Name)
        .Match(static n => n.Length > 0 ? OneOfValue(n) : default(None), static none => none)
        .Match(static n => OneOfValue(n), static none => none)
        .Match(static n => n.Length, static _ => -1);

    private static OneOf<T, None> OneOfValue<T>(T value) => value;
}
