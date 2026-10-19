using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool Csfe() => MakeCsfe().IsFailure;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool CsfeRef() => MakeCsfeRef().IsFailure;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeCsfe() => Result.Failure<int>("Rejected.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeCsfeRef() => Result.Failure<Customer>("Rejected.");
}
