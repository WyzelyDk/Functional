using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class Create
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OptionalLib() => MakeOptional(_number).ValueOr(-1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OptionalRef() => MakeOptionalRef(_customer).Match(static c => c.Name.Length, static _ => -1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<int, string> MakeOptional(int value) => Option.Some<int, string>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<Customer, string> MakeOptionalRef(Customer value) => Option.Some<Customer, string>(value);
}
