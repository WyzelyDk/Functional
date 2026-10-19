namespace Wyzely.Functional.ResultTests;

public sealed class ErrorKindTests
{
    [Fact]
    internal void Default_is_the_failure_kind()
    {
        var kind = default(ErrorKind);

        Assert.Multiple(
            () => Assert.Equal("Failure", kind.Name),
            () => Assert.Equal(ErrorKind.Failure, kind));
    }

    [Fact]
    internal void Constructor_keeps_the_name_of_a_custom_kind()
    {
        var kind = new ErrorKind("RateLimited");

        Assert.Equal("RateLimited", kind.Name);
    }

    [Fact]
    internal void Constructor_throws_when_the_name_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new ErrorKind(null!));
    }

    [Fact]
    internal void Constructor_throws_when_the_name_is_whitespace()
    {
        Assert.Throws<ArgumentException>(() => new ErrorKind("  "));
    }

    [Fact]
    internal void Well_known_kinds_have_their_own_names()
    {
        Assert.Multiple(
            () => Assert.Equal("Failure", ErrorKind.Failure.Name),
            () => Assert.Equal("Unexpected", ErrorKind.Unexpected.Name),
            () => Assert.Equal("Validation", ErrorKind.Validation.Name),
            () => Assert.Equal("NotFound", ErrorKind.NotFound.Name),
            () => Assert.Equal("Conflict", ErrorKind.Conflict.Name),
            () => Assert.Equal("Unauthorized", ErrorKind.Unauthorized.Name),
            () => Assert.Equal("Forbidden", ErrorKind.Forbidden.Name),
            () => Assert.Equal("Unavailable", ErrorKind.Unavailable.Name),
            () => Assert.Equal("Timeout", ErrorKind.Timeout.Name),
            () => Assert.Equal("Cancelled", ErrorKind.Cancelled.Name));
    }

    [Fact]
    internal void Equality_compares_the_names()
    {
        var first = new ErrorKind("NotFound");
        var second = new ErrorKind("NotFound");
        var other = new ErrorKind("Conflict");

        Assert.Multiple(
            () => Assert.True(first.Equals(second)),
            () => Assert.True(first == second),
            () => Assert.False(first != second),
            () => Assert.Equal(first.GetHashCode(), second.GetHashCode()),
            () => Assert.False(first.Equals(other)),
            () => Assert.True(first != other),
            () => Assert.True(first.Equals((object)second)),
            () => Assert.False(first.Equals("NotFound")));
    }

    [Fact]
    internal void ToString_returns_the_name()
    {
        Assert.Equal("Timeout", ErrorKind.Timeout.ToString());
    }
}
