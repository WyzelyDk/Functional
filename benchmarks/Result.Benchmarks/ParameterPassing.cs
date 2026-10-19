using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

// Does passing a result by `in` instead of by value pay off? The same body is written once with a by-value receiver and once with
// an `in` receiver, for the untyped result (one pointer), a result with a small payload (int) and one with a large payload (a
// 64-byte struct), each with the call allowed to inline (as in most real code) and, where it matters, forced not to inline.
[MemoryDiagnoser]
public class ParameterPassing
{
    private static readonly Func<int, int> SmallFunction = static value => value + 1;
    private static readonly Func<Large64, long> LargeFunction = static value => value.A;
    private static readonly Func<int, int> SmallStep = static value => value + 1;
    private static readonly Func<Large64, Large64> LargeStep = static value => new Large64(value.A + 1);

    private readonly Result _untyped = Result.Success();
    private readonly Result<int> _small = Result.Success(7);
    private readonly Result<Large64> _large = Result.Success(new Large64(7));

    [Benchmark(Baseline = true)]
    public int SmallByValue() => ByValue(_small, SmallFunction, -1);

    [Benchmark]
    public int SmallIn() => ViaIn(in _small, SmallFunction, -1);

    [Benchmark]
    public int SmallByValueNotInlined() => ByValueNotInlined(_small, SmallFunction, -1);

    [Benchmark]
    public int SmallInNotInlined() => ViaInNotInlined(in _small, SmallFunction, -1);

    [Benchmark]
    public long LargeByValue() => ByValue(_large, LargeFunction, -1);

    [Benchmark]
    public long LargeIn() => ViaIn(in _large, LargeFunction, -1);

    [Benchmark]
    public long LargeByValueNotInlined() => ByValueNotInlined(_large, LargeFunction, -1);

    [Benchmark]
    public long LargeInNotInlined() => ViaInNotInlined(in _large, LargeFunction, -1);

    [Benchmark]
    public int UntypedByValueNotInlined() => UntypedByValue(_untyped, 1, -1);

    [Benchmark]
    public int UntypedInNotInlined() => UntypedViaIn(in _untyped, 1, -1);

    // A chain passes the result of the previous call, a temporary, not a field: this is how the methods are really used.
    [Benchmark]
    public int SmallChainByValue() => ChainByValue(ChainByValue(ChainByValue(_small, SmallStep), SmallStep), SmallStep).GetValue();

    [Benchmark]
    public int SmallChainIn() => ChainIn(ChainIn(ChainIn(_small, SmallStep), SmallStep), SmallStep).GetValue();

    [Benchmark]
    public long LargeChainByValue() => ChainByValue(ChainByValue(ChainByValue(_large, LargeStep), LargeStep), LargeStep).GetValue().A;

    [Benchmark]
    public long LargeChainIn() => ChainIn(ChainIn(ChainIn(_large, LargeStep), LargeStep), LargeStep).GetValue().A;

    private static Result<T> ChainByValue<T>(Result<T> result, Func<T, T> step) =>
        result.TryGetValue(out var value) ? Result.Success(step(value)) : result;

    private static Result<T> ChainIn<T>(in Result<T> result, Func<T, T> step) =>
        result.TryGetValue(out var value) ? Result.Success(step(value)) : result;

    private static TResult ByValue<T, TResult>(Result<T> result, Func<T, TResult> map, TResult fallback) =>
        result.TryGetValue(out var value) ? map(value) : fallback;

    private static TResult ViaIn<T, TResult>(in Result<T> result, Func<T, TResult> map, TResult fallback) =>
        result.TryGetValue(out var value) ? map(value) : fallback;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TResult ByValueNotInlined<T, TResult>(Result<T> result, Func<T, TResult> map, TResult fallback) =>
        result.TryGetValue(out var value) ? map(value) : fallback;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TResult ViaInNotInlined<T, TResult>(in Result<T> result, Func<T, TResult> map, TResult fallback) =>
        result.TryGetValue(out var value) ? map(value) : fallback;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UntypedByValue(Result result, int success, int failure) => result.IsSuccess ? success : failure;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UntypedViaIn(in Result result, int success, int failure) => result.IsSuccess ? success : failure;

    // 64 bytes: eight longs.
    public readonly struct Large64(long seed)
    {
        public long A { get; } = seed;

        public long B { get; } = seed + 1;

        public long C { get; } = seed + 2;

        public long D { get; } = seed + 3;

        public long E { get; } = seed + 4;

        public long F { get; } = seed + 5;

        public long G { get; } = seed + 6;

        public long H { get; } = seed + 7;
    }
}
