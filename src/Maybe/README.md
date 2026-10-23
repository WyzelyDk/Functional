# Wyzely.Functional.Maybe

[![NuGet](https://img.shields.io/nuget/v/Wyzely.Functional.Maybe)](https://www.nuget.org/packages/Wyzely.Functional.Maybe)
[![Downloads](https://img.shields.io/nuget/dt/Wyzely.Functional.Maybe)](https://www.nuget.org/packages/Wyzely.Functional.Maybe)

**A `Maybe` type for .NET: a value that may be absent, without `null`. A struct that allocates nothing, whose `default` is simply "no value", and that chains with synchronous and asynchronous steps alike.**

> **Not published yet.** None of the packages of this repository has been released to NuGet at the time of writing, so the NuGet badges above show
> "not found" until the first release, and the install command below works from then on.

```csharp
using Wyzely.Functional;

// Task<Maybe<Customer>>     FindCustomerAsync(int id)             a Task step
// ValueTask<Maybe<string>>  FindEmailAsync(Customer customer)     a ValueTask step

var message = await FindCustomerAsync(7)
    .Filter(customer => customer.IsActive)
    .Bind(FindEmailAsync)
    .Map(email => email.Trim())
    .Match(email => $"Mail sent to {email}.", () => "Nobody to mail.");

// "Mail sent to ada@example.com."   (an unknown or inactive customer, or one without an email: "Nobody to mail.")
```

One chain, no `await` in the middle, no `null` check between the steps: the first missing value skips the rest.

> **Lineage:** built on code that has been in production use in many codebases, then expanded, improved and documented with Claude Code.
> [What that does and does not mean](https://github.com/WyzelyDk/Functional#where-this-code-comes-from).

## Fluent from first step to last

The whole railway is available as chained calls: `Map`, `Bind`, `Filter`, `Tap`, `TapNone` and `Or` continue the chain; `ValueOr`, `Match`
and `Switch` leave it. Each accepts a synchronous, `Task` or `ValueTask` receiver and delegate in any mix, so the chain above needs no
`await` in the middle, and LINQ query syntax, `Zip` (two to five maybes) and `Flatten` follow the same rules.
[The full list](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/the-railway.md).

## Why this exists

"There may be no customer" is an ordinary situation, and `null` says it badly: nothing in the type tells the reader, and a missing check is
found at run time. C#'s nullable reference types help for references and `int?` exists for value types, but neither gives you a chain
you can write without a check between the steps, or one vocabulary that reads the same for `string` and `int`.

There are good libraries for this already (CSharpFunctionalExtensions, LanguageExt, Optional and others). This one exists because we
wanted a specific combination, and we wanted it to sit next to the [`Result` type](https://github.com/WyzelyDk/Functional/blob/main/src/Result/README.md) of the same family:

- **`default` is none.** A struct always has a default value; here that value is a perfectly good "no value", so an unassigned field or array slot
  is never an error.
- **`where T : notnull`.** Absence is what `None` is for, so a `Maybe<string?>` with two ways to be empty is not something you meet by accident.
- **Every sync / `Task` / `ValueTask` combination under the same method name**, so chains never need an `await` in the middle.
- **An analyzer in the package** for the one mistake that matters: `GetValue()` without a check.
- **No dependencies** outside the BCL, and **performance that is measured**: nothing allocated on the synchronous path.

It also gives something up: it is not published yet and has not been run in production as this package, it does less than the larger libraries
(no arithmetic on options, no `Sequence`/`Traverse`, no JSON or EF Core integration), and it is opinionated. If you already use one of the libraries
above and are happy, there is little reason to switch.
[The honest long version, including when to use something else.](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/why.md)

## Why use a library when writing your own Maybe is easy?

It is easy. A struct with a value, a flag, `Map`, `Bind` and `Match` is about twenty lines, and `default` is already "no value" if the flag is the
state ([the sketch](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/why.md#why-use-a-library-when-writing-your-own-maybe-is-easy)).
What the library adds is everything around that core, and each item is checked against the source or a test:

- **One way to be empty.** `Maybe.Some(null)` throws, `Maybe.From(x)` turns `null` into none, `Maybe<string?>` is reported against `notnull`.
- **Decided conversions.** A `T` or a `null` reference converts to a maybe implicitly; nothing converts out of one (pinned by tests).
- **The generated railway:** nine operations in every sync / `Task` / `ValueTask` combination, **90 methods** (`Map` has a second form for nullable value types), each with a generated test, and no
  `await` in the middle of a chain.
- **Lookups:** `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone`, `Choose`; and LINQ, `Zip` and `Flatten`.
- **An analyzer (`WYZ0201`) and nullability annotations** that are pinned by tests which compile consumer code.
- **Configuration, equality, ordering and formatting**, including redaction of values in `ToString()`.
- **Measured size and allocation** (only what `PerformanceTests` checks), **interop with `Result`** in the umbrella package, and **C# 15 union support**.

A library costs something too: a vocabulary your team has to learn, generated code you did not write (102 files), one more package to keep updated,
and an API that can still change before 1.0. When your use is one corner of one project, with synchronous steps and a handful of call sites, writing
your own is the better choice. [The whole argument, with the cost of a dependency.](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/why.md)

## Install

```shell
dotnet add package Wyzely.Functional.Maybe
```

| Target | Package dependencies |
|---|---|
| `net10.0` | none |
| `net8.0` (also used by .NET 9) | none |
| `netstandard2.0` (.NET Framework 4.6.1+, Unity, ...) | `System.Threading.Tasks.Extensions`, only because `ValueTask` is not part of `netstandard2.0` |
| `net11.0` | none; adds [C# 15 union support](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/unions.md) |

The analyzer comes with the package. Use **C# 13 or later** to write `async` lambdas without ambiguity
([why](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/async.md#async-lambdas-and-the-language-version)). All public types are in the namespace
`Wyzely.Functional`. If you use `Result` as well, install the umbrella package
[`Wyzely.Functional`](https://github.com/WyzelyDk/Functional/blob/main/src/Functional/README.md) instead.

## A five-minute tour

### Create a maybe and read it

```csharp
using Wyzely.Functional;

string? nickname = null;
var count = 3;

Maybe<string> name = Maybe.Some("Ada");                 // Some(Ada); Maybe.Some(null) throws ArgumentNullException
Maybe<string> nobody = Maybe.None<string>();            // None
Maybe<string> nick = Maybe.From(nickname);              // None, because the reference is null
Maybe<int> several = Maybe.SomeIf(count > 1, count);  // Some(3)
Maybe<string> plain = "Ada";                            // Some(Ada); a null reference converts to None

name.IsSome;                                            // true
name.ValueOr("nobody");                                 // "Ada"
nobody.ValueOr("nobody");                               // "nobody"
name.TryGetValue(out var value);                        // true, and value is "Ada"
```

A plain value converts to a maybe implicitly, and a `null` reference becomes none, so `return user;` in a method that returns
`Maybe<User>` does the right thing. That is a deliberate exception to the rule that nothing converts to a `Result` implicitly:
there, `return false;` would read like a failure, whereas here the absence of a value is exactly what `null` means
([why](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/design-decisions.md)).

### Chain steps; the first missing value skips the rest

```csharp
var directory = new Dictionary<string, string> { ["ada"] = "Ada Lovelace", ["alan"] = "Alan Turing" };

string Describe(string key) => directory.GetValueOrNone(key)
    .Filter(name => name.StartsWith("Ada", StringComparison.Ordinal))
    .Map(name => name.ToUpperInvariant())
    .ValueOr("nobody");

Describe("ada");     // "ADA LOVELACE"
Describe("alan");    // "nobody": filtered out
Describe("zed");     // "nobody": not in the dictionary
```

`Map`, `Bind`, `Filter`, `Tap`, `TapNone`, `Or`, `ValueOr`, `Match` and `Switch`:
[the railway](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/the-railway.md). A delegate of `Map` may return a nullable reference
(`person => person.Nickname` with a `string?`) or a nullable value type (`person => person.Age` with an `int?`) without a warning, and a `null` result
becomes none; the result is a `Maybe<string>` or a `Maybe<int>`, never a `Maybe<string?>` or a `Maybe<int?>`.

### Mix synchronous and asynchronous steps freely

The receiver can be a maybe, a `Task<Maybe<T>>` or a `ValueTask<Maybe<T>>`, and each delegate can be synchronous, return a `Task`, or return
a `ValueTask`. All nine combinations exist under the same name, which is why the example at the top has no `await` between the steps.
[Async in detail](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/async.md).

### Ask a collection a question

```csharp
int[] numbers = [4, 8, 15, 16];

numbers.FirstOrNone(n => n > 10);      // Some(15)
numbers.FirstOrNone(n => n > 100);     // None
numbers.SingleOrNone(n => n > 15);     // Some(16)
numbers.SingleOrNone(n => n > 10);     // None: more than one match

IEnumerable<Maybe<int>> lookups = [Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3)];
lookups.Choose();                      // 1, 3: the values of the somes, in order
```

`FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone` and `Choose`:
[collections](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/collections.md).

### Combine maybes

```csharp
var pair = Maybe.Some(1).Zip(Maybe.Some("one"));                       // Some((1, "one")): all values, or none when any is missing
var triple = Maybe.Some(1).Zip(Maybe.Some("one"), Maybe.Some(true));   // Some((1, "one", true)); up to five maybes
var sum = Maybe.Some(1).Zip(Maybe.Some(2), (a, b) => a + b);           // Some(3)

var flat = Maybe.Some(Maybe.Some(7)).Flatten();                        // Some(7); also on Task and ValueTask receivers
```

### Let the compiler catch the mistake

```csharp
Maybe<int> missing = default;           // a valid "no value"; nothing to warn about

missing.GetValue();                     // WYZ0201: not guarded by an IsSome check; throws InvalidOperationException

if (missing.IsSome)
{
    var fine = missing.GetValue();      // guarded, no warning
}
```

[Safety and analyzers](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/safety-and-analyzers.md).

### On net11.0: switch exhaustively (C# 15)

```csharp
string Describe(Maybe<int> maybe) => maybe switch
{
    int value => $"some: {value}",
    null => "none",
};                                      // no discard arm: the compiler knows the two cases; the none is the null case
```

This needs a package built with the .NET 11 SDK, was built and tested with SDK `11.0.100-rc.2` (7 tests), and may change with the final language release:
[C# 15 unions](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/unions.md).

## Samples

[`samples/Maybe.Samples`](https://github.com/WyzelyDk/Functional/tree/main/samples/Maybe.Samples) is a runnable console project with one file per theme (basics, the
railway, async, collections, LINQ/`Zip`/`Flatten`, nullability, equality and formatting, configuration, and the C# 15 union), in a small customer and order
domain. Each file says what the scenario is and why, and prints labelled lines; the same lines are checked by tests.

```shell
dotnet run --project samples/Maybe.Samples -c Release
```

## Everything else

| Topic | |
|---|---|
| [Getting started](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/getting-started.md) | install, core type, the full list of ways to create and read a maybe, the samples |
| [The railway](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/the-railway.md) | every operation and what it does, `Zip`, `Flatten` |
| [Async](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/async.md) | the nine variants and `async` lambdas |
| [Collections](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/collections.md) | `FirstOrNone`, `GetValueOrNone`, `Choose` and friends |
| [Safety and analyzers](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/safety-and-analyzers.md) | `default`, `GetValue()`, `WYZ0201`, nullability |
| [C# 15 unions](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/unions.md) | exhaustive `switch` on `net11.0` |
| [Configuration](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/configuration.md) | what you can change, and the defaults |
| [Performance](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/performance.md) | what is guaranteed and measured, and what is not claimed |
| [How it compares](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/comparison.md) | a factual comparison with five other libraries: type, `default` and `null`, API surface |
| [Why this one](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/why.md) | why a library, when to use it, and when to use something else |
| [Design decisions](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/design-decisions.md) | why it is the way it is, including what was rejected |
| [Scope](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/scope.md) | what this package will and will not take |

## Performance, in short

A `Maybe<int>` is 8 bytes, a `Maybe<string>` is two references wide, a synchronous chain allocates nothing, on success and on none, and a `Task` chain
over a reference type that stays none allocates nothing either, because one completed task per type is cached (checked by tests, on .NET 8 and later). Timings
are measurements, not claims: benchmarked against five other libraries, for value and reference types, a four-step synchronous chain took about 5 ns
without allocating, as it did in LanguageExt and CSharpFunctionalExtensions for an `int`; Funcky and Optional took 1.6 to 4.5 times as long and allocated.
[What is claimed and how to run the benchmarks](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/performance.md).

## Contributing

Bug reports and ideas are welcome. Please read the [scope](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/scope.md) first; it says what will not be
added and why. [Building, testing and how the generated code works](https://github.com/WyzelyDk/Functional/blob/main/docs/maybe/contributing.md).

## License

[MIT](https://github.com/WyzelyDk/Functional/blob/main/LICENSE)
