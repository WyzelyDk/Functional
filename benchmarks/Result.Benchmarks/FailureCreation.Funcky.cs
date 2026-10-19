using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    // The error of a Funcky Result is an Exception, created here but not thrown by this code.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool FunckyLib() => MakeFuncky().Match(static _ => false, static _ => true);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool FunckyRef() => MakeFunckyRef().Match(static _ => false, static _ => true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<int> MakeFuncky() => Result<int>.Error(new InvalidOperationException("Rejected."));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Result<Customer> MakeFunckyRef() => Result<Customer>.Error(new InvalidOperationException("Rejected."));
}
