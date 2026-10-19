namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: combine several maybes: dependent ones with query syntax, independent ones with Zip, nested ones with Flatten.
//
// WHY: query syntax ("from ... in ... select") reads well when each step needs the values of the earlier ones. Zip combines maybes that
// do not depend on each other: the result is a some only when ALL of them are (there is no error to aggregate, so the answer is
// "all" or "none"). Flatten removes one level when a Map delegate returned a maybe.
public static class LinqExample
{
    public static void Run(TextWriter output)
    {
        output.WriteLine($"Query syntax, customer 1: {Describe(1)}");
        output.WriteLine($"Query syntax, customer 2 (no email): {Describe(2)}");
        output.WriteLine($"Query syntax, customer 3 (inactive): {Describe(3)}");

        // Zip: two to five maybes. Without a selector the result is a tuple with named elements (First, Second, ...).
        var customerName = SampleData.FindCustomer(1).Map(customer => customer.Name);
        var quantity = Maybe.Some(3);
        output.WriteLine($"Zip into a tuple: {customerName.Zip(quantity)}");

        // With a selector the values are combined at once; a null that the selector returns becomes none, like in Map.
        var unitPrice = Maybe.Some(4);
        output.WriteLine($"Zip with a selector: {unitPrice.Zip(quantity, (price, count) => price * count)}");
        output.WriteLine($"Zip with a missing side: {unitPrice.Zip(Maybe.None<int>(), (price, count) => price * count)}");

        // Flatten: Map with a delegate that itself returns a Maybe gives a Maybe<Maybe<T>>; Flatten removes one level.
        var nested = Maybe.Some(1).Map(SampleData.FindCustomer);
        output.WriteLine($"Flatten: {nested.Flatten().Map(customer => customer.Name)}");
        output.WriteLine($"Flatten of an inner none: {Maybe.Some(99).Map(SampleData.FindCustomer).Flatten().Map(customer => customer.Name)}");
        output.WriteLine($"Flatten of an outer none: {Maybe.None<int>().Map(SampleData.FindCustomer).Flatten().Map(customer => customer.Name)}");

        // Flatten is Bind with the identity function, so when you are writing the step yourself, Bind says the same without nesting.
        output.WriteLine($"Bind says the same without nesting: {Maybe.Some(1).Bind(SampleData.FindCustomer).Map(customer => customer.Name)}");
    }

    private static Maybe<string> Describe(int customerId) =>
        from customer in SampleData.FindCustomer(customerId)
        from email in SampleData.FindEmail(customer)
        where customer.IsActive
        select $"{customer.Name} <{email}>";
}
