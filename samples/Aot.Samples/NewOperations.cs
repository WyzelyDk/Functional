using System.Globalization;

namespace Wyzely.Functional.AotSamples;

// The checks for the operations added after the first release, kept apart so that Program.cs stays short.
// Each method reports to the same Check that Program.cs uses.
internal static class NewOperations
{
    internal static void Railway(Action<string, bool> check)
    {
        var conditional = Result.Success(3).BindIf(static x => x > 2, static x => Result.Success(x * 10)).MapIf(static _ => true, static x => x + 1);
        var conditionalValue = conditional.GetValue();
        check("result conditional", conditionalValue == 31);

        var bigError = Error.Validation("big");
        var ensuredNot = Result.Success(3).EnsureNot(static x => x > 2, bigError);
        check("result ensure not", ensuredNot.IsFailure);

        var nullError = Error.Validation("null");
        var ensuredNotNull = Result.Success<string?>(null).EnsureNotNull(nullError);
        check("result ensure not null", ensuredNotNull.IsFailure);

        var tooSmall = Result.Success(1).Ensure(static x => x > 5, static x => Error.Validation("too small: {x}").WithDetail("x", x));
        var tooSmallMessage = tooSmall.GetError().Message;
        check("result ensure factory", tooSmallMessage == "too small: 1");

        var conflict = Error.Conflict("c");
        var successIf = Result.SuccessIf(true, conflict);
        check("result success if", successIf.IsSuccess);

        var failureIf = Result.FailureIf(true, "f");
        check("result failure if", failureIf.IsFailure);

        var boundZip = Result.Success(1).BindZip(static x => Result.Success(x + 1)).GetValue();
        check("result bind zip", boundZip == (1, 2));

        var mappedTry = Result.Success("12").MapTry(static s => int.Parse(s, CultureInfo.InvariantCulture)).GetValue();
        check("result map try", mappedTry == 12);

        var typedTry = Result.Try(static () => int.Parse("x", CultureInfo.InvariantCulture), static (FormatException e) => Error.FromException(e));
        check("result typed try", typedTry.IsFailure);

        var convertedFailure = Result.Failure<int>("x").ConvertFailure<string>();
        check("result convert failure", convertedFailure.IsFailure);

        var mixed = new[] { Result.Success(), Result.Failure("a"), Result.Failure("b") };
        var firstFailureMessage = mixed.FirstFailureOrSuccess().GetError().Message;
        check("result first failure", firstFailureMessage == "a");

        var notFound = Error.NotFound("n");
        var someFive = Maybe.Some(5);
        var ensuredSome = Result.Success(someFive).EnsureSome(notFound).GetValue();
        check("result ensure some", ensuredSome == 5);
    }

    internal static void ErrorsAndMaybe(Action<string, bool> check)
    {
        // Templates, details and context frames are formatted without reflection.
        var framesText = Error.Create("m {a}").WithDetail("a", 1).WithContext("Checkout of {id}", "id", 7).ToString();
        check("error frames", framesText == "Checkout of 7: m 1");

        var anyOverThree = Maybe.Some(5).Any(static x => x > 3);
        check("maybe any", anyOverThree);

        var allOverThree = Maybe.Some(5).All(static x => x > 3);
        check("maybe all", allOverThree);

        var containsFive = Maybe.Some(5).Contains(5);
        check("maybe contains", containsFive);

        var fromNull = Maybe.From((string?)null);
        var fromText = Maybe.From("x");
        check("maybe from", fromNull.IsNone && fromText.IsSome);
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
        var each = Result.Success(numbers).BindEach(static async x =>
        {
            await Task.Yield();

            return x == 2 ? Result.Failure<int>("two") : Result.Success(x);
        });

        var (values, errors) = await each.Partition();
        check("result stream partition", values.Count == 2 && errors.Count == 1);
        var combined = await each.Combine();
        check("result stream combine", combined.IsFailure);
    }
}
