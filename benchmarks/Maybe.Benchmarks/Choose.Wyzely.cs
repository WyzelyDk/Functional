using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Choose
{
    private readonly Maybe<int>[] _wyzelyNumbers = WyzelyMake(Data.Number);
    private readonly Maybe<string>[] _wyzelyNames = WyzelyMake(Data.Customer.Name);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => Total(_wyzelyNumbers.Choose());

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => Total(_wyzelyNames.Choose());

    private static Maybe<T>[] WyzelyMake<T>(T value)
        where T : notnull
    {
        var maybes = new Maybe<T>[Count];

        for (var index = 0; index < Count; index++)
        {
            maybes[index] = index % 2 == 0 ? Maybe.Some(value) : Maybe.None<T>();
        }

        return maybes;
    }
}
