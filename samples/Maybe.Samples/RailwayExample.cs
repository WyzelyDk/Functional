namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: look up a customer and mail them, where every step can find nothing.
//
// WHY: with null, a chain of lookups becomes a ladder of "if (x is null) return null;". With a maybe, each step is a method call, and the
// first none skips every step after it. A none never runs a delegate that expects a value, so the delegates do not have to check.
public static class RailwayExample
{
    public static void Run(TextWriter output)
    {
        var ada = SampleData.FindCustomer(1);
        var bob = SampleData.FindCustomer(2);
        var cleo = SampleData.FindCustomer(3);

        // Map: transform the value, keep the shape. A none stays a none and the delegate does not run.
        var upperCaseName = ada.Map(customer => customer.Name.ToUpperInvariant());
        output.WriteLine($"Map: {upperCaseName}");

        // Bind: the next step itself may find nothing, so it returns a Maybe. Map would give a Maybe<Maybe<string>>; Bind flattens.
        var adaEmail = ada.Bind(SampleData.FindEmail);
        output.WriteLine($"Bind, customer with an email: {adaEmail}");
        var bobEmail = bob.Bind(SampleData.FindEmail);
        output.WriteLine($"Bind, customer without an email: {bobEmail}");

        // Filter: keep the value only when a condition holds, otherwise the result is none.
        var activeName = ada.Filter(customer => customer.IsActive).Map(customer => customer.Name);
        output.WriteLine($"Filter, active customer: {activeName}");
        var inactiveName = cleo.Filter(customer => customer.IsActive).Map(customer => customer.Name);
        output.WriteLine($"Filter, inactive customer: {inactiveName}");

        // Tap runs a side effect (logging, metrics) for a some and hands back the same maybe; TapNone does the same for a none.
        var tapRanOnSome = false;
        var tapRanOnNone = false;
        var tapNoneRanOnNone = false;
        var tapNoneRanOnSome = false;

        _ = ada.Tap(_ => tapRanOnSome = true).TapNone(() => tapNoneRanOnSome = true);
        _ = Maybe.None<Customer>().Tap(_ => tapRanOnNone = true).TapNone(() => tapNoneRanOnNone = true);

        output.WriteLine($"Tap on a some ran: {tapRanOnSome}");
        output.WriteLine($"Tap on a none ran: {tapRanOnNone}");
        output.WriteLine($"TapNone on a none ran: {tapNoneRanOnNone}");
        output.WriteLine($"TapNone on a some ran: {tapNoneRanOnSome}");

        // OrElse: an alternative that is only tried when there is no value, and that is itself a Maybe, so alternatives can be chained.
        var alternativeUsed = bob.Bind(SampleData.FindEmail).OrElse(() => Maybe.Some("support@example.com"));
        output.WriteLine($"OrElse, alternative used: {alternativeUsed}");
        var alternativeNotNeeded = ada.Bind(SampleData.FindEmail).OrElse(() => Maybe.Some("support@example.com"));
        output.WriteLine($"OrElse, alternative not needed: {alternativeNotNeeded}");

        // ValueOr with a delegate leaves the railway: the value, or what the delegate computes (only when it is needed).
        // The instance method ValueOr("text") takes a plain value instead.
        var emailOrFallback = bob.Bind(SampleData.FindEmail).ValueOr(() => "no email on file");
        output.WriteLine($"ValueOr(Func): {emailOrFallback}");

        // Match leaves the railway with one value out; both cases are handled, so nothing is forgotten.
        var adaInstruction = ada.Bind(SampleData.FindEmail).Match(email => $"Send to {email}", () => "Nobody to mail");
        output.WriteLine($"Match, some: {adaInstruction}");
        var bobInstruction = bob.Bind(SampleData.FindEmail).Match(email => $"Send to {email}", () => "Nobody to mail");
        output.WriteLine($"Match, none: {bobInstruction}");

        // Switch leaves the railway for side effects: one action per case, no value out.
        ada.Switch(customer => output.WriteLine($"Switch, some: found {customer.Name}"), () => output.WriteLine("Switch, none: nobody found"));
        Maybe.None<Customer>().Switch(
            customer => output.WriteLine($"Switch, some: found {customer.Name}"),
            () => output.WriteLine("Switch, none: nobody found"));

        // The whole railway, for four different customers. The method below has no null check and no early return; every way to find nothing (no such customer,
        // inactive, no email) ends in the same "Nobody to mail".
        var messageForAda = MailMessage(1);
        output.WriteLine($"Whole chain, customer 1: {messageForAda}");
        var messageForBob = MailMessage(2);
        output.WriteLine($"Whole chain, customer 2 (no email): {messageForBob}");
        var messageForCleo = MailMessage(3);
        output.WriteLine($"Whole chain, customer 3 (inactive): {messageForCleo}");
        var messageForUnknown = MailMessage(99);
        output.WriteLine($"Whole chain, customer 99 (unknown): {messageForUnknown}");
    }

    private static string MailMessage(int customerId) =>
        SampleData.FindCustomer(customerId)
            .Filter(customer => customer.IsActive)
            .Bind(SampleData.FindEmail)
            .Map(email => email.Trim())
            .Match(email => $"Mail sent to {email}", () => "Nobody to mail");
}
