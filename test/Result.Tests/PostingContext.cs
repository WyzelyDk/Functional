namespace Wyzely.Functional.ResultTests;

// Like a UI thread: it counts what is posted to it and is the current context while it runs the callback, so that a second await
// inside the chain finds it too.
internal sealed class PostingContext : SynchronizationContext
{
    private int _posts;
    private int _active;

    internal int Posts => Volatile.Read(ref _posts);

    // Blocks until every callback that was posted has finished.
    public void WaitUntilIdle() => Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref _active) == 0, TimeSpan.FromSeconds(10)));

    public override void Post(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        Interlocked.Increment(ref _active);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                SetSynchronizationContext(this);
                d(state);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        });
    }
}
