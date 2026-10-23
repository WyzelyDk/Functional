#if NET8_0_OR_GREATER
namespace Wyzely.Functional.Internal;

/// <summary>Reads asynchronous streams for the helpers of results, which follow <see cref="ResultOptions.ContinueOnCapturedContext"/>.</summary>
internal static class ResultStreams
{
    /// <summary>Reads a whole stream, once and in order, into a list.</summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="source">The stream to read.</param>
    /// <param name="cancellationToken">A token that stops the reading.</param>
    /// <returns>A task that produces the items of the stream.</returns>
    internal static async ValueTask<List<T>> ReadAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        var items = new List<T>();

        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(ResultConfiguration.ContinueOnCapturedContext))
        {
            items.Add(item);
        }

        return items;
    }
}
#endif
