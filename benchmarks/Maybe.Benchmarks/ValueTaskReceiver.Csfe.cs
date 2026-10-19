using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using CsfeCustomer = CSharpFunctionalExtensions.Maybe<Wyzely.Functional.MaybeBenchmarks.Customer>;
using CsfeInt = CSharpFunctionalExtensions.Maybe<int>;
using CsfeString = CSharpFunctionalExtensions.Maybe<string>;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class ValueTaskReceiver
{
    private readonly ValueTask<CsfeInt> _csfeTask = new(CsfeInt.From(Data.Number));
    private readonly ValueTask<CsfeCustomer> _csfeTaskRef = new(CsfeCustomer.From(Data.Customer));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Csfe()
    {
        var maybe = await _csfeTask
            .Map(static x => x + 1)
            .Where(static x => x > 0)
            .Bind(static x => CsfeInt.From(x * 2));

        return maybe.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> CsfeRef()
    {
        var maybe = await _csfeTaskRef
            .Map(static c => c.Name)
            .Where(static n => n.Length > 0)
            .Bind(static n => CsfeString.From(n));

        return maybe.Value.Length;
    }
}
