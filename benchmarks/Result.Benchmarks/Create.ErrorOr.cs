using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int ErrorOrLib() => MakeErrorOr(_number).Value;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ErrorOrRef() => MakeErrorOrRef(_customer).Value.Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ErrorOr<int> MakeErrorOr(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ErrorOr<Customer> MakeErrorOrRef(Customer value) => value;
}
