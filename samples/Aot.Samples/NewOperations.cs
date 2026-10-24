using System.Globalization;

namespace Wyzely.Functional.AotSamples;

// The checks for the operations added after the first release, kept apart so that Program.cs stays short. Each method reports to the same
// Check that Program.cs uses.
internal static class NewOperations
{
    internal static void Railway(Action<string, bool> check)
    {
        var conditional = Result.Success(3).BindIf(static x => x > 2, static x => Result.Success(x * 10)).MapIf(static _ => true, static x => x + 1);
        check("result conditional", conditional.GetValue() == 31);
        check("result ensure not", Result.Success(3).EnsureNot(static x => x > 2, Error.Validation("big")).IsFailure);
        check("result ensure not null", Result.Success<string?>(null).EnsureNotNull(Error.Validation("null")).IsFailure);
        var tooSmall = Result.Success(1).Ensure(static x => x > 5, static x => Error.Validation("too small: {x}").WithDetail("x", x));
        check("result ensure factory", tooSmall.GetError().Message == "too small: 1");
        check("result success if", Result.SuccessIf(true, Error.Conflict("c")).IsSuccess);
        check("result failure if", Result.FailureIf(true, "f").IsFailure);
        check("result bind zip", Result.Success(1).BindZip(static x => Result.Success(x + 1)).GetValue() == (1, 2));
        check("result map try", Result.Success("12").MapTry(static s => int.Parse(s, CultureInfo.InvariantCulture)).GetValue() == 12);
        check(
            "result typed try",
            Result.Try(static () => int.Parse("x", CultureInfo.InvariantCulture), static (FormatException e) => Error.FromException(e)).IsFailure);
        check("result convert failure", Result.Failure<int>("x").ConvertFailure<string>().IsFailure);
        check("result first failure", new[] { Result.Success(), Result.Failure("a"), Result.Failure("b") }.FirstFailureOrSuccess().GetError().Message == "a");
        check("result ensure some", Result.Success(Maybe.Some(5)).EnsureSome(Error.NotFound("n")).GetValue() == 5);
    }

    internal static void ErrorsAndMaybe(Action<string, bool> check)
    {
        // Templates, details and context frames are formatted without reflection.
        check("error frames", Error.Create("m {a}").WithDetail("a", 1).WithContext("Checkout of {id}", "id", 7).ToString() == "Checkout of 7: m 1");

        check("maybe any", Maybe.Some(5).Any(static x => x > 3));
        check("maybe all", Maybe.Some(5).All(static x => x > 3));
        check("maybe contains", Maybe.Some(5).Contains(5));
        check("maybe from", Maybe.From((string?)null).IsNone && Maybe.From("x").IsSome);
    }

    internal static void Exits(Action<string, bool> check)
    {
        try
        {
            _ = Result.Failure<int>("x").GetValueOrThrow(static e => new ArgumentException(e.Message));
            check("result get value or throw", false);
        }
        catch (ArgumentException)
        {
            // Expected: the factory chooses the exception.
        }
    }

    internal static async Task StreamsAsync(Action<string, bool> check)
    {
        IReadOnlyList<int> numbers = [1, 2, 3];
        var each = Result.Success(numbers).SelectEach(static async x =>
        {
            await Task.Yield();

            return x == 2 ? Result.Failure<int>("two") : Result.Success(x);
        });

        var (values, errors) = await each.Partition();
        check("result stream partition", values.Count == 2 && errors.Count == 1);
        check("result stream combine", (await each.Combine()).IsFailure);
    }
}
