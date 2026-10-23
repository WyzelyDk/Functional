# Getting started

[Back to the Maybe overview](../../src/Maybe/README.md)

## Install

```shell
dotnet add package Wyzely.Functional.Maybe
```

| Target | Package dependencies |
|---|---|
| `net10.0` | none |
| `net8.0` (also used by .NET 9) | none |
| `netstandard2.0` (.NET Framework 4.6.1+, Unity, Xamarin, ...) | `System.Threading.Tasks.Extensions`, only because `ValueTask` is not part of `netstandard2.0` |
| `net11.0` | none; adds [C# 15 union support](unions.md) |

The analyzer is part of the package; there is nothing else to install. For the best experience with `async` lambdas use **C# 13 or
later**; see [Async lambdas and the language version](async.md#async-lambdas-and-the-language-version). If you also use `Result`, install
the umbrella package [`Wyzely.Functional`](../../src/Functional/README.md) instead of the two separately.

## Quick start

```csharp
using System.Globalization;
using Wyzely.Functional;

static Maybe<int> ParseAge(string text) =>
    int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var age)
        ? Maybe.Some(age)
        : Maybe.None<int>();

var message = ParseAge("42")
    .Filter(age => age >= 18)
    .Map(age => $"Welcome, age {age}.")
    .ValueOr("Sorry, we cannot let you in.");
// "Welcome, age 42."   (for "abc" or "12": "Sorry, we cannot let you in.")
```

## The core type

### `Maybe<T>`

`Maybe<T>` is a `readonly struct` with the constraint `where T : notnull`. A maybe is in exactly one of two states:

| State | How you get it | What it means |
|---|---|---|
| **Some** | `Maybe.Some(value)`, `Maybe.From(value)` for a non-null value, an implicit conversion from a non-null value | there is a value |
| **None** | `Maybe.None<T>()`, `default(Maybe<T>)`, an array element or field that was never assigned, `Maybe.From(reference)` when the reference is `null` | there is no value |

There is **no uninitialized state**: the `default` value of a maybe *is* none, and it is safe to read. A struct always
has a default value, and for "a value that may be absent" the default has an obvious meaning. The struct holds the value and one flag,
so creating, passing and chaining maybes allocates nothing.

| Create | |
|---|---|
| `Maybe.Some(value)` | a value; throws `ArgumentNullException` when `value` is `null` (use `From` when it may be) |
| `Maybe.None<T>()` | no value |
| `Maybe.From(value)` | from a reference that may be `null`, or from a nullable value type such as `int?`: `null` becomes none |
| `Maybe.SomeIf(condition, value)` / `Maybe.NoneIf(condition, value)` | a value only when (or unless) a condition holds; `value` is only checked for `null` when it is used |
| implicit conversion | a `T` converts to `Maybe<T>`, and a `null` reference converts to none |

```csharp
using Wyzely.Functional;

string? nickname = null;
int? score = 12;

Maybe<string> fromReference = Maybe.From(nickname);        // None
Maybe<int> fromNullable = Maybe.From(score);               // Some(12)
Maybe<int> unassigned = default;                           // None: the default value is a valid "no value"
Maybe<string> converted = "Ada";                           // Some(Ada), by implicit conversion
Maybe<string> convertedNull = (string?)null;               // None
```

The implicit conversion is deliberate, and it is the one place where this library differs from `Result`, which converts nothing
implicitly. `return user;` in a method that returns `Maybe<User>` does the right thing, because a `null` reference *means* "no value".
The reasoning is in [Design decisions](design-decisions.md#an-implicit-conversion-where-null-means-none).

| Read | |
|---|---|
| `IsSome`, `IsNone` | the state |
| `GetValue()` | the value; throws `InvalidOperationException` when there is none. A method, not a property, and the analyzer [`WYZ0201`](analyzers/WYZ0201.md) checks that it is guarded |
| `TryGetValue(out v)` | the non-throwing way; annotated so that `v` is known to be non-null when it returns `true` |
| `ValueOr(fallback)` | the value, or the fallback. There is also `ValueOr(() => fallback)` for a fallback that is costly to compute |
| `Any(predicate)` | whether there is a value that satisfies the predicate (`false` for none) |
| `All(predicate)` | whether there is no value, or the value satisfies the predicate (`true` for none) |
| `Contains(value)` | whether there is a value equal to `value`, with the default equality of the type |
| `ToEnumerable()` | a sequence of zero or one values |
| `AsNullable()` | for a maybe of a value type: the value as a `T?`, or `null` |

```csharp
using Wyzely.Functional;

var some = Maybe.Some(5);
var none = Maybe.None<int>();

some.ValueOr(0);                          // 5
none.ValueOr(0);                          // 0
some.AsNullable();                        // 5, as an int?
none.AsNullable();                        // null
none.ToEnumerable();                      // an empty sequence
some.Any(value => value > 3);             // true: there is a value and it satisfies the condition
none.Any(value => value > 3);             // false
none.All(value => value > 3);             // true: there is no value that fails the condition
some.Contains(5);                         // true
some == Maybe.Some(5);                    // true: value equality
none == default;                          // true: every none is equal
none < some;                              // true: none sorts before every some
some.ToString();                          // "Some(5)"
none.ToString();                          // "None"
```

Two somes are equal when their values are, and two nones are always equal. None sorts before every some, and somes sort by their values.
`ToString()` never throws; its text, and whether it shows the value at all, are [settings](configuration.md).

On a `net11.0` build, a maybe is also a C# 15 union of its value and `null`, so `maybe switch { int value => ..., null => ... }` needs no discard arm
([C# 15 unions](unions.md)).

## Samples

`samples/Maybe.Samples` is a runnable console project with one example file per theme, in a small customer and order domain, each with the scenario and the
reason in its heading comment:

| File | Shows |
|---|---|
| `BasicsExample.cs` | `Some`, `None`, `From`, `SomeIf`, `NoneIf`, the implicit conversion, `TryGetValue`, `ValueOr`, `Match`, why `GetValue` is a method |
| `RailwayExample.cs` | `Map`, `Bind`, `Filter`, `Tap`, `TapNone`, `Or`, `ValueOr(Func)`, `Match`, `Switch` |
| `AsyncExample.cs` | all nine sync, `Task` and `ValueTask` combinations, a mixed chain, async lambdas |
| `CollectionsExample.cs` | `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `GetValueOrNone`, `Choose` |
| `LinqExample.cs` | query syntax, `Zip`, `Flatten` |
| `NullabilityExample.cs` | nullable references and nullable value types, `AsNullable` |
| `EqualityExample.cs` | equality, ordering, hash codes, `ToString()` |
| `ConfigurationExample.cs` | `MaybeConfiguration`, redaction, `ContinueOnCapturedContext` |
| `UnionExample.cs` | the C# 15 union `switch` (`net11.0` builds only) |

```shell
dotnet run --project samples/Maybe.Samples -c Release
```

The samples are tests as well: `test/Samples.Tests/MaybeSamplesTests.cs` runs each example and checks the lines it prints.
