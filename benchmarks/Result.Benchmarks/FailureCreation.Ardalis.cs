using System.Runtime.CompilerServices;
using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool Ardalis() => !MakeArdalis().IsSuccess;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool ArdalisRef() => !MakeArdalisRef().IsSuccess;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeArdalis() => Result<int>.Error("Rejected.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeArdalisRef() => Result<Customer>.Error("Rejected.");
}
