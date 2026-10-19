using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Wyzely.Benchmarks;

// Turning code that can throw into a result: a call that succeeds and a call that throws, for an int (parsing text) and for a reference
// type (looking up a Customer). A library without a Try wrapper is measured with the try/catch its user would write instead. A throw
// costs microseconds whatever the library, so the failure columns are dominated by the exception.
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public partial class Try
{
    private static int Parse(string text) => int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    private static Customer Find(string id) => id.Length == 0 ? throw new ArgumentException("No id.", nameof(id)) : Data.Customer;
}
