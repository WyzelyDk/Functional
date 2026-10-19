namespace Wyzely.Functional.MaybeSamples;

// The small domain that every example shares: a customer directory with lookups that may find nothing. Nothing in it is specific to
// this library; it is the kind of code that has "maybe there is one" questions everywhere.
internal sealed record Customer(int Id, string Name, string? Email, bool IsActive, string? Nickname, int? Age);

internal static class SampleData
{
    private static readonly Customer[] Customers =
    [
        new(1, "Ada", "ada@example.com", true, "Countess", 36),
        new(2, "Bob", null, true, null, null),
        new(3, "Cleo", "cleo@example.com", false, null, 41),
    ];

    internal static IReadOnlyList<Customer> AllCustomers => Customers;

    // The lookup returns a Maybe instead of a Customer that may be null: the signature says "there may be no customer".
    internal static Maybe<Customer> FindCustomer(int id) => Customers.FirstOrNone(customer => customer.Id == id);

    // Maybe.From turns the null of a missing email into none.
    internal static Maybe<string> FindEmail(Customer customer) => Maybe.From(customer.Email);

    internal static Task<Maybe<Customer>> FindCustomerAsync(int id) => Task.FromResult(FindCustomer(id));

    internal static ValueTask<Maybe<string>> FindEmailAsync(Customer customer) => new(FindEmail(customer));
}
