using System.Globalization;
using Wyzely.Functional;
using Wyzely.Functional.AotSamples;

// Exercises every kind of code path the libraries have, so that the AOT compiler must compile and keep all of it. The exit code is
// the number of checks that failed.
var failures = new List<string>();

void Check(string name, bool ok)
{
    if (!ok)
    {
        failures.Add(name);
    }
}

int[] threeAndFour = [3, 4];
int[] oneTwoThree = [1, 2, 3];
Result<int>[] oneAndTwo = [Result.Success(1), Result.Success(2)];
var someOne = Maybe.Some(1);
var someOneAgain = Maybe.Some(1);

// Maybe: sync, Task and ValueTask receivers, all kinds of delegate.
var maybe = Maybe.Some(20).Map(static x => x + 1).Filter(static x => x > 5).Bind(static x => Maybe.Some(x * 2));
Check("maybe sync", maybe.GetValue() == 42);

var maybeAsync = await Task.FromResult(Maybe.Some("a"))
    .Map(static async s =>
    {
        await Task.Yield();

        return s + "b";
    })
    .Bind(static s => new ValueTask<Maybe<string>>(Maybe.Some(s + "c")));
Check("maybe async", maybeAsync.GetValue() == "abc");
Check("maybe none stays none", (await Task.FromResult(Maybe.None<string>()).Map(static s => s + "!")).IsNone);
Check("maybe nullable struct", Maybe.Some(1).Map(static _ => (int?)null).IsNone);
Check("maybe flatten", Maybe.Some(Maybe.Some(1)).Flatten().GetValue() == 1);
Check("maybe zip5", Maybe.Some(1).Zip(Maybe.Some(2), Maybe.Some(3), Maybe.Some(4), Maybe.Some(5)).GetValue() == (1, 2, 3, 4, 5));
Check("maybe lookups", threeAndFour.FirstOrNone(static x => x > 3).GetValue() == 4);
Check("maybe text", Maybe.Some(7).ToString() == "Some(7)" && Maybe.None<int>().ToString() == "None");
Check("maybe equality and order", someOne == someOneAgain && someOne.CompareTo(Maybe.None<int>()) > 0);
Check("maybe linq", (from a in Maybe.Some(1) from b in Maybe.Some(2) select a + b).GetValue() == 3);

// Result: errors, railway, async, combine, parallel, try.
var result = Result.Success(2).Map(static x => x * 10).Ensure(static x => x > 5, Error.Validation("small")).Bind(static x => Result.Success(x + 1));
Check("result sync", result.GetValue() == 21);

var failed = await Task.FromResult(Result.Success(1)).Ensure(static x => x > 5, Error.Conflict("c")).MapError(static e => e.WithContext("ctx"));
Check("result failure", failed.GetError().ToString() == "ctx: c");

var aggregate = Result.Success(1).Zip(Result.Failure<int>("e1"), Result.Failure<int>("e2"));
Check("result aggregate", aggregate.GetError().IsAggregate && aggregate.GetError().Children.Count == 2);
Check("result combine", oneAndTwo.Combine().GetValue().Count == 2);
Check("result try", Result.Try(static () => int.Parse("x", CultureInfo.InvariantCulture)).IsFailure);
Check("result redaction", new ErrorDetail("pw", "secret", isSensitive: true).ToString() == "pw=***");

var parallel = await oneTwoThree.SelectInParallel(static async (x, _) =>
{
    await Task.Yield();

    return Result.Success(x * 2);
});
Check("result parallel", parallel.GetValue().Sum() == 12);
Check("result flatten", Result.Success(Result.Success(1)).Flatten().GetValue() == 1);

// Interop in the umbrella package.
Check("interop to result", Maybe.None<int>().ToResult(Error.NotFound("n")).GetError().Kind == ErrorKind.NotFound);
Check("interop async", (await Task.FromResult(Maybe.Some(3)).ToResult(Error.NotFound("n"))).GetValue() == 3);
Check("interop to maybe", Result.Failure<int>("x").ToMaybe().IsNone);
Check(
    "interop inverted",
    Maybe.Some(1).ToInvertedResult(Error.Conflict("taken")).IsFailure && Maybe.None<int>().ToInvertedResult(Error.Conflict("taken")).IsSuccess);
var invertedAsync = await Task.FromResult(Maybe.Some(1)).ToInvertedResult(static id => Error.Conflict($"{id} exists"));
Check("interop inverted async", invertedAsync.GetError().Message == "1 exists");

// The operations added after the first release.
NewOperations.Railway(Check);
NewOperations.ErrorsAndMaybe(Check);
NewOperations.Exits(Check);
await NewOperations.StreamsAsync(Check);

// Settings are respected under AOT too.
MaybeConfiguration.Configure(static o =>
{
    o.SomeFormat = "Just {0}";
    o.NoneText = "Nothing";
});
Check("maybe settings", Maybe.Some(1).ToString() == "Just 1" && Maybe.None<int>().ToString() == "Nothing");
ResultConfiguration.Configure(static o => o.ContextSeparator = " > ");
Check("result settings", Error.Create("m").WithContext("c").ToString() == "c > m");

// Misuse throws the documented exceptions.
try
{
    _ = Maybe.None<int>().GetValue();
    Check("maybe get value throws", false);
}
catch (InvalidOperationException)
{
    // Expected: a maybe without a value has nothing to give.
}

Check("default result is a success", default(Result<int>).IsSuccess);

await Console.Out.WriteLineAsync(failures.Count == 0 ? "AOT OK: all checks passed" : "AOT FAILURES: " + string.Join(", ", failures));

return failures.Count;
