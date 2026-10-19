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
        output.WriteLine($"Map: {ada.Map(customer => customer.Name.ToUpperInvariant())}");

        // Bind: the next step itself may find nothing, so it returns a Maybe. Map would give a Maybe<Maybe<string>>; Bind flattens.
        output.WriteLine($"Bind, customer with an email: {ada.Bind(SampleData.FindEmail)}");
        output.WriteLine($"Bind, customer without an email: {bob.Bind(SampleData.FindEmail)}");

        // Filter: keep the value only when a condition holds, otherwise the result is none.
        output.WriteLine($"Filter, active customer: {ada.Filter(customer => customer.IsActive).Map(customer => customer.Name)}");
        output.WriteLine($"Filter, inactive customer: {cleo.Filter(customer => customer.IsActive).Map(customer => customer.Name)}");

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

        // Or: an alternative that is only tried when there is no value, and that is itself a Maybe, so alternatives can be chained.
        output.WriteLine($"Or, alternative used: {bob.Bind(SampleData.FindEmail).Or(() => Maybe.Some("support@example.com"))}");
        output.WriteLine($"Or, alternative not needed: {ada.Bind(SampleData.FindEmail).Or(() => Maybe.Some("support@example.com"))}");

        // ValueOr with a delegate leaves the railway: the value, or what the delegate computes (only when it is needed). The instance
        // method ValueOr("text") takes a plain value instead.
        output.WriteLine($"ValueOr(Func): {bob.Bind(SampleData.FindEmail).ValueOr(() => "no email on file")}");

        // Match leaves the railway with one value out; both cases are handled, so nothing is forgotten.
        output.WriteLine($"Match, some: {ada.Bind(SampleData.FindEmail).Match(email => $"Send to {email}", () => "Nobody to mail")}");
        output.WriteLine($"Match, none: {bob.Bind(SampleData.FindEmail).Match(email => $"Send to {email}", () => "Nobody to mail")}");

        // Switch leaves the railway for side effects: one action per case, no value out.
        ada.Switch(customer => output.WriteLine($"Switch, some: found {customer.Name}"), () => output.WriteLine("Switch, none: nobody found"));
        Maybe.None<Customer>().Switch(
            customer => output.WriteLine($"Switch, some: found {customer.Name}"),
            () => output.WriteLine("Switch, none: nobody found"));

        // The whole railway, for four different customers. The method below has no null check and no early return; every way to find
        // nothing (no such customer, inactive, no email) ends in the same "Nobody to mail".
        output.WriteLine($"Whole chain, customer 1: {MailMessage(1)}");
        output.WriteLine($"Whole chain, customer 2 (no email): {MailMessage(2)}");
        output.WriteLine($"Whole chain, customer 3 (inactive): {MailMessage(3)}");
        output.WriteLine($"Whole chain, customer 99 (unknown): {MailMessage(99)}");
    }

    private static string MailMessage(int customerId) =>
        SampleData.FindCustomer(customerId)
            .Filter(customer => customer.IsActive)
            .Bind(SampleData.FindEmail)
            .Map(email => email.Trim())
            .Match(email => $"Mail sent to {email}", () => "Nobody to mail");
}
