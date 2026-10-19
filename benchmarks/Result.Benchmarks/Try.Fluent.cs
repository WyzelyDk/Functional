using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class Try
{
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int Fluent() => Fold(Result.Try(static () => Parse("21")));

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int FluentFailure() => Fold(Result.Try(static () => Parse("x")));

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int FluentRef() => FoldRef(Result.Try(static () => Find("ada")));

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int FluentRefFailure() => FoldRef(Result.Try(static () => Find(string.Empty)));

    private static int Fold(Result<int> result) => result.IsSuccess ? result.Value : -1;

    private static int FoldRef(Result<Customer> result) => result.IsSuccess ? result.Value.Name.Length : -1;
}
