using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using CsfeCustomer = CSharpFunctionalExtensions.Maybe<Wyzely.Functional.MaybeBenchmarks.Customer>;
using CsfeInt = CSharpFunctionalExtensions.Maybe<int>;
using CsfeString = CSharpFunctionalExtensions.Maybe<string>;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncToAsync
{
    private static readonly Task<CsfeInt> CsfeDone = Task.FromResult(CsfeInt.From(43));
    private static readonly Task<CsfeString> CsfeDoneRef = Task.FromResult(CsfeString.From(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Csfe()
    {
        var maybe = await CsfeInt.From(_number)
            .Bind(static x => CsfeDone)
            .Map(static x => x + 1);

        return maybe.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> CsfeRef()
    {
        var maybe = await CsfeCustomer.From(_customer)
            .Bind(static c => CsfeDoneRef)
            .Map(static n => n.Length);

        return maybe.Value;
    }
}
