using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Benchmarks;

public partial class Combine
{
    // OneOf is a union, not a railway library: the loop collects the values and returns at the first failure.
    private readonly OneOf<int, Error<string>>[] _oneOfNumbers = OneOfMake(Data.Number, fail: false);
    private readonly OneOf<int, Error<string>>[] _oneOfNumbersFailed = OneOfMake(Data.Number, fail: true);
    private readonly OneOf<string, Error<string>>[] _oneOfNames = OneOfMake(Data.Customer.Name, fail: false);
    private readonly OneOf<string, Error<string>>[] _oneOfNamesFailed = OneOfMake(Data.Customer.Name, fail: true);

    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int OneOfLib() => OneOfCombine(_oneOfNumbers);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int OneOfLibFailure() => OneOfCombine(_oneOfNumbersFailed);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int OneOfLibRef() => OneOfCombine(_oneOfNames);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int OneOfLibRefFailure() => OneOfCombine(_oneOfNamesFailed);

    private static OneOf<T, Error<string>>[] OneOfMake<T>(T value, bool fail)
    {
        var results = new OneOf<T, Error<string>>[Count];

        for (var index = 0; index < Count; index++)
        {
            results[index] = fail && index == FailingIndex ? new Error<string>("Rejected.") : value;
        }

        return results;
    }

    private static int OneOfCombine<T>(OneOf<T, Error<string>>[] results)
    {
        var values = new List<T>(Count);

        foreach (var result in results)
        {
            if (!result.IsT0)
            {
                return -1;
            }

            values.Add(result.AsT0);
        }

        return values.Count;
    }
}
