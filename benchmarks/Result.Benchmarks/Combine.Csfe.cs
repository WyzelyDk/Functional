using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // Result.Combine takes the results and gives the values, or the errors joined into one text.
    private readonly Result<int>[] _csfeNumbers = CsfeMake(Data.Number, fail: false);
    private readonly Result<int>[] _csfeNumbersFailed = CsfeMake(Data.Number, fail: true);
    private readonly Result<string>[] _csfeNames = CsfeMake(Data.Customer.Name, fail: false);
    private readonly Result<string>[] _csfeNamesFailed = CsfeMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int Csfe() => CsfeCount(_csfeNumbers);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int CsfeFailure() => CsfeCount(_csfeNumbersFailed);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int CsfeRef() => CsfeCount(_csfeNames);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int CsfeRefFailure() => CsfeCount(_csfeNamesFailed);

    private static Result<T>[] CsfeMake<T>(T value, bool fail)
    {
        var results = new Result<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? Result.Failure<T>("Rejected.") : Result.Success(value);
        }

        return results;
    }

    private static int CsfeCount<T>(Result<T>[] results) => results.AsEnumerable().Combine().Match(static x => x.Count(), static _ => -1);
}
