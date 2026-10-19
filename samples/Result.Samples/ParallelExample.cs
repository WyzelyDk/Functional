// PARALLEL: SelectInParallel and ForEachInParallel.
//
// Scenario: load or process many orders at once, without flooding a service with hundreds of simultaneous calls.
//
// WHY these helpers exist: the usual way to do this is an array of results that tasks fill in a loop. An element
// that is never filled in is a default result, which counts as a success and hides the work that was never done, and getting
// the error handling right is fiddly.
// These helpers never expose an unfilled slot, and they follow four rules:
//  1. BOUNDED parallelism: you say how many operations run at the same time (or -1 for no limit).
//  2. EVERY item is attempted: one failure does not hide the failures of the others.
//  3. Values and errors come back in SOURCE order, not in the order the operations happened to finish.
//  4. An EXCEPTION thrown by the operation is not turned into a result: it escapes. Wrap the operation in
//     Result.Try to turn it into a failure. Cancellation stops items that have not started and surfaces as an
//     OperationCanceledException.
namespace Wyzely.Functional.ResultSamples;

public static class ParallelExample
{
    private static readonly int[] Found = [7, 8, 12];
    private static readonly int[] FoundAndMissing = [7, 98, 8, 99];
    private static readonly int[] Reservable = [7, 12];
    private static readonly int[] ReservableAndShort = [7, 11];
    private static readonly int[] TwoOrders = [7, 8];
    private static readonly int[] One = [1];
    private static readonly int[] OneMissing = [7, 98];

    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    public static async Task RunAsync(TextWriter output)
    {
        // Values come back as one result holding the list, in source order.
        var found = await Found.SelectInParallel(
            SampleShop.FindOrderAsync,
            maxDegreeOfParallelism: 2);
        var inOrder = found.Match(orders => string.Join(", ", orders.Select(order => order.Id)), error => error.Message);
        await output.WriteLineAsync("Values in source order: " + inOrder);

        // Two lookups fail. Order 98 is deliberately SLOWER than order 99, yet the errors come back in source order.
        // Every one of the four lookups was attempted, and no more than two ran at the same time.
        var gauge = new ConcurrencyGauge();
        var some = await FoundAndMissing.SelectInParallel(
            async (id, token) =>
            {
                gauge.Enter();

                try
                {
                    await Task.Delay(id == 98 ? 30 : 1, token);

                    return await SampleShop.FindOrderAsync(id, token);
                }
                finally
                {
                    gauge.Exit();
                }
            },
            maxDegreeOfParallelism: 2);
        await output.WriteLineAsync("Errors in source order: " + some.Match(_ => "all found", error => error.Message));
        await output.WriteLineAsync($"Lookups attempted: {gauge.Calls}");
        await output.WriteLineAsync($"Never more than 2 at the same time: {gauge.Peak <= 2}");

        // ForEachInParallel is for operations that return no value. This one returns a ValueTask<Result>.
        var reserved = await Reservable.ForEachInParallel(
            (id, token) => new ValueTask<Result>(SampleShop.FindOrder(id).Bind(SampleShop.ReserveStock)));
        await output.WriteLineAsync($"ForEachInParallel (ValueTask): {reserved}");

        var notReserved = await ReservableAndShort.ForEachInParallel(
            (id, token) => SampleShop.FindOrderAsync(id, token).Bind(SampleShop.ReserveStock));
        await output.WriteLineAsync("ForEachInParallel with a failure: " + notReserved.Match(() => "ok", error => error.Message));

        // Cancellation stops the work and surfaces as an exception, not as a failed result.
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        try
        {
            _ = await TwoOrders.SelectInParallel(
                SampleShop.FindOrderAsync,
                cancellationToken: cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            await output.WriteLineAsync("Cancellation surfaces as an exception: True");
        }

        // An exception thrown by the operation escapes...
        try
        {
            _ = await One.SelectInParallel(ExplodeAsync);
        }
        catch (InvalidOperationException exception)
        {
            await output.WriteLineAsync($"An exception in the operation escapes: {exception.GetType().Name}");
        }

        // ...unless the operation is wrapped in Result.Try, which turns it into a failure of that item.
        var wrapped = await One.SelectInParallel((id, token) => Result.Try(() => ExplodeAsync(id, token)));
        await output.WriteLineAsync($"Wrapped in Result.Try it becomes a failure: kind {(wrapped.TryGetError(out var error) ? error.Kind : "none")}");

        // Result.Combine also accepts a sequence of tasks: it waits for all of them and reports every failure.
        // Unlike SelectInParallel it does not limit how many run at once; the tasks have already started.
        var combined = await Result.Combine(OneMissing.Select(id => SampleShop.FindOrderAsync(id)));
        await output.WriteLineAsync("Combine of tasks: " + combined.Match(_ => "all found", problem => problem.Message));
    }

    private static async Task<Result<int>> ExplodeAsync(int id, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);

        throw new InvalidOperationException($"The database is down (item {id}).");
    }
}
