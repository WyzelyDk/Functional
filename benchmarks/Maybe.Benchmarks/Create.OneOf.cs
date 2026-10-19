using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib() => MakeOneOf(_number).AsT0;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef() => MakeOneOfRef(_customer).AsT0.Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static OneOf<int, None> MakeOneOf(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static OneOf<Customer, None> MakeOneOfRef(Customer value) => value;
}
