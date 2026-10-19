using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class Try
{
    // Ardalis.Result has no Try wrapper: the exception is caught and turned into an error by hand.
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int Ardalis() => RunArdalis(static () => Parse("21")) is { IsSuccess: true } result ? result.Value : -1;

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int ArdalisFailure() => RunArdalis(static () => Parse("x")) is { IsSuccess: true } result ? result.Value : -1;

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int ArdalisRef() => RunArdalis(static () => Find("ada")) is { IsSuccess: true } result ? result.Value.Name.Length : -1;

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int ArdalisRefFailure() => RunArdalis(static () => Find(string.Empty)) is { IsSuccess: true } result ? result.Value.Name.Length : -1;

    private static Result<T> RunArdalis<T>(Func<T> function)
    {
        try
        {
            return Result<T>.Success(function());
        }
        catch (Exception exception)
        {
            return Result<T>.Error(exception.Message);
        }
    }
}
