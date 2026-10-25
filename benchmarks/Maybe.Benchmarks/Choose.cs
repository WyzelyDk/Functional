using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Functional.MaybeBenchmarks;

// Keeping the values of the maybes that have one, from eight maybes of which every second is none, and counting them. Wyzely and CSharpFunctionalExtensions
// have Choose; the other libraries are measured with their nearest equivalent. The maybes exist in advance, and every library counts the values with the same
// loop, so that the columns show the cost of choosing. The reference case chooses the name of a Customer.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class Choose
{
    private const int Count = 8;

    private static int Total<T>(IEnumerable<T> values)
    {
        var total = 0;

        foreach (var value in values)
        {
            total += value is string text ? text.Length : 1;
        }

        return total;
    }
}
