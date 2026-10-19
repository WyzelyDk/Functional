using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class Try
{
    // Funcky has no Try wrapper: the exception is caught and becomes the error of the Result, by hand.
    [Benchmark]
    [BenchmarkCategory("ValueSuccess")]
    public int FunckyLib() => RunFuncky(static () => Parse("21")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ValueFailure")]
    public int FunckyFailure() => RunFuncky(static () => Parse("x")).Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceSuccess")]
    public int FunckyRef() => RunFuncky(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("ReferenceFailure")]
    public int FunckyRefFailure() => RunFuncky(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);

    private static Result<T> RunFuncky<T>(Func<T> function)
        where T : notnull
    {
        try
        {
            return Result.Ok(function());
        }
        catch (Exception exception)
        {
            return Result<T>.Error(exception);
        }
    }
}
