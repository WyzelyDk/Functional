namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: an email directory. Looking a customer up by email may find nobody, and the lookup says so with a Maybe. The caller, though,
// is a use case that cannot go on without the customer, so for it "nobody" is a failure that has to be described.
//
// WHY: a Maybe knows only "there is no value". It cannot know whether that is a typo (NotFound), a broken order (Unexpected) or
// something else, because that depends on who asks. ToResult is the one place where the caller says what the absence means, and the
// library deliberately invents no error for you.
public static class AbsentToFailedExample
{
    // An Error is immutable, so one instance can be shared by every call. When the error is the same every time, build it once.
    private static readonly Error NoSuchCustomer = Error.NotFound("No customer with that email address.");

    public static void Run(TextWriter output)
    {
        var directory = new CustomerDirectory();
        var found = directory.FindByEmail("ada@example.com");
        var missing = directory.FindByEmail("nobody@example.com");

        // 1. ToResult(Error): the error already exists, so it costs nothing to pass it. Use it for a constant error (the static field
        //    above) or when you already hold the error. An Error you build inline is built on every call, found or not.
        Describe(output, "Eager error, customer found", found.ToResult(NoSuchCustomer));
        Describe(output, "Eager error, customer missing", missing.ToResult(NoSuchCustomer));

        // 2. ToResult(Func<Error>): the factory runs only when the maybe is none, so the error (with its details) is built only
        //    for the failure. Use it when the error carries data from this call. The email is marked sensitive: Message shows it,
        //    RedactedMessage and ToRedactedString() hide it, which is what you want in a log.
        var factoryCalls = 0;

        Result<Customer> Lookup(string email) => directory.FindByEmail(email).ToResult(() =>
        {
            factoryCalls++;

            return Error.NotFound("No customer with email {Email}.").WithDetail("Email", email, isSensitive: true);
        });

        Describe(output, "Lazy error, customer found", Lookup("ada@example.com"));
        var lazyFailure = Lookup("nobody@example.com");
        Describe(output, "Lazy error, customer missing", lazyFailure);
        var redacted = lazyFailure.Match(_ => string.Empty, error => error.RedactedMessage);
        output.WriteLine($"Lazy error, redacted message hides the email: {!redacted.Contains("nobody", StringComparison.Ordinal)}");
        output.WriteLine($"Error factory calls: {factoryCalls}");

        // 3. The same absence can mean different things, so the kind is yours to choose. The kind is what a caller (an API layer,
        //    a retry policy) switches on; it is not text.
        var typed = missing.ToResult(NoSuchCustomer);
        var broken = missing.ToResult(() => Error.Unexpected("Order {OrderId} points at a customer that does not exist.").WithDetail("OrderId", 17));
        output.WriteLine($"Kind for an email somebody typed: {typed.Match(_ => "none", error => error.Kind.ToString())}");
        output.WriteLine($"Kind for an order that points at a missing customer: {broken.Match(_ => "none", error => error.Kind.ToString())}");

        // 4. A null error is a programming mistake and throws when you call ToResult, not later. (A Func<Error> that returns null
        //    throws too.)
        output.WriteLine($"A null error throws: {NameOfThrown(() => missing.ToResult((Error)null!))}");
    }

    private static void Describe(TextWriter output, string label, Result<Customer> result) =>
        output.WriteLine($"{label}: {result.Match(customer => $"success {customer.Name}", error => $"failure [{error.Kind}] {error.Message}")}");

    private static string NameOfThrown(Action action)
    {
        try
        {
            action();

            return "nothing";
        }
        catch (ArgumentNullException exception)
        {
            return exception.GetType().Name;
        }
    }
}
