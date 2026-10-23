using System.Globalization;

namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: a value that may be absent, without null.
//
// WHY: "there may be no customer" is an ordinary situation, and null says it badly: nothing in the type tells the reader, and a missing
// check is found at run time. A Maybe<T> puts the absence in the signature. This example creates a maybe in every way the library
// offers, reads it in the ways that cannot fail, and shows the one way that can (GetValue), and why that one is a method.
public static class BasicsExample
{
    public static void Run(TextWriter output)
    {
        // ---- Creating a maybe ----------------------------------------------------------------------------------------------

        // Some says "there is a value". It rejects null with an ArgumentNullException, so a null that slipped in is loud.
        var some = Maybe.Some("Ada");
        output.WriteLine($"Maybe.Some(\"Ada\"): {some}");

        // None says "there is no value". The type argument says what would have been there.
        var none = Maybe.None<string>();
        output.WriteLine($"Maybe.None<string>(): {none}");

        // From says "this may be null": a null reference becomes none instead of an exception.
        var missing = default(string);
        var present = (string?)"Ada";
        output.WriteLine($"Maybe.From(null reference): {Maybe.From(missing)}");
        output.WriteLine($"Maybe.From(non-null reference): {Maybe.From(present)}");

        // From also accepts a nullable value type such as int?.
        int? score = 12;
        output.WriteLine($"Maybe.From(int? with a value): {Maybe.From(score)}");

        // SomeIf / NoneIf: a value only when (or unless) a condition holds.
        output.WriteLine($"Maybe.SomeIf(true, 3): {Maybe.SomeIf(true, 3)}");
        output.WriteLine($"Maybe.SomeIf(false, 3): {Maybe.SomeIf(false, 3)}");
        output.WriteLine($"Maybe.NoneIf(true, 3): {Maybe.NoneIf(true, 3)}");
        output.WriteLine($"Maybe.NoneIf(false, 3): {Maybe.NoneIf(false, 3)}");

        // The implicit conversion: a plain value converts to a maybe and a null reference becomes none, so "return customer;" works in
        // a method that returns Maybe<Customer>. This is the one implicit conversion in the library, and it is deliberate: null
        // already means "no value". Nothing converts the other way; you have to say how to leave a maybe.
        Maybe<string> converted = "Ada";
        Maybe<string> convertedNull = missing;
        output.WriteLine($"Implicit conversion of a value: {converted}");
        output.WriteLine($"Implicit conversion of null: {convertedNull}");

        // default is none. A struct always has a default value, and here that value is a perfectly good "no value", so a field or an
        // array slot that was never assigned is not an error.
        Maybe<int> unassigned = default;
        output.WriteLine($"default(Maybe<int>) is none: {unassigned.IsNone}");

        output.WriteLine($"Maybe.Some(null) throws: {ExceptionNameOf(() => Maybe.Some(missing!))}");

        // ---- Reading a maybe the safe way ----------------------------------------------------------------------------------

        // TryGetValue is the non-throwing way. It is annotated, so after "if (...)" the compiler knows that name is not null.
        if (some.TryGetValue(out var name))
        {
            output.WriteLine($"TryGetValue: {name} has {name.Length.ToString(CultureInfo.InvariantCulture)} letters");
        }

        // ValueOr: the value, or a fallback.
        output.WriteLine($"ValueOr on a some: {some.ValueOr("nobody")}");
        output.WriteLine($"ValueOr on a none: {none.ValueOr("nobody")}");

        // Match: handle both cases and get one value out. It is the way to leave a maybe when the two cases need different code.
        output.WriteLine($"Match on a some: {some.Match(value => $"Hello, {value}", () => "Hello, stranger")}");
        output.WriteLine($"Match on a none: {none.Match(value => $"Hello, {value}", () => "Hello, stranger")}");

        // ---- GetValue: the one reading that can throw ----------------------------------------------------------------------

        // GetValue is a method, not a property, because it can throw: a property that throws for a state the caller can reach is a
        // trap. When the package is installed, the analyzer WYZ0201 reports a GetValue() that is not guarded by IsSome / IsNone of the
        // same maybe. (This project references the source of the library, so the analyzer, which ships inside the NuGet package, does not
        // run on this file; in your own project the unguarded call below would be a warning.)
        if (some.IsSome)
        {
            output.WriteLine($"GetValue after an IsSome check: {some.GetValue()}");
        }

        output.WriteLine($"GetValue on a none throws: {ExceptionNameOf(() => none.GetValue())}");
    }

    private static string ExceptionNameOf(Action action)
    {
        try
        {
            action();

            return "nothing";
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }
}
