using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool Fluent() => MakeFluent().IsFailed;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool FluentRef() => MakeFluentRef().IsFailed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeFluent() => Result.Fail<int>("Rejected.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeFluentRef() => Result.Fail<Customer>("Rejected.");
}
