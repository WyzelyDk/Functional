# The railway: Map, Bind, Match and friends

[Back to the Maybe overview](../../src/Maybe/README.md)

The operations are extension methods on `Maybe<T>`. While there is a value the chain carries it along; the first none skips every step
that follows, without running its delegate.

| Method | Runs the delegate when | Does |
|---|---|---|
| `Map(f)` | there is a value | transforms the value; a `null` that `f` returns becomes none |
| `Bind(f)` | there is a value | chains an operation that itself returns a `Maybe` |
| `Filter(predicate)` | there is a value | turns the maybe into none when the predicate is false |
| `Tap(f)` | there is a value | runs a side effect, returns the maybe unchanged |
| `TapNone(f)` | there is none | runs a side effect, returns the maybe unchanged |
| `Or(f)` | there is none | tries an alternative that returns a `Maybe` |
| `ValueOr(f)` | there is none | leaves the railway: the value, or what `f` computes (the instance method `ValueOr(fallback)` takes a plain value) |
| `Match(onSome, onNone)` | always | leaves the railway: one value out |
| `Switch(onSome, onNone)` | always | leaves the railway for side effects |

```csharp
using Wyzely.Functional;

var logged = new List<string>();

var title = Maybe.Some("  Hitchhiker's Guide  ")
    .Map(text => text.Trim())                                   // Some("Hitchhiker's Guide")
    .Filter(text => text.Length > 3)                            // still Some: the predicate holds
    .Tap(text => logged.Add($"found {text}"))                   // runs, because there is a value
    .Bind(text => text.Contains(' ') ? Maybe.Some(text) : Maybe.None<string>())
    .Or(() => Maybe.Some("untitled"))                           // not needed here: there is a value
    .Map(text => text.ToUpperInvariant())
    .Match(text => text, () => "no title");                     // "HITCHHIKER'S GUIDE"

var empty = Maybe.None<string>()
    .TapNone(() => logged.Add("nothing to show"))               // runs, because there is none
    .Or(() => Maybe.Some("untitled"))                           // the alternative takes over
    .ValueOr(() => "no title");                                 // "untitled"
```

`Map` turns a `null` result of its delegate into none, because a `Maybe<T>` cannot hold `null`. The delegate is a `Func<T, TResult?>`, so a
lambda that returns a **nullable reference**, such as `person => person.Nickname` with a `string? Nickname`, compiles **without a warning**:
`TResult` is inferred as the non-nullable `string`, the result is a `Maybe<string>`, and a `null` result becomes none at run time
(`ConsumerNullabilityTests` pins this, also for `Task` delegates). The same holds for the result selectors of query syntax and `Zip`.

```csharp
var person = Maybe.Some(new NamedPerson("Ada", null));
var nickname = person.Map(p => p.Nickname);                                  // Maybe<string>: None, without a warning
var same = person.Bind(p => Maybe.From(p.Nickname));                        // the explicit form: also None
```

`NamedPerson` is a record with `string Name`, `string? Nickname` and `int? Age`. A **nullable value type** behaves the same way, through a second overload of
`Map` constrained to structs: `person.Map(p => p.Age)` is a `Maybe<int>` (never a `Maybe<int?>`) and a `null` age becomes none. The explicit form
`person.Bind(p => Maybe.From(p.Age))` gives the same result. `Maybe.From` is also the way to convert an `int?` that is not in a lambda: the implicit
conversion exists for nullable references only.

## LINQ query syntax, `Zip` and `Flatten`

```csharp
using Wyzely.Functional;

var total = from price in Maybe.Some(40)
            from tax in Maybe.Some(2)
            where price > 0
            select price + tax;                          // Some(42); None at the first missing value or false condition

var pair = Maybe.Some(1).Zip(Maybe.Some("one"));         // Some((1, "one")): both values, or none when either is missing
var sum = Maybe.Some(1).Zip(Maybe.Some(2), (a, b) => a + b);   // Some(3)
var triple = Maybe.Some(1).Zip(Maybe.Some("one"), Maybe.Some(true));   // Some((1, "one", true)), elements named First, Second, Third

Maybe<Maybe<int>> nested = Maybe.Some(Maybe.Some(7));
var flat = nested.Flatten();                             // Some(7)
```

`Select`, `SelectMany` and `Where` are `Map`, `Bind` and `Filter` under the names that query syntax requires. They live in
`MaybeLinqExtensions`.

**`Zip`** combines two to five independent maybes: the result is a some only when all of them are. Without a selector it is a tuple with the
named elements `First`, `Second`, ... `Fifth`; with a selector (a `Func` of the values that may return a `null`, which becomes none) the values are
combined at once. Unlike `Result.Zip` there is no error to aggregate, so the answer is simply "all" or "none". The overloads are generated
into `MaybeExtensions` (`src/Maybe/Generated/Zip`, 8 files: a tuple and a selector form for each count), and the limit of five is one setting
of the generator (`GeneratorSettings.MaxZipArity`), not something that can be changed by a consumer. For more than five, nest `Zip`s or use query syntax.

**`Flatten`** turns a `Maybe<Maybe<T>>` into a `Maybe<T>`, for a maybe, a `Task<Maybe<Maybe<T>>>` and a `ValueTask<Maybe<Maybe<T>>>`. It is `Bind` with
the identity function (`MaybeExtensions.Flatten.cs`), so it is mostly useful when the nesting came from somewhere else; when you are writing the step
yourself, `Bind` instead of `Map` never creates the extra level.

[A runnable tour of all of this](../../samples/Maybe.Samples/LinqExample.cs).

## Samples

`samples/Maybe.Samples` has one file per theme with the examples of this page (`RailwayExample.cs`) and of the others, each printing labelled lines;
`dotnet run --project samples/Maybe.Samples -c Release` runs them all.

## Taking a maybe apart with `switch` (net11.0)

On a `net11.0` build with C# 15, a `switch` with an arm for the value and an arm for `null` is exhaustive, so you can use it instead of `Match`
without a discard arm: `maybe switch { int value => ..., null => ... }`. See [C# 15 unions](unions.md).

## What is not here

There is no `Apply`, `Sequence`, `Traverse` or arithmetic on maybes, and no `Fold`; [Scope](scope.md) says why and what to write instead.
