namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: read the optional properties of a customer (a nickname that is a string?, an age that is an int?).
//
// WHY: the nullable annotations are part of the API. A Maybe<T> requires T : notnull, because absence is what none is for; a
// Maybe<string?> would have two ways to be empty. This example shows how a nullable reference and a nullable value type enter a
// chain, and what the compiler says about each.
public static class NullabilityExample
{
    public static void Run(TextWriter output)
    {
        var ada = SampleData.FindCustomer(1);
        var bob = SampleData.FindCustomer(2);

        // A Map delegate may return a nullable REFERENCE: it compiles without a warning, the result type is Maybe<string> (not Maybe<string?>),
        // and a null result becomes none at run time.
        var nicknameSet = ada.Map(customer => customer.Nickname);
        output.WriteLine($"Map to a string? property, nickname set: {nicknameSet}");
        var noNickname = bob.Map(customer => customer.Nickname);
        output.WriteLine($"Map to a string? property, no nickname: {noNickname}");
        var nullResult = ada.Map(static customer => (string?)null);
        output.WriteLine($"Map delegate that returns null becomes none: {nullResult}");

        // A nullable VALUE type behaves the same way: Map has an overload for a delegate that returns int?,
        // so the result is a Maybe<int> (never a Maybe<int?>) and a null becomes none.
        var ageKnown = ada.Map(customer => customer.Age);
        output.WriteLine($"Map to an int? property, age known: {ageKnown}");
        var ageUnknown = bob.Map(customer => customer.Age);
        output.WriteLine($"Map to an int? property, age unknown: {ageUnknown}");

        // The explicit form gives the same answer: Maybe.From turns an int? into a Maybe<int>.
        var boundAgeKnown = ada.Bind(customer => Maybe.From(customer.Age));
        output.WriteLine($"Bind with Maybe.From on an int? property, age known: {boundAgeKnown}");
        var boundAgeUnknown = bob.Bind(customer => Maybe.From(customer.Age));
        output.WriteLine($"Bind with Maybe.From on an int? property, age unknown: {boundAgeUnknown}");

        // Maybe.From has an overload for nullable references and one for nullable value types.
        int? withValue = 12;
        int? withoutValue = null;
        var fromWithValue = Maybe.From(withValue);
        output.WriteLine($"Maybe.From(int? with a value): {fromWithValue}");
        var fromWithoutValue = Maybe.From(withoutValue);
        output.WriteLine($"Maybe.From(int? without a value): {fromWithoutValue}");

        // The way back for a value type: AsNullable.
        var nullableSome = Maybe.Some(12).AsNullable();
        var shownSome = Show(nullableSome);
        output.WriteLine($"AsNullable of a some: {shownSome}");
        var nullableNone = Maybe.None<int>().AsNullable();
        var shownNone = Show(nullableNone);
        output.WriteLine($"AsNullable of a none: {shownNone}");

        // The implicit conversion accepts a string? without a warning: a null becomes none.
        var nothing = default(string);
        var something = (string?)"Ada";
        Maybe<string> fromNull = nothing;
        Maybe<string> fromValue = something;
        output.WriteLine($"Implicit conversion of a string? that is null: {fromNull}");
        output.WriteLine($"Implicit conversion of a string? that is set: {fromValue}");

        // Not shown, because it does not compile cleanly on purpose: Maybe<string?> reports CS8714, as string? does not satisfy the notnull constraint.
        // TryGetValue is annotated with [MaybeNullWhen(false)], so after a successful call the value is known to be non-null.
    }

    private static string Show(int? value) => value is null ? "null" : value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
