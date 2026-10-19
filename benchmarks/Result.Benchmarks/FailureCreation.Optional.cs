using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Optional;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool OptionalLib() => !MakeOptional().HasValue;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool OptionalRef() => !MakeOptionalRef().HasValue;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<int, string> MakeOptional() => Option.None<int, string>("Rejected.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Option<Customer, string> MakeOptionalRef() => Option.None<Customer, string>("Rejected.");
}
