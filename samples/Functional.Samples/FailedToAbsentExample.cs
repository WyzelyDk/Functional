namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: customers are validated (a Result: found? allowed?). One screen only needs the customers that passed, for a mailing list.
// It does not care why the others did not.
//
// WHY: ToMaybe is the way back from "failed" to "absent". It is lossy on purpose: the error is dropped. Use it only where the
// reason really does not matter; if anyone will ask "why is there nothing?", stay with the Result.
public static class FailedToAbsentExample
{
    private static readonly string[] ShownEmails = ["ada@example.com", "linus@example.com", "nobody@example.com"];

    private static readonly string[] MailingList = ["ada@example.com", "linus@example.com", "grace@example.com", "nobody@example.com"];

    public static void Run(TextWriter output)
    {
        var directory = new CustomerDirectory();

        // The validation: a lookup (Maybe) made into a Result, then a rule on top of it.
        Result<Customer> Validate(string email) => directory.FindByEmail(email)
            .ToResult(Error.NotFound("No customer with that email address."))
            .Ensure(customer => customer.IsActive, Error.Forbidden("The customer is blocked."));

        foreach (var email in ShownEmails)
        {
            var result = Validate(email);
            var maybe = result.ToMaybe();

            output.WriteLine($"Result for {email}: {result.Match(customer => $"success {customer.Name}", error => $"failure [{error.Kind}] {error.Message}")}");
            output.WriteLine($"Maybe for {email}: {maybe.Match(customer => customer.Name, () => "none")}");
        }

        // What is dropped: two different failures become the same none. Nothing in the maybe tells them apart.
        var blocked = Validate("linus@example.com").ToMaybe();
        var missing = Validate("nobody@example.com").ToMaybe();
        output.WriteLine($"A blocked and a missing customer look the same as maybes: {blocked == missing}");

        // A success whose value is null becomes none, because a Maybe cannot hold null. A Result<T> can end up holding null when
        // the value comes from code that is not null-aware (the ! below says "trust me", which is how it happens in practice).
        string? nicknameColumn = null;
        var withNull = Result.Success(nicknameColumn!);
        output.WriteLine($"Success with a null value: IsSuccess is {withNull.IsSuccess}, ToMaybe is none: {withNull.ToMaybe().IsNone}");

        // The usual reason to come back to Maybe: keep what worked and skip the rest. ToMaybe + Choose is "the successes".
        var passed = MailingList
            .Select(email => Validate(email).ToMaybe())
            .Choose()
            .Select(customer => customer.Name);
        output.WriteLine($"Customers that passed validation: {string.Join(", ", passed)}");

        // ToMaybe also exists for Task and ValueTask receivers, so it can end an async chain.
        var fromTask = CustomerNameAsync(directory).GetAwaiter().GetResult();
        output.WriteLine($"From Task<Result<Customer>>: {fromTask}");
    }

    private static async Task<string> CustomerNameAsync(CustomerDirectory directory) =>
        await directory.FindAsync(1)
            .ToResult(Error.NotFound("No such customer."))
            .ToMaybe()
            .Map(customer => customer.Name)
            .ValueOr(() => "nobody");
}
