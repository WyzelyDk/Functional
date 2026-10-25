using BenchmarkDotNet.Attributes;
using OneOf;
using OneOf.Types;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Choose
{
    // OneOf is a union, not a railway library: OneOf<T, None> stands for the maybe, and the values are collected with a loop.
    private readonly OneOf<int, None>[] _oneofNumbers = OneOfMake(Data.Number);
    private readonly OneOf<string, None>[] _oneofNames = OneOfMake(Data.Customer.Name);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int OneOfLib() => Total(OneOfChoose(_oneofNumbers));

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int OneOfRef() => Total(OneOfChoose(_oneofNames));

    private static OneOf<T, None>[] OneOfMake<T>(T value)
    {
        var maybes = new OneOf<T, None>[Count];

        for (var index = 0; index < Count; index++)
        {
            maybes[index] = index % 2 == 0 ? value : default(None);
        }

        return maybes;
    }

    private static List<T> OneOfChoose<T>(OneOf<T, None>[] maybes)
    {
        var values = new List<T>(Count);

        foreach (var maybe in maybes)
        {
            if (maybe.IsT0)
            {
                values.Add(maybe.AsT0);
            }
        }

        return values;
    }
}
