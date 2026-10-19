using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public bool Wyzely() => MakeWyzely().IsFailure;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public bool WyzelyRef() => MakeWyzelyRef().IsFailure;

    // A rich error: a kind, a message template with a detail, and a context frame. Other libraries have no counterpart.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool WyzelyRich() => MakeWyzelyRich().IsFailure;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeWyzely() => Result.Failure<int>(Error.Create("Rejected."));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeWyzelyRef() => Result.Failure<Customer>(Error.Create("Rejected."));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeWyzelyRich() =>
        Result.Failure<int>(Error.Validation("{Name} was rejected.").WithDetail("Name", "x").WithContext("Benchmark"));
}
