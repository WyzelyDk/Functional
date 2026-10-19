using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Create
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => MakeWyzely(_number).GetValue();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => MakeWyzelyRef(_customer).GetValue().Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Maybe<int> MakeWyzely(int value) => Maybe.Some(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Maybe<Customer> MakeWyzelyRef(Customer value) => Maybe.Some(value);
}
