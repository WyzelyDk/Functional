namespace Wyzely.Functional.ResultTests;

// The value of a success is compared with the default equality of its type. A comparer that is given decides instead, for one comparison (Equals, GetHashCode)
// or for a collection of results (CreateEqualityComparer). A failure is always compared by its error, and the comparer is not asked about it.
public sealed class ResultComparerTests
{
    private static readonly IEqualityComparer<string> IgnoreCase = StringComparer.OrdinalIgnoreCase;
    private static readonly IEqualityComparer<string> NeverAsked = EqualityComparer<string>.Create(
        static (_, _) => throw new InvalidOperationException("The comparer must not be asked."),
        static _ => throw new InvalidOperationException("The comparer must not be asked."));

    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void Equals_with_a_comparer_finds_two_successes_equal_that_the_comparer_finds_equal()
    {
        var upper = Result.Success("A");
        var lower = Result.Success("a");

        Assert.Multiple(
            () => Assert.True(lower.Equals(upper, IgnoreCase)),
            () => Assert.False(lower.Equals(upper)));
    }

    [Fact]
    internal void Equals_with_a_comparer_finds_two_different_values_different()
    {
        var left = Result.Success("a");
        var right = Result.Success("b");

        Assert.False(left.Equals(right, IgnoreCase));
    }

    [Fact]
    internal void Equals_with_a_comparer_compares_two_failures_by_their_error_without_asking_the_comparer()
    {
        var first = Result.Failure<string>(Boom);
        var same = Result.Failure<string>(Boom);
        var other = Result.Failure<string>(Error.Create("Other."));

        Assert.Multiple(
            () => Assert.True(first.Equals(same, NeverAsked)),
            () => Assert.False(first.Equals(other, NeverAsked)));
    }

    [Fact]
    internal void Equals_with_a_comparer_is_false_for_a_success_and_a_failure_without_asking_the_comparer()
    {
        var success = Result.Success("a");
        var failure = Result.Failure<string>(Boom);

        Assert.Multiple(
            () => Assert.False(success.Equals(failure, NeverAsked)),
            () => Assert.False(failure.Equals(success, NeverAsked)));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_is_the_same_for_values_the_comparer_finds_equal()
    {
        var upper = Result.Success("A");
        var lower = Result.Success("a");

        Assert.Equal(upper.GetHashCode(IgnoreCase), lower.GetHashCode(IgnoreCase));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_uses_the_hash_of_the_comparer_for_a_success()
    {
        var constant = EqualityComparer<string>.Create(static (left, right) => left == right, static _ => 7);

        Assert.Equal(Result.Success("a").GetHashCode(constant), Result.Success("b").GetHashCode(constant));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_of_a_success_without_a_value_is_the_hash_of_a_default_success_without_asking_the_comparer()
    {
        var noValue = Result.Success<string?>(null);
        var neverAsked = EqualityComparer<string?>.Create(
            static (_, _) => throw new InvalidOperationException("The comparer must not be asked."),
            static _ => throw new InvalidOperationException("The comparer must not be asked."));

        Assert.Equal(noValue.GetHashCode(), noValue.GetHashCode(neverAsked));
    }

    [Fact]
    internal void GetHashCode_with_a_comparer_of_a_failure_is_the_hash_of_its_error_without_asking_the_comparer()
    {
        var failure = Result.Failure<string>(Boom);

        Assert.Equal(Boom.GetHashCode(), failure.GetHashCode(NeverAsked));
    }

    [Fact]
    internal void CreateEqualityComparer_makes_a_hash_set_treat_values_the_comparer_finds_equal_as_one()
    {
        var set = new HashSet<Result<string>>(Result.CreateEqualityComparer(IgnoreCase))
        {
            Result.Success("A"),
            Result.Success("a"),
            Result.Failure<string>(Boom),
            Result.Failure<string>(Boom),
        };

        Assert.Multiple(
            () => Assert.Equal(2, set.Count),
            () => Assert.Contains(Result.Success("A"), set),
            () => Assert.Contains(Result.Failure<string>(Boom), set));
    }

    [Fact]
    internal void CreateEqualityComparer_makes_distinct_drop_the_values_the_comparer_finds_equal()
    {
        Result<string>[] results = [Result.Success("a"), Result.Success("A"), Result.Failure<string>(Boom), Result.Success("b")];

        var distinct = results.Distinct(Result.CreateEqualityComparer(IgnoreCase)).ToList();

        Assert.Equal(3, distinct.Count);
    }
}
