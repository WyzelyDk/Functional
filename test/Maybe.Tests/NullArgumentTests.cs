using Wyzely.Functional.ArgumentTesting;

namespace Wyzely.Functional.MaybeTests;

// A public method validates its arguments when it is called, also when it returns a task: the exception must not wait for the await.
public sealed class NullArgumentTests
{
    [Fact]
    internal void Every_public_static_method_throws_an_ArgumentNullException_at_the_call_for_a_null_argument()
    {
        var report = NullArgumentProbe.Probe(typeof(Maybe).Assembly.GetExportedTypes());

        Assert.Multiple(
            () => Assert.True(report.Probed > 150, "Too few arguments were probed: " + report.Probed),
            () => Assert.True(report.Unsupported.Count == 0, report.UnsupportedText),
            () => Assert.True(report.Violations.Count == 0, report.ViolationsText));
    }
}
