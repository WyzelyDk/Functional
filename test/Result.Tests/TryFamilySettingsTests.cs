namespace Wyzely.Functional.ResultTests;

// The *Try operations share the exception handling of Result.Try: the settings apply to the form that takes any exception, and a named type is
// the caller's own decision.
[Collection(ProcessWideConfiguration.Name)]
public sealed class TryFamilySettingsTests : IDisposable
{
    private static readonly Func<int, int> Throw = static _ => Boom.Throw<int>();
    private static readonly Func<int, int> Cancel = static _ => throw new OperationCanceledException();

    public void Dispose() => ResultConfiguration.Reset();

    [Fact]
    internal void ShouldCatch_can_make_MapTry_turn_a_cancellation_into_a_failure()
    {
        ResultConfiguration.Configure(options => options.ShouldCatch = _ => true);

        var outcome = Result.Success(2).MapTry(Cancel);

        Assert.Equal(ErrorKind.Cancelled, outcome.GetError().Kind);
    }

    [Fact]
    internal void ShouldCatch_can_let_an_exception_escape_from_TapTry()
    {
        ResultConfiguration.Configure(options => options.ShouldCatch = exception => exception is not InvalidOperationException);
        var result = Result.Success(2);

        Assert.Throws<InvalidOperationException>(() => result.TapTry(static _ => Boom.Throw()));
    }

    [Fact]
    internal void ShouldCatch_is_not_consulted_when_the_exception_type_is_named()
    {
        ResultConfiguration.Configure(options => options.ShouldCatch = _ => false);

        var outcome = Result.Success(2).MapTry(Throw, static (InvalidOperationException exception) => Error.Create("Handled."));

        Assert.Equal("Handled.", outcome.GetError().Message);
    }

    [Fact]
    internal void ExceptionMapper_creates_the_error_when_there_is_no_handler()
    {
        ResultConfiguration.Configure(options => options.ExceptionMapper = exception => Error.Create("Mapped: " + exception.Message));

        var outcome = Result.Success(2).MapTry(Throw);

        Assert.Equal("Mapped: Boom.", outcome.GetError().Message);
    }
}
