using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using ErrorOr;

namespace Wyzely.Benchmarks;

public partial class FailureCreation
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public bool ErrorOrLib() => MakeErrorOr().IsError;

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public bool ErrorOrRef() => MakeErrorOrRef().IsError;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ErrorOr<int> MakeErrorOr() => Error.Failure("Rejected", "Rejected.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ErrorOr<Customer> MakeErrorOrRef() => Error.Failure("Rejected", "Rejected.");
}
