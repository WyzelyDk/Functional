namespace Wyzely.Functional.SharedTests;

public sealed class GuardTests
{
    [Fact]
    internal void NotNull_returns_the_value_when_it_is_not_null()
    {
        var value = new object();

        Assert.Same(value, Guard.NotNull(value, nameof(value)));
    }

    [Fact]
    internal void NotNull_throws_with_the_name_of_the_argument_when_the_value_is_null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.NotNull<object>(null, "thing"));

        Assert.Equal("thing", exception.ParamName);
    }

    [Fact]
    internal void NotNullOrWhiteSpace_returns_the_text_when_it_has_content()
    {
        Assert.Equal("a", Guard.NotNullOrWhiteSpace("a", "text"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    internal void NotNullOrWhiteSpace_throws_an_argument_exception_for_empty_or_blank_text(string text)
    {
        var exception = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(text, "argument"));

        Assert.Equal("argument", exception.ParamName);
    }

    [Fact]
    internal void NotNullOrWhiteSpace_throws_an_argument_null_exception_for_null()
    {
        Assert.Throws<ArgumentNullException>(() => Guard.NotNullOrWhiteSpace(null, "text"));
    }
}
