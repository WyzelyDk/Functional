namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: compare, sort, de-duplicate and print maybes.
//
// WHY: a maybe is a value, so it behaves like one: two somes are equal when their values are, every none is equal to every other none
// (and to default), none sorts before every some, and ToString() never throws.
public static class EqualityExample
{
    public static void Run(TextWriter output)
    {
        var five = Maybe.Some(5);
        var none = Maybe.None<int>();

        output.WriteLine($"Some(5) == Some(5): {five == Maybe.Some(5)}");
        output.WriteLine($"Some(5) == Some(6): {five == Maybe.Some(6)}");
        output.WriteLine($"Some(5) == None: {five == none}");
        output.WriteLine($"None == default: {none == default}");
        output.WriteLine($"Equals(object): {five.Equals((object)Maybe.Some(5))}");

        // Ordering: none is the smallest; somes sort by their values (the type of the value must be comparable).
        output.WriteLine($"None < Some(1): {none < Maybe.Some(1)}");
        output.WriteLine($"Some(1) < Some(2): {Maybe.Some(1) < Maybe.Some(2)}");

        var sorted = new List<Maybe<int>> { Maybe.Some(3), none, Maybe.Some(1) };
        sorted.Sort();
        output.WriteLine($"Sorted: {string.Join(", ", sorted)}");

        // Equal maybes have equal hash codes, so they work as keys and in sets.
        HashSet<Maybe<int>> set = [Maybe.Some(1), Maybe.Some(1), none, default];
        output.WriteLine($"Distinct in a HashSet: {set.Count}");
        var first = Maybe.Some(7);
        var second = Maybe.Some(7);
        output.WriteLine($"Equal values have equal hash codes: {first.GetHashCode() == second.GetHashCode()}");

        // Formatting: the text is a setting (see ConfigurationExample), and the default is Some(value) and None.
        Maybe<int> unassigned = default;
        output.WriteLine($"ToString of a some: {five}");
        output.WriteLine($"ToString of a none: {none}");
        output.WriteLine($"ToString of a default: {unassigned}");
        output.WriteLine($"Interpolated into text: Customer: {SampleData.FindCustomer(1).Map(customer => customer.Name)}");
    }
}
