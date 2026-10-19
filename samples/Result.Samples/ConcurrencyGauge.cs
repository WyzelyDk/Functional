// Counts how many operations ran, and the most that ran at the same time, so that the parallel examples can show
// that the limit is respected. It is plain thread-safe counting code and has nothing to do with results.
namespace Wyzely.Functional.ResultSamples;

internal sealed class ConcurrencyGauge
{
    private int _running;
    private int _peak;
    private int _calls;

    public int Calls => Volatile.Read(ref _calls);

    public int Peak => Volatile.Read(ref _peak);

    public void Enter()
    {
        _ = Interlocked.Increment(ref _calls);
        var now = Interlocked.Increment(ref _running);
        int seen;

        do
        {
            seen = Volatile.Read(ref _peak);
        }
        while (now > seen && Interlocked.CompareExchange(ref _peak, now, seen) != seen);
    }

    public void Exit() => Interlocked.Decrement(ref _running);
}
