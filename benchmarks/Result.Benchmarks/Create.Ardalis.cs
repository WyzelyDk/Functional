using System.Runtime.CompilerServices;
using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Ardalis() => MakeArdalis(_number).Value;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ArdalisRef() => MakeArdalisRef(_customer).Value.Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeArdalis(int value) => Result<int>.Success(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeArdalisRef(Customer value) => Result<Customer>.Success(value);
}
