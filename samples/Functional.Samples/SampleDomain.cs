namespace Wyzely.Functional.FunctionalSamples;

// The small domain that the examples share: customers who can be looked up (a lookup may find nothing, so it returns a Maybe) and a
// payment gateway that may fail (so it returns a Result). Everything is in memory, so the examples run anywhere and print the same lines
// every time.
internal sealed record Customer(int Id, string Name, string Email, bool IsActive, int CreditLimit);

internal sealed record Receipt(string Number, int Amount);

internal sealed class CustomerDirectory
{
    private readonly Dictionary<int, Customer> _byId;
    private readonly Dictionary<string, Customer> _byEmail;

    internal CustomerDirectory()
    {
        Customer[] customers =
        [
            new(1, "Ada Lovelace", "ada@example.com", IsActive: true, CreditLimit: 500),
            new(2, "Grace Hopper", "grace@example.com", IsActive: true, CreditLimit: 100),
            new(3, "Linus Torvalds", "linus@example.com", IsActive: false, CreditLimit: 500),
            new(5, "Alan Turing", "alan@example.com", IsActive: true, CreditLimit: 1000),
        ];

        _byId = customers.ToDictionary(customer => customer.Id);
        _byEmail = customers.ToDictionary(customer => customer.Email, StringComparer.OrdinalIgnoreCase);
    }

    // GetValueOrNone turns "the dictionary has no such key" into a Maybe, so there is no TryGetValue and no null.
    internal Maybe<Customer> FindByEmail(string email) => _byEmail.GetValueOrNone(email);

    // A lookup that really is asynchronous: Task.Yield makes the returned task incomplete when the caller gets it.
    internal async Task<Maybe<Customer>> FindAsync(int id)
    {
        await Task.Yield();

        return _byId.GetValueOrNone(id);
    }
}

internal sealed class PaymentGateway
{
    internal int Calls { get; private set; }

    // The gateway fails for customer 5, the way a real one is sometimes unreachable.
    internal async ValueTask<Result<Receipt>> ChargeAsync(Customer customer, int amount)
    {
        Calls++;
        await Task.Yield();

        return customer.Id == 5
            ? Result.Failure<Receipt>(Error.Unavailable("The payment gateway did not answer."))
            : Result.Success(new Receipt($"R-{customer.Id}", amount));
    }
}
