# Getting started with Wyzely.Functional

[Back to the Wyzely.Functional overview](../../src/Functional/README.md)

This page is a guided tour of the umbrella package. If you use only one of `Result` or `Maybe`, start with
[Result](../result/getting-started.md) or [Maybe](../maybe/getting-started.md) instead. Nothing is published on NuGet yet, so the install
command below describes the planned package.

## Install

```shell
dotnet add package Wyzely.Functional
```

It brings `Wyzely.Functional.Result` and `Wyzely.Functional.Maybe` (and their analyzers) as dependencies. One `using Wyzely.Functional;` gives you
everything. Use C# 13 or later to write `async` lambdas without ambiguity.

## The idea in one paragraph

A lookup may find nothing, and that is a `Maybe`. An operation may fail, and that is a `Result`. Real workflows contain both, in order: *find the
customer, then charge them*. The two types do not know about each other, because the packages are independent, so the umbrella package adds the
only two methods that connect them: `ToResult` (absent becomes failed, with the error you choose) and `ToMaybe` (failed becomes absent, error dropped).

## Step 1: from absent to failed

```csharp
using Wyzely.Functional;

var found = Maybe.Some("ada@example.com");
var missing = Maybe.None<string>();

var success = found.ToResult(Error.NotFound("No such customer."));           // success with the value
var failure = missing.ToResult(() => Error.NotFound("No such customer."));   // failure; the factory runs only for none
```

Pass an `Error` you already hold, or a `Func<Error>` when the error carries data from this call and should only be built for the failure. The
library never invents the error: only the caller knows whether an absence is a typo (`NotFound`) or a broken order (`Unexpected`). Run
[`AbsentToFailedExample`](../../samples/Functional.Samples/AbsentToFailedExample.cs) to see the eager and lazy forms, a sensitive detail being redacted, and the null checks.

## Step 2: from failed to absent

```csharp
using Wyzely.Functional;

var someValue = Result.Success(5).ToMaybe();                  // Some(5)
var noValue = Result.Failure<int>("It broke.").ToMaybe();     // None: the error is dropped, on purpose
```

Use it where only "is there a value" matters, for example to keep the customers that passed validation with `Choose`. If anyone will ask why there is
nothing, stay with the `Result`. [`FailedToAbsentExample`](../../samples/Functional.Samples/FailedToAbsentExample.cs) shows what is dropped.

## Step 3: one chain, no await in the middle

```csharp
using Wyzely.Functional;

// Task<Maybe<Customer>>      FindCustomerAsync(int id)
// ValueTask<Result<Receipt>> ChargeAsync(Customer customer)

var outcome = await FindCustomerAsync(7)
    .ToResult(Error.NotFound("No such customer."))
    .Bind(ChargeAsync)
    .Map(receipt => receipt.Number);
```

Both methods exist for plain, `Task` and `ValueTask` receivers, and the railway steps on either side accept every mix, so the chain keeps
its shape. The first failure skips the remaining steps. [`AsyncWorkflowExample`](../../samples/Functional.Samples/AsyncWorkflowExample.cs)
extends this with `Filter`, a synchronous rule and a failing gateway, and prints each outcome.

## What to read next

| Topic | Page |
|---|---|
| Every overload of `ToResult` and `ToMaybe` | [Interop](interop.md) |
| Whether you need the umbrella package at all | [Why an umbrella package](why.md) |
| Mixing collections and results (`Choose`, `Combine`, `Partition`, `Zip`) | [`CollectionsExample`](../../samples/Functional.Samples/CollectionsExample.cs) |
| The two settings that affect awaits | [`ConfigurationExample`](../../samples/Functional.Samples/ConfigurationExample.cs) |
| The analyzers that arrive with the package | [`AnalyzersExample`](../../samples/Functional.Samples/AnalyzersExample.cs), [rule IDs](../analyzers.md) |
| A scenario you can copy | [`CheckoutExample`](../../samples/Functional.Samples/CheckoutExample.cs) |

Run all seven examples with `dotnet run --project samples/Functional.Samples`.
