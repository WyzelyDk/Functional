using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => MakeFuncky(_number).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyRef() => MakeFunckyRef(_customer).Match(static c => c.Name.Length, static _ => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeFuncky(int value) => Result.Ok(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeFunckyRef(Customer value) => Result.Ok(value);
}
