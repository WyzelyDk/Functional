using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => MakeFuncky(_number).Match(none: static () => -1, some: static x => x);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyRef() => MakeFunckyRef(_customer).Match(none: static () => -1, some: static c => c.Name.Length);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<int> MakeFuncky(int value) => Option.Some(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<Customer> MakeFunckyRef(Customer value) => Option.Some(value);
}
