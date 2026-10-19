namespace Wyzely.Functional.MaybeBenchmarks;

// The reference-type payload of the benchmarks. The chains read its name and never allocate a new payload, so the allocation columns
// show what the maybe type costs and not what the payload costs.
internal sealed class Customer(string name)
{
    public string Name { get; } = name;
}
