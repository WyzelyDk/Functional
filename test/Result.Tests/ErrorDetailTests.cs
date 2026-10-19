namespace Wyzely.Functional.ResultTests;

public sealed class ErrorDetailTests
{
    [Fact]
    internal void Constructor_keeps_the_name_value_and_sensitivity()
    {
        var detail = new ErrorDetail("Path", "a/b", isSensitive: true);

        Assert.Multiple(
            () => Assert.Equal("Path", detail.Name),
            () => Assert.Equal("a/b", detail.Value),
            () => Assert.True(detail.IsSensitive));
    }

    [Fact]
    internal void Constructor_marks_a_detail_as_not_sensitive_by_default()
    {
        var detail = new ErrorDetail("Path", "a/b");

        Assert.False(detail.IsSensitive);
    }

    [Fact]
    internal void Constructor_throws_when_the_name_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new ErrorDetail(null!, 1));
    }

    [Fact]
    internal void Constructor_throws_when_the_name_is_whitespace()
    {
        Assert.Throws<ArgumentException>(() => new ErrorDetail(" ", 1));
    }

    [Fact]
    internal void Equality_compares_name_value_and_sensitivity()
    {
        var detail = new ErrorDetail("Count", 3);

        Assert.Multiple(
            () => Assert.True(detail.Equals(new ErrorDetail("Count", 3))),
            () => Assert.True(detail == new ErrorDetail("Count", 3)),
            () => Assert.Equal(detail.GetHashCode(), new ErrorDetail("Count", 3).GetHashCode()),
            () => Assert.False(detail.Equals(new ErrorDetail("Total", 3))),
            () => Assert.False(detail.Equals(new ErrorDetail("Count", 4))),
            () => Assert.True(detail != new ErrorDetail("Count", 3, isSensitive: true)),
            () => Assert.True(detail.Equals((object)new ErrorDetail("Count", 3))),
            () => Assert.False(detail.Equals("Count")));
    }

    [Fact]
    internal void ToString_shows_the_name_and_value()
    {
        Assert.Equal("Path=a/b", new ErrorDetail("Path", "a/b").ToString());
    }

    [Fact]
    internal void ToString_hides_the_value_of_a_sensitive_detail()
    {
        Assert.Equal("Password=***", new ErrorDetail("Password", "hunter2", isSensitive: true).ToString());
    }

    [Fact]
    internal void A_default_detail_has_an_empty_name()
    {
        Assert.Equal(string.Empty, default(ErrorDetail).Name);
    }

    [Fact]
    internal void A_detail_without_a_value_has_a_stable_hash_code()
    {
        Assert.Equal(new ErrorDetail("Path", null).GetHashCode(), new ErrorDetail("Path", null).GetHashCode());
    }

    [Fact]
    internal void ToString_shows_nothing_for_a_value_that_has_no_text()
    {
        Assert.Equal("Path=", new ErrorDetail("Path", new WithoutText()).ToString());
    }

    private sealed class WithoutText
    {
        public override string? ToString() => null;
    }
}
