using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OptionalLib() => MakeOptional(_number).ValueOr(-1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OptionalRef() => MakeOptionalRef(_customer).Match(static c => c.Name.Length, static () => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<int> MakeOptional(int value) => Option.Some(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<Customer> MakeOptionalRef(Customer value) => Option.Some(value);
}
