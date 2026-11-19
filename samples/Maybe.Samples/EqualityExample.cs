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

        var anotherFive = Maybe.Some(5);
        output.WriteLine($"Some(5) == Some(5): {five == anotherFive}");
        var six = Maybe.Some(6);
        output.WriteLine($"Some(5) == Some(6): {five == six}");
        output.WriteLine($"Some(5) == None: {five == none}");
        output.WriteLine($"None == default: {none == default}");
        var boxedFive = (object)Maybe.Some(5);
        var equalsObject = five.Equals(boxedFive);
        output.WriteLine($"Equals(object): {equalsObject}");

        // Ordering: none is the smallest; somes sort by their values (the type of the value must be comparable).
        var one = Maybe.Some(1);
        output.WriteLine($"None < Some(1): {none < one}");
        var two = Maybe.Some(2);
        output.WriteLine($"Some(1) < Some(2): {one < two}");

        var sorted = new List<Maybe<int>> { Maybe.Some(3), none, Maybe.Some(1) };
        sorted.Sort();
        var sortedText = string.Join(", ", sorted);
        output.WriteLine($"Sorted: {sortedText}");

        // Equal maybes have equal hash codes, so they work as keys and in sets.
        HashSet<Maybe<int>> set = [Maybe.Some(1), Maybe.Some(1), none, default];
        output.WriteLine($"Distinct in a HashSet: {set.Count}");
        var first = Maybe.Some(7);
        var second = Maybe.Some(7);
        var firstHashCode = first.GetHashCode();
        var secondHashCode = second.GetHashCode();
        output.WriteLine($"Equal values have equal hash codes: {firstHashCode == secondHashCode}");

        // Your own equality: pass a comparer for the values, or create one for a collection.
        var ada = Maybe.Some("Ada");
        var loudAda = Maybe.Some("ADA");
        var ignoreCase = StringComparer.OrdinalIgnoreCase;
        var sameIgnoringCase = ada.Equals(loudAda, ignoreCase);
        output.WriteLine($"Equal ignoring case: {sameIgnoringCase}");
        var containsIgnoringCase = ada.Contains("ADA", ignoreCase);
        output.WriteLine($"Contains ignoring case: {containsIgnoringCase}");
        var namesComparer = Maybe.CreateEqualityComparer<string>(ignoreCase);
        var names = new HashSet<Maybe<string>>(namesComparer) { ada, loudAda };
        output.WriteLine($"Distinct names ignoring case: {names.Count}");

        // Formatting: the text is a setting (see ConfigurationExample), and the default is Some(value) and None.
        Maybe<int> unassigned = default;
        output.WriteLine($"ToString of a some: {five}");
        output.WriteLine($"ToString of a none: {none}");
        output.WriteLine($"ToString of a default: {unassigned}");
        var customerName = SampleData.FindCustomer(1).Map(customer => customer.Name);
        output.WriteLine($"Interpolated into text: Customer: {customerName}");
    }
}
