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

        var maybeSyncMapped = maybe.Map(n => n + 1);
        var maybeSync = Is(maybeSyncMapped);
        await output.WriteLineAsync($"Maybe receiver, sync delegate: {maybeSync}");
        var maybeTaskMapped = maybe.Map(n => Task.FromResult(n + 1));
        var maybeTask = await Is(maybeTaskMapped);
        await output.WriteLineAsync($"Maybe receiver, Task delegate: {maybeTask}");
        var maybeValueTaskMapped = maybe.Map(n => new ValueTask<int>(n + 1));
        var maybeValueTask = await Is(maybeValueTaskMapped);
        await output.WriteLineAsync($"Maybe receiver, ValueTask delegate: {maybeValueTask}");

        var taskReceiverSync = TaskReceiver(maybe);
        var taskSyncMapped = taskReceiverSync.Map(n => n + 1);
        var taskSync = await Is(taskSyncMapped);
        await output.WriteLineAsync($"Task receiver, sync delegate: {taskSync}");
        var taskReceiverTask = TaskReceiver(maybe);
        var taskTaskMapped = taskReceiverTask.Map(n => Task.FromResult(n + 1));
        var taskTask = await Is(taskTaskMapped);
        await output.WriteLineAsync($"Task receiver, Task delegate: {taskTask}");
        var taskReceiverValueTask = TaskReceiver(maybe);
        var taskValueTaskMapped = taskReceiverValueTask.Map(n => new ValueTask<int>(n + 1));
        var taskValueTask = await Is(taskValueTaskMapped);
        await output.WriteLineAsync($"Task receiver, ValueTask delegate: {taskValueTask}");

        var valueTaskReceiverSync = ValueTaskReceiver(maybe);
        var valueTaskSyncMapped = valueTaskReceiverSync.Map(n => n + 1);
        var valueTaskSync = await Is(valueTaskSyncMapped);
        await output.WriteLineAsync($"ValueTask receiver, sync delegate: {valueTaskSync}");
        var valueTaskReceiverTask = ValueTaskReceiver(maybe);
        var valueTaskTaskMapped = valueTaskReceiverTask.Map(n => Task.FromResult(n + 1));
        var valueTaskTask = await Is(valueTaskTaskMapped);
        await output.WriteLineAsync($"ValueTask receiver, Task delegate: {valueTaskTask}");
        var valueTaskReceiverValueTask = ValueTaskReceiver(maybe);
        var valueTaskValueTaskMapped = valueTaskReceiverValueTask.Map(n => new ValueTask<int>(n + 1));
        var valueTaskValueTask = await Is(valueTaskValueTaskMapped);
        await output.WriteLineAsync($"ValueTask receiver, ValueTask delegate: {valueTaskValueTask}");

        // ---- One chain that uses a Task step, a ValueTask step and synchronous steps ------------------------------------------
        // There is exactly one await, where the caller reads the result; none sits between the steps.
        var messageForAda = await MailMessageAsync(1);
        await output.WriteLineAsync($"Mixed chain, customer 1: {messageForAda}");
        var messageForBob = await MailMessageAsync(2);
        await output.WriteLineAsync($"Mixed chain, customer 2 (no email): {messageForBob}");
        var messageForCleo = await MailMessageAsync(3);
        await output.WriteLineAsync($"Mixed chain, customer 3 (inactive): {messageForCleo}");
        var messageForUnknown = await MailMessageAsync(99);
        await output.WriteLineAsync($"Mixed chain, customer 99 (unknown): {messageForUnknown}");

        // An async lambda converts to both a Task and a ValueTask delegate.
        // The library marks the Task overloads with [OverloadResolutionPriority], so with C# 13 or later this compiles without ambiguity.
        var doubled = await Maybe.Some(21).Bind(async number => await ScaleAsync(number, 2));
        await output.WriteLineAsync($"Async lambda: {doubled}");

        // A none skips the delegate, including an asynchronous one: it is never called.
        var called = false;
        var absent = Maybe.None<int>();
        var absentTask = Task.FromResult(absent);
        _ = await absentTask.Map(RecordCallAsync);
        await output.WriteLineAsync($"A none skips the Task delegate: {called}");

        async Task<int> RecordCallAsync(int number)
        {
            called = true;
            await Task.Yield();

            return number;
        }
    }

    private static async Task<Maybe<int>> ScaleAsync(int number, int factor)
    {
        await Task.Yield();

        return Maybe.Some(number * factor);
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
