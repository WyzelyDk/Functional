# Collections: FirstOrNone, GetValueOrNone, Choose

[Back to the Maybe overview](../../src/Maybe/README.md)

These extension methods turn the "maybe there is one" questions about sequences and dictionaries into a `Maybe<T>`, instead of an
exception (`First`, `Single`, `ElementAt`, the dictionary indexer) or a `null` (`FirstOrDefault` on a reference type).

| API | Returns |
|---|---|
| `source.FirstOrNone()` / `source.FirstOrNone(predicate)` | the first element (that satisfies the predicate), or none |
| `source.LastOrNone()` / `source.LastOrNone(predicate)` | the last element (that satisfies the predicate), or none |
| `source.SingleOrNone()` / `source.SingleOrNone(predicate)` | the only element (that satisfies the predicate); none when there are **none or several** |
| `source.ElementAtOrNone(index)` | the element at the index, or none when it is out of range (a negative index is out of range) |
| `dictionary.GetValueOrNone(key)` | the value of the key, or none when the key is missing |
| `maybes.Choose()` | the values of the somes, in order; the nones are dropped |
| `source.Choose(selector)` | maps every element to a `Maybe` and keeps the values of the somes, in order |

```csharp
using Wyzely.Functional;

int[] numbers = [4, 8, 15, 16];

var first = numbers.FirstOrNone(n => n > 10);            // Some(15)
var last = numbers.LastOrNone();                         // Some(16)
var onlyBig = numbers.SingleOrNone(n => n > 15);         // Some(16)
var ambiguous = numbers.SingleOrNone(n => n > 10);       // None: 15 and 16 both match
var third = numbers.ElementAtOrNone(2);                  // Some(15)
var tenth = numbers.ElementAtOrNone(10);                 // None

var prices = new Dictionary<string, decimal> { ["tea"] = 3m, ["coffee"] = 4m };
var tea = prices.GetValueOrNone("tea");                  // Some(3)
var water = prices.GetValueOrNone("water");              // None

IEnumerable<Maybe<int>> lookups = [Maybe.Some(1), Maybe.None<int>(), Maybe.Some(3)];
var found = lookups.Choose();                            // 1, 3

var lengths = new[] { "a", "", "ccc" }.Choose(text => Maybe.SomeWhen(text.Length > 0, text.Length));   // 1, 3
```

Each of these is run, with its output, in `samples/Maybe.Samples/CollectionsExample.cs`. `Choose` is the one to reach for when you would otherwise write
"for each element, look something up, and keep what was found"; `Zip` (combining independent maybes) and `Flatten` are on [the railway page](the-railway.md).

Things to know:

- **A `null` element counts as no value**, like everywhere else in the library: `FirstOrNone()` of a sequence whose first element is `null` is none.
- **`GetValueOrNone` has one overload, for `IReadOnlyDictionary<TKey, TValue>`.** `Dictionary<TKey, TValue>` implements both that interface
  and `IDictionary<TKey, TValue>`, so two overloads would make the call ambiguous for the most common type. A variable that is typed only as
  `IDictionary<TKey, TValue>` does not match; use `TryGetValue` there or wrap it. See [Design decisions](design-decisions.md).
- **`Choose` is lazy**, like LINQ's `Select` and `Where`: the selector runs while you enumerate.
- **`LastOrNone()` and `ElementAtOrNone()` use the index of an `IList<T>`** when the source is one, and enumerate otherwise.
