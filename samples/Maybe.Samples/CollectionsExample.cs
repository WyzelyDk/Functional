namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: ask a sequence or a dictionary a "maybe there is one" question.
//
// WHY: First, Single, ElementAt and the dictionary indexer throw when the answer is "nothing"; FirstOrDefault returns null for a
// reference type and 0 for an int, which cannot be told from a real answer. The *OrNone methods answer with a Maybe, so the answer
// "nothing" is part of the type and joins the railway.
public static class CollectionsExample
{
    public static void Run(TextWriter output)
    {
        int[] numbers = [4, 8, 15, 16];

        output.WriteLine($"FirstOrNone(n > 10): {numbers.FirstOrNone(n => n > 10)}");
        output.WriteLine($"FirstOrNone(n > 100): {numbers.FirstOrNone(n => n > 100)}");
        output.WriteLine($"FirstOrNone() on an empty sequence: {Array.Empty<int>().FirstOrNone()}");
        output.WriteLine($"LastOrNone(): {numbers.LastOrNone()}");

        // SingleOrNone is none when there are NONE or SEVERAL matches. Single would throw in both cases, and the two cases mean
        // different things, so use it only when "not exactly one" is one answer for you.
        output.WriteLine($"SingleOrNone(n > 15): {numbers.SingleOrNone(n => n > 15)}");
        output.WriteLine($"SingleOrNone(n > 10), two match: {numbers.SingleOrNone(n => n > 10)}");
        output.WriteLine($"SingleOrNone(n > 100), none match: {numbers.SingleOrNone(n => n > 100)}");

        // ElementAtOrNone: an index outside the sequence, including a negative one, is none.
        output.WriteLine($"ElementAtOrNone(2): {numbers.ElementAtOrNone(2)}");
        output.WriteLine($"ElementAtOrNone(10): {numbers.ElementAtOrNone(10)}");
        output.WriteLine($"ElementAtOrNone(-1): {numbers.ElementAtOrNone(-1)}");

        // GetValueOrNone: a dictionary lookup that does not throw and does not need the "out" variable of TryGetValue. The price is in
        // cents, so the line below does not depend on the culture of the machine.
        var priceInCents = new Dictionary<string, int> { ["tea"] = 350, ["coffee"] = 400 };
        output.WriteLine($"GetValueOrNone(\"tea\"): {priceInCents.GetValueOrNone("tea")}");
        output.WriteLine($"GetValueOrNone(\"water\"): {priceInCents.GetValueOrNone("water")}");

        // Choose keeps the values of the somes, in order, and drops the nones.
        IEnumerable<Maybe<int>> lookups = [Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3)];
        output.WriteLine($"Choose(): {string.Join(", ", lookups.Choose())}");

        // Choose with a selector maps every element to a Maybe first. Here: the emails of all the customers that have one. It is the
        // loop "foreach, look up, if found add" written as one line, and it is lazy like LINQ's Select.
        var emails = SampleData.AllCustomers.Choose(SampleData.FindEmail);
        output.WriteLine($"Choose(selector), emails of all customers: {string.Join(", ", emails)}");
    }
}
