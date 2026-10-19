using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool OneOfLib() => MakeOneOf().IsT1;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool OneOfRef() => MakeOneOfRef().IsT1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static OneOf<int, Error<string>> MakeOneOf() => new Error<string>("Rejected.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static OneOf<Customer, Error<string>> MakeOneOfRef() => new Error<string>("Rejected.");
}
