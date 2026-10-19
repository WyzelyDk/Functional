namespace Wyzely.Functional.FunctionalSamples;

// SCENARIO: a customer pays an amount. The customer is looked up (asynchronous, may find nobody), must be active, must have enough
// credit (a plain synchronous rule), is charged by a payment gateway (asynchronous, may fail), and the outcome becomes a message.
//
// WHY: those steps return five different shapes: Task<Maybe<T>>, Maybe<T>, Result<T>, ValueTask<Result<T>> and a plain value. The
// library has the same method names for all of them, so the whole workflow is ONE expression with no await in the middle and no if
// for the failure. The first step that fails (or finds nothing) skips every step after it.
public static class AsyncWorkflowExample
{
    private static readonly Error NoActiveCustomer = Error.NotFound("No active customer with that id.");

    // 1 pays fine; 2 has too little credit; 3 is inactive; 4 does not exist; 5 is rejected by the gateway.
    private static readonly (int CustomerId, int Amount)[] Payments = [(1, 250), (2, 250), (3, 100), (4, 100), (5, 100)];

    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    private static async Task RunAsync(TextWriter output)
    {
        var directory = new CustomerDirectory();
        var gateway = new PaymentGateway();

        foreach (var (customerId, amount) in Payments)
        {
            var outcome = await PayAsync(directory, gateway, customerId, amount);
            await output.WriteLineAsync($"Customer {customerId} pays {amount}: {outcome}");
        }

        // Customers 3 and 4 never reached the gateway, and customer 2 was stopped by the credit rule before it: only 1 and 5 did.
        await output.WriteLineAsync($"Payment gateway calls: {gateway.Calls}");
    }

    // What each line of the chain is, top to bottom:
    //   FindAsync     Task<Maybe<Customer>>: may find nobody.
    //   Filter        still a Task<Maybe<Customer>>: an inactive customer becomes none.
    //   ToResult      Task<Result<Customer>>: none now carries a described error (this is the umbrella package).
    //   Bind (credit) a synchronous step that can fail, inside an asynchronous chain.
    //   Bind (charge) a ValueTask<Result<Receipt>> step: still no await.
    //   Map           runs only when everything before it succeeded.
    //   Match         the one place where the chain ends: both outcomes become a string.
    private static async Task<string> PayAsync(CustomerDirectory directory, PaymentGateway gateway, int customerId, int amount) =>
        await directory.FindAsync(customerId)
            .Filter(customer => customer.IsActive)
            .ToResult(NoActiveCustomer)
            .Bind(customer => CheckCredit(customer, amount))
            .Bind(customer => gateway.ChargeAsync(customer, amount))
            .Map(receipt => $"{receipt.Number} for {receipt.Amount}")
            .Match(
                text => $"Paid: {text}",
                error => $"Failed [{error.Kind}] {error.Message}{(error.IsTransient ? " Retry later." : string.Empty)}");

    private static Result<Customer> CheckCredit(Customer customer, int amount)
    {
        if (amount <= customer.CreditLimit)
        {
            return Result.Success(customer);
        }

        var error = Error.Validation("Credit limit {Limit} is below the amount {Amount}.")
            .WithDetail("Limit", customer.CreditLimit)
            .WithDetail("Amount", amount);

        return Result.Failure<Customer>(error);
    }
}
