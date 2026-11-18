using System.Globalization;
using Wyzely.Functional;
using Wyzely.Functional.AotSamples;

// Exercises every kind of code path the libraries have, so that the AOT compiler must compile and keep all of it.
// The exit code is the number of checks that failed.
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
var noneInt = Maybe.None<int>();

// Maybe: sync, Task and ValueTask receivers, all kinds of delegate.
var maybe = Maybe.Some(20).Map(static x => x + 1).Filter(static x => x > 5).Bind(static x => Maybe.Some(x * 2));
var maybeValue = maybe.GetValue();
Check("maybe sync", maybeValue == 42);

var someA = Maybe.Some("a");
var maybeAsync = await Task.FromResult(someA)
    .Map(static async s => await AppendAsync(s, "b"))
    .Bind(static s => SomeAppended(s, "c"));
var maybeAsyncValue = maybeAsync.GetValue();
Check("maybe async", maybeAsyncValue == "abc");

var noneText = Maybe.None<string>();
var stillNone = await Task.FromResult(noneText).Map(static s => s + "!");
Check("maybe none stays none", stillNone.IsNone);

var mappedToNull = Maybe.Some(1).Map(static _ => (int?)null);
Check("maybe nullable struct", mappedToNull.IsNone);

var nestedMaybe = Maybe.Some(someOne);
var flattenedMaybeValue = nestedMaybe.Flatten().GetValue();
Check("maybe flatten", flattenedMaybeValue == 1);

var someTwo = Maybe.Some(2);
var someThree = Maybe.Some(3);
var someFour = Maybe.Some(4);
var someFive = Maybe.Some(5);
var zippedValues = Maybe.Some(1).Zip(someTwo, someThree, someFour, someFive).GetValue();
Check("maybe zip5", zippedValues == (1, 2, 3, 4, 5));

var firstOverThree = threeAndFour.FirstOrNone(static x => x > 3).GetValue();
Check("maybe lookups", firstOverThree == 4);

var someSevenText = Maybe.Some(7).ToString();
var noneIntText = noneInt.ToString();
Check("maybe text", someSevenText == "Some(7)" && noneIntText == "None");

var comparison = someOne.CompareTo(noneInt);
Check("maybe equality and order", someOne == someOneAgain && comparison > 0);

var linqSum = (from a in Maybe.Some(1) from b in Maybe.Some(2) select a + b).GetValue();
Check("maybe linq", linqSum == 3);

// Result: errors, railway, async, combine, parallel, try.
var smallError = Error.Validation("small");
var result = Result.Success(2).Map(static x => x * 10).Ensure(static x => x > 5, smallError).Bind(static x => Result.Success(x + 1));
var resultValue = result.GetValue();
Check("result sync", resultValue == 21);

var successOne = Result.Success(1);
var conflict = Error.Conflict("c");
var failed = await Task.FromResult(successOne).Ensure(static x => x > 5, conflict).MapError(static e => e.WithContext("ctx"));
var failedText = failed.GetError().ToString();
Check("result failure", failedText == "ctx: c");

var failureOne = Result.Failure<int>("e1");
var failureTwo = Result.Failure<int>("e2");
var aggregate = Result.Success(1).Zip(failureOne, failureTwo);
var aggregateError = aggregate.GetError();
Check("result aggregate", aggregateError.IsAggregate && aggregateError.Children.Count == 2);

var combined = oneAndTwo.Combine().GetValue();
Check("result combine", combined.Count == 2);

var parsed = Result.Try(static () => int.Parse("x", CultureInfo.InvariantCulture));
Check("result try", parsed.IsFailure);

var secretDetail = new ErrorDetail("pw", "secret", isSensitive: true);
var secretText = secretDetail.ToString();
Check("result redaction", secretText == "pw=***");

var parallel = await oneTwoThree.SelectInParallel(static async (x, _) => await DoubleAsync(x));
var parallelSum = parallel.GetValue().Sum();
Check("result parallel", parallelSum == 12);

var nestedResult = Result.Success(successOne);
var flattenedResultValue = nestedResult.Flatten().GetValue();
Check("result flatten", flattenedResultValue == 1);

// Interop in the umbrella package.
var notFound = Error.NotFound("n");
var missing = noneInt.ToResult(notFound);
var missingKind = missing.GetError().Kind;
Check("interop to result", missingKind == ErrorKind.NotFound);

var converted = await Task.FromResult(someThree).ToResult(notFound);
var convertedValue = converted.GetValue();
Check("interop async", convertedValue == 3);

var asMaybe = Result.Failure<int>("x").ToMaybe();
Check("interop to maybe", asMaybe.IsNone);

var taken = Error.Conflict("taken");
var invertedSome = someOne.ToInvertedResult(taken);
var invertedNone = noneInt.ToInvertedResult(taken);
Check("interop inverted", invertedSome.IsFailure && invertedNone.IsSuccess);

var invertedAsync = await Task.FromResult(someOne).ToInvertedResult(static id => Error.Conflict($"{id} exists"));
var invertedMessage = invertedAsync.GetError().Message;
Check("interop inverted async", invertedMessage == "1 exists");

// The operations added after the first release.
NewOperations.Railway(Check);
NewOperations.ErrorsAndMaybe(Check);
NewOperations.Exits(Check);
await NewOperations.StreamsAsync(Check);

// Settings are respected under AOT too.
MaybeConfiguration.Configure(UseJustAndNothing);
var justText = someOne.ToString();
var nothingText = noneInt.ToString();
Check("maybe settings", justText == "Just 1" && nothingText == "Nothing");

ResultConfiguration.Configure(static o => o.ContextSeparator = " > ");
var contextText = Error.Create("m").WithContext("c").ToString();
Check("result settings", contextText == "c > m");

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

var summary = Summarize(failures);
await Console.Out.WriteLineAsync(summary);

return failures.Count;

static string Summarize(List<string> failures) =>
    failures.Count == 0 ? "AOT OK: all checks passed" : "AOT FAILURES: " + string.Join(", ", failures);

static async Task<string> AppendAsync(string text, string suffix)
{
    await Task.Yield();

    return text + suffix;
}

static ValueTask<Maybe<string>> SomeAppended(string text, string suffix)
{
    var someText = Maybe.Some(text + suffix);

    return new ValueTask<Maybe<string>>(someText);
}

static async Task<Result<int>> DoubleAsync(int x)
{
    await Task.Yield();

    return Result.Success(x * 2);
}

static void UseJustAndNothing(MaybeOptions options)
{
    options.SomeFormat = "Just {0}";
    options.NoneText = "Nothing";
}
