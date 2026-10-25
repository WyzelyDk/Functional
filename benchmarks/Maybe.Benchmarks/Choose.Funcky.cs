using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Choose
{
    // Funcky has no Choose: the options that have a value are turned into sequences of one, which SelectMany joins.
    private readonly Option<int>[] _funckylibNumbers = FunckyLibMake(Data.Number);
    private readonly Option<string>[] _funckylibNames = FunckyLibMake(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => Total(_funckylibNumbers.SelectMany(static o => o.ToEnumerable()));

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyLibRef() => Total(_funckylibNames.SelectMany(static o => o.ToEnumerable()));

    private static Option<T>[] FunckyLibMake<T>(T value)
        where T : notnull
    {
        var maybes = new Option<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            maybes[index] = index % 2 == 0 ? Option.Some(value) : Option<T>.None;
        }

        return maybes;
    }
}
