using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => MakeWyzely(_number).GetValue();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => MakeWyzelyRef(_customer).GetValue().Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeWyzely(int value) => Result.Success(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeWyzelyRef(Customer value) => Result.Success(value);
}
