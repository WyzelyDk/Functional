using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Fluent() => MakeFluent(_number).Value;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FluentRef() => MakeFluentRef(_customer).Value.Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeFluent(int value) => Result.Ok(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeFluentRef(Customer value) => Result.Ok(value);
}
