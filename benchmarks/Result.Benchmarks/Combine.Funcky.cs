using BenchmarkDotNet.Attributes;
using Funcky.Extensions;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // Sequence turns a sequence of Result<T> into a Result of the sequence of T. The error of a Funcky Result is an Exception.
    private static readonly Exception FunckyRejected = new InvalidOperationException("Rejected.");

    private readonly Result<int>[] _funckyNumbers = FunckyMake(Data.Number, fail: false);
    private readonly Result<int>[] _funckyNumbersFailed = FunckyMake(Data.Number, fail: true);
    private readonly Result<string>[] _funckyNames = FunckyMake(Data.Customer.Name, fail: false);
    private readonly Result<string>[] _funckyNamesFailed = FunckyMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int FunckyLib() => _funckyNumbers.Sequence().Match(static x => x.Count, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int FunckyLibFailure() => _funckyNumbersFailed.Sequence().Match(static x => x.Count, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int FunckyLibRef() => _funckyNames.Sequence().Match(static x => x.Count, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int FunckyLibRefFailure() => _funckyNamesFailed.Sequence().Match(static x => x.Count, static _ => -1);

    private static Result<T>[] FunckyMake<T>(T value, bool fail)
        where T : notnull
    {
        var results = new Result<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Result<T>.Error(FunckyRejected) : Result.Ok(value);
        }

        return results;
    }
}
