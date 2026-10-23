// ASYNC: plain, Task and ValueTask in one chain, with no await in the middle.
//
// Scenario: checking out an order where the lookup returns a Task, reserving stock returns a Task, charging the
// card returns a ValueTask, and the validation in between is plain synchronous code.
//
// WHY it matters: with most Result libraries a chain like this needs an `await` between the steps, or a different
// method name per kind of step. Here every operation exists for every receiver (a result, a Task<Result<T>>, a
// ValueTask<Result<T>>) and every delegate (sync, returning a Task, returning a ValueTask): nine variants under
// ONE name. The return type follows the receiver, and a synchronous receiver takes the kind of the delegate.
//
// Notes:
//  - An already completed Task or ValueTask takes a synchronous fast path without a state machine.
//  - The awaits inside the library do not capture the synchronization context (a setting, see ConfigurationExample).
//  - Arguments are validated when you CALL the method, not when you await what it returns.
//  - With C# 13 or later, `async` lambdas just work (OverloadResolutionPriority picks the Task overload).
namespace Wyzely.Functional.ResultSamples;

public static class AsyncExample
{
    public static void Run(TextWriter output) => RunAsync(output).GetAwaiter().GetResult();

    public static async Task RunAsync(TextWriter output)
    {
        // One chain. The comments say which of the nine variants each step uses.
        foreach (var orderId in new[] { 7, 8 })
        {
            var outcome = await SampleShop.FindOrderAsync(orderId) // a Task<Result<Order>> receiver
                .Ensure(order => order.IsOpen, Error.Conflict("The order is closed.")) // sync delegate
                .Ensure(SampleShop.ReserveStockAsync) // Task delegate
                .Bind(SampleShop.ChargeAsync) // ValueTask delegate
                .Map(receipt => receipt.Number) // sync delegate
                .MapError(error => error.WithContext("Checkout"));

            await output.WriteLineAsync($"Chain, order {orderId}: " + outcome.Match(number => number, error => error.ToString()));
        }

        // A failing step skips the rest: the charge is never made for the closed order.
        var charges = 0;
        _ = await SampleShop.FindOrderAsync(8)
            .Ensure(order => order.IsOpen, Error.Conflict("The order is closed."))
            .Bind(order =>
            {
                charges++;

                return SampleShop.ChargeAsync(order);
            });
        await output.WriteLineAsync($"Charges made for the closed order: {charges}");

        // The nine variants of one operation (Map), side by side. All of them produce Success(2).
        var value = Result.Success(1);

        var resultSync = value.Map(number => number + 1);
        var resultTask = await value.Map(number => Task.FromResult(number + 1));
        var resultValueTask = await value.Map(number => new ValueTask<int>(number + 1));
        await output.WriteLineAsync($"Result receiver, sync delegate: {resultSync}");
        await output.WriteLineAsync($"Result receiver, Task delegate: {resultTask}");
        await output.WriteLineAsync($"Result receiver, ValueTask delegate: {resultValueTask}");

        var taskSync = await Task.FromResult(value).Map(number => number + 1);
        var taskTask = await Task.FromResult(value).Map(number => Task.FromResult(number + 1));
        var taskValueTask = await Task.FromResult(value).Map(number => new ValueTask<int>(number + 1));
        await output.WriteLineAsync($"Task receiver, sync delegate: {taskSync}");
        await output.WriteLineAsync($"Task receiver, Task delegate: {taskTask}");
        await output.WriteLineAsync($"Task receiver, ValueTask delegate: {taskValueTask}");

        var valueTaskSync = await new ValueTask<Result<int>>(value).Map(number => number + 1);
        var valueTaskTask = await new ValueTask<Result<int>>(value).Map(number => Task.FromResult(number + 1));
        var valueTaskValueTask = await new ValueTask<Result<int>>(value).Map(number => new ValueTask<int>(number + 1));
        await output.WriteLineAsync($"ValueTask receiver, sync delegate: {valueTaskSync}");
        await output.WriteLineAsync($"ValueTask receiver, Task delegate: {valueTaskTask}");
        await output.WriteLineAsync($"ValueTask receiver, ValueTask delegate: {valueTaskValueTask}");

        // An async lambda converts to both a Task and a ValueTask delegate; with C# 13 or later the Task overload wins.
        var lambda = await SampleShop.FindOrderAsync(7)
            .Bind(SampleShop.ChargeAsync)
            .Map(async receipt =>
            {
                await Task.Yield();

                return receipt.Number + " (checked)";
            });
        await output.WriteLineAsync("Async lambda: " + lambda.ValueOr("?"));

        // Argument checks happen at the call. A null delegate throws here, not later inside the returned task.
        try
        {
            _ = Task.FromResult(value).Map((Func<int, int>)null!);
        }
        catch (ArgumentNullException)
        {
            await output.WriteLineAsync("A null delegate throws at the call, before any await: ArgumentNullException");
        }
    }
}
