using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using CsfeCustomer = CSharpFunctionalExtensions.Maybe<Wyzely.Functional.MaybeBenchmarks.Customer>;
using CsfeInt = CSharpFunctionalExtensions.Maybe<int>;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Csfe() => MakeCsfe(_number).Value;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int CsfeRef() => MakeCsfeRef(_customer).Value.Name.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CsfeInt MakeCsfe(int value) => CsfeInt.From(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CsfeCustomer MakeCsfeRef(Customer value) => CsfeCustomer.From(value);
}
