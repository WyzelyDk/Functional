# Testing code that returns a Result

[Back to the Result overview](../../src/Result/README.md)

An expected failure is a value, so a test needs no mock and no `Assert.Throws`: call the method and look at the result. The examples use xUnit, and the same
assertions work in any test framework. Every snippet on this page is a test in `ReadmeExamplesTests`, so they are run.

The code under test is a lookup that can fail:

```csharp
public static class Prices
{
    public static Result<decimal> Find(string product) =>
        product == "apple" ? Result.Success(2.5m) : Result.Failure<decimal>(NoPrice(product));

    public static Task<Result<decimal>> FindAsync(string product) => Task.FromResult(Find(product));

    public static Error NoPrice(string product) =>
        Error.NotFound("No price for {Product}.").WithCode("NO_PRICE").WithDetail("Product", product);
}
```

## A success

```csharp
[Fact]
public void Find_succeeds_for_a_product_with_a_price()
{
    var result = Prices.Find("apple");

    Assert.True(result.IsSuccess);
    Assert.Equal(2.5m, result.GetValue());
}
```

## An error

Assert the kind and the code of the error. The message is text for people and changes more often than they do.

```csharp
[Fact]
public void Find_fails_with_not_found_for_a_product_without_a_price()
{
    var result = Prices.Find("pear");

    Assert.True(result.IsFailure);
    var error = result.GetError();
    Assert.Equal(ErrorKind.NotFound, error.Kind);
    Assert.Equal("NO_PRICE", error.Code);
    Assert.Equal("No price for pear.", error.Message);
}
```

`Message` has the details filled in. A detail that is marked sensitive is in `Message` and not in `RedactedMessage` ([errors](errors.md)), so assert on
`RedactedMessage` when a test is about what may be shown or logged.

## A whole result

A result is a value, so two results can be compared. An error is equal to another when its template, code, kind, details, context and children are equal
and the cause is the same exception instance ([errors](errors.md)).

```csharp
[Fact]
public void Find_gives_the_results_that_are_expected()
{
    Assert.Equal(Result.Success(2.5m), Prices.Find("apple"));
    Assert.Equal(Result.Failure<decimal>(Prices.NoPrice("pear")), Prices.Find("pear"));
}
```

## A method that is asynchronous

Await it and assert on the result, as above.

```csharp
[Fact]
public async Task FindAsync_succeeds_for_a_product_with_a_price()
{
    var result = await Prices.FindAsync("apple");

    Assert.Equal(Result.Success(2.5m), result);
}
```

## A chain stops at the first failure

To show that the steps after a failure do not run, count the calls of a step:

```csharp
[Fact]
public void A_failure_skips_the_steps_after_it()
{
    var calls = 0;

    var result = Prices.Find("pear")
        .Map(static price => price * 2)
        .Tap(_ => calls++);

    Assert.Equal(0, calls);
    Assert.Equal(ErrorKind.NotFound, result.GetError().Kind);
}
```

## When an assertion prints a result

A result prints as `Failure: ` and its message, or as `Success(` and its value, so a failing assertion on a whole result is readable:

```csharp
[Fact]
public void A_failure_prints_as_its_message()
{
    Assert.Equal("Failure: No price for pear.", Prices.Find("pear").ToString());
}
```

## Analyzers in a test project

The analyzer of the package reports `GetValue()` and `GetError()` that are not guarded by a check ([WYZ0102](analyzers/WYZ0102.md)), as in the tests above, in a
test project that installs the package. Tests often assert on the error right after asserting `IsFailure`, so lower the severity for them in `.editorconfig`:

```ini
[**/*Tests*/**.cs]
dotnet_diagnostic.WYZ0102.severity = none
```
