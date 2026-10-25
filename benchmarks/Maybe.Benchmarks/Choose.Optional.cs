using BenchmarkDotNet.Attributes;
using Optional;
using Optional.Collections;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Choose
{
    // Optional has Values on a sequence of Option.
    private readonly Option<int>[] _optionallibNumbers = OptionalLibMake(Data.Number);
    private readonly Option<string>[] _optionallibNames = OptionalLibMake(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OptionalLib() => Total(_optionallibNumbers.Values());

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OptionalLibRef() => Total(_optionallibNames.Values());

    private static Option<T>[] OptionalLibMake<T>(T value)
        where T : notnull
    {
        var maybes = new Option<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            maybes[index] = index % 2 == 0 ? Option.Some(value) : Option.None<T>();
        }

        return maybes;
    }
}
