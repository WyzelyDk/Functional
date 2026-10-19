#if NET11_0_OR_GREATER
namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: handle a maybe with a switch that the compiler checks for completeness (C# 15 unions, net11.0 only).
//
// WHY: on net11.0 a Maybe<T> is a union of its case type T and the absence of a value, which a pattern sees as null. A switch with an arm for
// T and an arm for null is therefore exhaustive: no discard arm is needed, and if you forget one of the two the compiler tells you.
// It needs the .NET 11 SDK; the library adds the net11.0 target only when such an SDK builds it.
public static class UnionExample
{
    public static void Run(TextWriter output)
    {
        output.WriteLine($"Switch on a some: {Describe(SampleData.FindCustomer(1))}");
        output.WriteLine($"Switch on a none: {Describe(SampleData.FindCustomer(99))}");

        // A default maybe is none, so it matches the null arm.
        output.WriteLine($"Switch on a default maybe: {Describe(default)}");

        // A union gives the language an implicit conversion from its case type (the library already had one from T).
        Maybe<int> converted = 5;
        output.WriteLine($"Implicit conversion from the case type: {converted}");

        // The arms are ordinary patterns, so they can be nested: here a property pattern on the customer.
        output.WriteLine($"Switch with a nested pattern, active customer: {Mailability(SampleData.FindCustomer(1))}");
        output.WriteLine($"Switch with a nested pattern, inactive customer: {Mailability(SampleData.FindCustomer(3))}");
        output.WriteLine($"Switch with a nested pattern, nobody: {Mailability(SampleData.FindCustomer(99))}");
    }

    private static string Describe(Maybe<Customer> maybe) => maybe switch
    {
        Customer customer => $"found {customer.Name}",
        null => "nobody",
    };

    private static string Mailability(Maybe<Customer> maybe) => maybe switch
    {
        Customer { IsActive: true } customer => $"{customer.Name} can be mailed",
        Customer customer => $"{customer.Name} is inactive",
        null => "nobody",
    };
}
#endif
