using System.Runtime.CompilerServices;

namespace Wyzely.Functional.ResultTests;

// Asynchronous streams for the tests: items that arrive later, items that are counted as they are pulled, and a stream that honors cancellation.
internal static class AsyncStreams
{
    // Reads a whole stream into a list. The BCL only has this from .NET 10 on, and the tests run on .NET 8 too.
    internal static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        var list = new List<T>();

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }

    // Yields the items one by one, each after an await that completes later.
    internal static async IAsyncEnumerable<T> Of<T>(params T[] items)
    {
        foreach (var item in items)
        {
            await Task.Yield();

            yield return item;
        }
    }

    // Like Of, but the wait does not use the current synchronization context, so that a test can count the posts of the code under test.
    internal static async IAsyncEnumerable<T> Pending<T>(params T[] items)
    {
        foreach (var item in items)
        {
            await Task.Delay(1).ConfigureAwait(false);

            yield return item;
        }
    }

    // Yields the items and tells how many have been pulled so far, to see where a consumer stops.
    internal static async IAsyncEnumerable<T> Counted<T>(Action<int> pulled, params T[] items)
    {
        var count = 0;

        foreach (var item in items)
        {
            await Task.Yield();
            count++;
            pulled(count);

            yield return item;
        }
    }

    // Yields the items and stops with an OperationCanceledException as soon as the token of the consumer is cancelled.
    internal static IAsyncEnumerable<T> Cancellable<T>(T[] items) => CancellableCore(items, CancellationToken.None);

    private static async IAsyncEnumerable<T> CancellableCore<T>(T[] items, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return item;
        }
    }
}
