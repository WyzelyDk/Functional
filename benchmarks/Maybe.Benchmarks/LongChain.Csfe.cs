using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using CsfeCustomer = CSharpFunctionalExtensions.Maybe<Wyzely.Functional.MaybeBenchmarks.Customer>;
using CsfeInt = CSharpFunctionalExtensions.Maybe<int>;
using CsfeString = CSharpFunctionalExtensions.Maybe<string>;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class LongChain
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Csfe() => CsfeInt.From(_number)
        .Map(static x => x + 1)
        .Where(static x => x > 0)
        .Bind(static x => CsfeInt.From(x * 2))
        .Map(static x => x - 3)
        .Where(static x => x != 0)
        .Bind(static x => CsfeInt.From(x + 10))
        .Map(static x => x * 2)
        .Match(static x => x, static () => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int CsfeRef() => CsfeCustomer.From(_customer)
        .Map(static c => c.Name)
        .Where(static n => n.Length > 0)
        .Bind(static n => CsfeString.From(n))
        .Map(static n => n)
        .Where(static n => n[0] != ' ')
        .Bind(static n => CsfeString.From(n))
        .Map(static n => n)
        .Match(static n => n.Length, static () => -1);
}
