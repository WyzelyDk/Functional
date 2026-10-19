using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class Try
{
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int Csfe() => Result.Try(static () => Parse("21")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int CsfeFailure() => Result.Try(static () => Parse("x")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int CsfeRef() => Result.Try(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int CsfeRefFailure() => Result.Try(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);
}
