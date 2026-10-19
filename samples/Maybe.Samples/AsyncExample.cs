namespace Wyzely.Functional.MaybeSamples;

// SCENARIO: the same railway when some steps are synchronous and some are asynchronous.
//
// WHY: a chain such as FindCustomerAsync(...).Filter(...).Bind(...).Map(...) should not need an await in the middle just because one
// step is synchronous and the next returns a ValueTask. Every operation exists for a receiver that is a Maybe, a Task<Maybe<T>> or a
// ValueTask<Maybe<T>>, times a delegate that is synchronous, returns a Task or returns a ValueTask: nine variants under ONE name,
// with no "Async" suffix. The return type follows the receiver; a synchronous receiver takes the kind of the delegate.
public static class AsyncExample
{
    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    private static async Task RunAsync(TextWriter output)
    {
        // ---- The nine combinations of Map, written out one by one ---------------------------------------------------------
        // Is only returns its argument: its three overloads make the compiler check that each call returned the type of the table in the docs.
        // Task and ValueTask receivers are created fresh for each call (a ValueTask must be consumed once), and each result is awaited
        // where it is printed.
        var maybe = Maybe.Some(1);

        await output.WriteLineAsync($"Maybe receiver, sync delegate: {Is(maybe.Map(n => n + 1))}");
        await output.WriteLineAsync($"Maybe receiver, Task delegate: {await Is(maybe.Map(n => Task.FromResult(n + 1)))}");
        await output.WriteLineAsync($"Maybe receiver, ValueTask delegate: {await Is(maybe.Map(n => new ValueTask<int>(n + 1)))}");

        await output.WriteLineAsync($"Task receiver, sync delegate: {await Is(TaskReceiver(maybe).Map(n => n + 1))}");
        await output.WriteLineAsync($"Task receiver, Task delegate: {await Is(TaskReceiver(maybe).Map(n => Task.FromResult(n + 1)))}");
        await output.WriteLineAsync($"Task receiver, ValueTask delegate: {await Is(TaskReceiver(maybe).Map(n => new ValueTask<int>(n + 1)))}");

        await output.WriteLineAsync($"ValueTask receiver, sync delegate: {await Is(ValueTaskReceiver(maybe).Map(n => n + 1))}");
        await output.WriteLineAsync($"ValueTask receiver, Task delegate: {await Is(ValueTaskReceiver(maybe).Map(n => Task.FromResult(n + 1)))}");
        await output.WriteLineAsync($"ValueTask receiver, ValueTask delegate: {await Is(ValueTaskReceiver(maybe).Map(n => new ValueTask<int>(n + 1)))}");

        // ---- One chain that uses a Task step, a ValueTask step and synchronous steps ------------------------------------------
        // There is exactly one await, where the caller reads the result; none sits between the steps.
        await output.WriteLineAsync($"Mixed chain, customer 1: {await MailMessageAsync(1)}");
        await output.WriteLineAsync($"Mixed chain, customer 2 (no email): {await MailMessageAsync(2)}");
        await output.WriteLineAsync($"Mixed chain, customer 3 (inactive): {await MailMessageAsync(3)}");
        await output.WriteLineAsync($"Mixed chain, customer 99 (unknown): {await MailMessageAsync(99)}");

        // An async lambda converts to both a Task and a ValueTask delegate. The library marks the Task overloads with
        // [OverloadResolutionPriority], so with C# 13 or later this compiles without ambiguity.
        var doubled = await Maybe.Some(21).Bind(async number =>
        {
            await Task.Yield();

            return Maybe.Some(number * 2);
        });
        await output.WriteLineAsync($"Async lambda: {doubled}");

        // A none skips the delegate, including an asynchronous one: it is never called.
        var called = false;
        _ = await Task.FromResult(Maybe.None<int>()).Map(async number =>
        {
            called = true;
            await Task.Yield();

            return number;
        });
        await output.WriteLineAsync($"A none skips the Task delegate: {called}");
    }

    private static Maybe<int> Is(Maybe<int> value) => value;

    private static Task<Maybe<int>> Is(Task<Maybe<int>> value) => value;

    private static ValueTask<Maybe<int>> Is(ValueTask<Maybe<int>> value) => value;

    private static Task<Maybe<int>> TaskReceiver(Maybe<int> maybe) => Task.FromResult(maybe);

    private static ValueTask<Maybe<int>> ValueTaskReceiver(Maybe<int> maybe) => new(maybe);

    // FindCustomerAsync is a Task step, Filter a synchronous step on a Task receiver, Bind a ValueTask step, Map synchronous again.
    private static Task<string> MailMessageAsync(int customerId) =>
        SampleData.FindCustomerAsync(customerId)
            .Filter(customer => customer.IsActive)
            .Bind(SampleData.FindEmailAsync)
            .Map(email => email.Trim())
            .Match(email => $"Mail sent to {email}", () => "Nobody to mail");
}
