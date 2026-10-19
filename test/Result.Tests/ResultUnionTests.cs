#if NET11_0_OR_GREATER

namespace Wyzely.Functional.ResultTests;

// These tests need the C# 15 compiler and the .NET 11 runtime types, so they exist only for the net11.0 target. The exhaustive
// switch expressions below are part of what is tested: with warnings as errors, a result that was not recognised as a union
// (or that needed a null arm) would not compile.
public sealed class ResultUnionTests
{
    private static readonly Error Boom = Error.Create("Boom.");

    [Fact]
    internal void A_switch_matches_the_value_of_a_success()
    {
        Assert.Equal("ok:2", Describe(Result.Success(2)));
    }

    [Fact]
    internal void A_switch_matches_the_error_of_a_failure()
    {
        Assert.Equal("failed:Boom.", Describe(Result.Failure<int>(Boom)));
    }

    [Fact]
    internal void A_switch_over_a_default_result_matches_the_value_case()
    {
        Assert.Equal("ok:0", Describe(default));
    }

    [Fact]
    internal void A_null_value_of_a_success_matches_the_null_case()
    {
        Assert.Equal("none", DescribeText(Result.Success<string?>(null)));
    }

    [Fact]
    internal void The_language_converts_a_case_type_to_the_union_implicitly()
    {
        Result<int> fromValue = 5;
        Result<int> fromError = Boom;

        Assert.Multiple(
            () => Assert.Equal(5, fromValue.GetValue()),
            () => Assert.Same(Boom, fromError.GetError()));
    }

    [Fact]
    internal void The_union_members_expose_the_contained_case()
    {
        var success = (Result<int>.IUnionMembers)Result.Success(2);
        var failure = (Result<int>.IUnionMembers)Result.Failure<int>(Boom);
        var defaulted = (Result<int>.IUnionMembers)default(Result<int>);

        Assert.Multiple(
            () => Assert.Equal(2, success.Value),
            () => Assert.True(success.HasValue),
            () => Assert.Same(Boom, failure.Value),
            () => Assert.True(failure.HasValue),
            () => Assert.Equal(0, defaulted.Value),
            () => Assert.True(defaulted.HasValue));
    }

    [Fact]
    internal void The_non_boxing_access_reports_the_right_case()
    {
        var success = (Result<int>.IUnionMembers)Result.Success(2);
        var failure = (Result<int>.IUnionMembers)Result.Failure<int>(Boom);

        var successHasValue = success.TryGetValue(out int value);
        var successHasError = success.TryGetValue(out Error _);
        var failureHasError = failure.TryGetValue(out Error error);
        var failureHasValue = failure.TryGetValue(out int _);

        Assert.Multiple(
            () => Assert.True(successHasValue),
            () => Assert.Equal(2, value),
            () => Assert.False(successHasError),
            () => Assert.True(failureHasError),
            () => Assert.Same(Boom, error),
            () => Assert.False(failureHasValue));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0072",
        Justification = "The analyzer does not know yet that a union switch over its two case types is complete; the compiler does.")]
    private static string Describe(Result<int> result) => result switch
    {
        int value => $"ok:{value}",
        Error error => $"failed:{error.Message}",
    };

    private static string DescribeText(Result<string?> result) => result switch
    {
        string text => text,
        Error error => error.Message,
        null => "none",
    };
}

#endif
