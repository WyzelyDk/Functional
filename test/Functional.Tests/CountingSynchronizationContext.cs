namespace Wyzely.Functional.Tests;

// Counts how often a continuation is posted to it, so that a test can tell whether an await captured the context.
internal sealed class CountingSynchronizationContext : SynchronizationContext
{
    private int _posts;

    internal int Posts => Volatile.Read(ref _posts);

    public override void Post(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        ThreadPool.QueueUserWorkItem(_ => d(state));
    }
}
