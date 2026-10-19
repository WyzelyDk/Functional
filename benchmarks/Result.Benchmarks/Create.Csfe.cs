using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Csfe() => MakeCsfe(_number).Value;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int CsfeRef() => MakeCsfeRef(_customer).Value.Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeCsfe(int value) => Result.Success(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeCsfeRef(Customer value) => Result.Success(value);
}
