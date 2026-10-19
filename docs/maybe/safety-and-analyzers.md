# Safety and analyzers

[Back to the Maybe overview](../../src/Maybe/README.md)

```csharp
Maybe<int> unfilled = default;
unfilled.IsNone;           // true: the default value is a valid "no value"
unfilled.ToString();       // "None"
unfilled.ValueOr(7);       // 7
unfilled.GetValue();       // throws InvalidOperationException: "The maybe has no value. Check IsSome first, or use TryGetValue."
```

There is nothing to fail loudly about for a `default` maybe: an unassigned field or array slot simply has no value, which
is what it would mean anyway. The one mistake that throws is reading the value of a none with `GetValue()`, and that is what the analyzer is for.

The package ships one analyzer, a warning that is enabled by default:

| Id | Reports | Fix | Code fix |
|---|---|---|---|
| [`WYZ0201`](analyzers/WYZ0201.md) | `GetValue()` that is not guarded by a check of the same maybe | check `IsSome` / `IsNone` first, or use `TryGetValue`, `ValueOr` or `Match` | no (see the page for why) |

Change the severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.WYZ0201.severity = error
```

The rule numbers follow the scheme in [Analyzer rule IDs](../analyzers.md); the `WYZ02xx` range belongs to this package.

## Nullability is part of the API

- `Maybe<T>` requires `T : notnull`, so the compiler warns when a nullable type is used as `T`.
- `TryGetValue(out var value)` is annotated `[MaybeNullWhen(false)]`: after `if (maybe.TryGetValue(out var value))` the compiler treats `value` as
  not null.
- `Maybe.Some` rejects `null` at run time (`ArgumentNullException`); `Maybe.From` and the implicit conversion turn `null` into none.
- `Map` turns a `null` that its delegate returns into none at run time. A delegate that returns a **nullable reference** compiles without a warning, and
  the result is a `Maybe<string>`, not a `Maybe<string?>` (the delegate is a `Func<T, TResult?>`; pinned by `ConsumerNullabilityTests`).
  A delegate that returns a nullable **value** type such as `int?` behaves the same way, through a second overload constrained to structs: the result is a
  `Maybe<int>` and `null` becomes none (also pinned by `ConsumerNullabilityTests`). `Maybe<int?>` itself is still reported against the constraint
  (`CS8714`), and `Maybe<int> m = nullableInt;` does not compile; use `Maybe.From(nullableInt)`.
- `Maybe.From` has one overload for nullable references and one for nullable value types such as `int?`.

```csharp
using Wyzely.Functional;

Maybe<string> maybe = Maybe.Some("Ada");

if (maybe.TryGetValue(out var name))
{
    var length = name.Length;                   // no nullability warning: name is known to be non-null here
}
```

A compile-checked consumer test project exists in the repository (`test/Maybe.Analyzers.Tests`, with `ConsumerNullabilityTests`, as for
`Result`); the page does not claim more than the annotations above.

The analyzer ships inside the NuGet package. A project that references the library's *source* project, as `samples/Maybe.Samples` does, does not run
it, which is why the samples show the unguarded `GetValue()` on a none throwing without a warning; in a project that installs the package, that call is
reported.

The union support on `net11.0` is checked by the compiler rather than by an analyzer: [C# 15 unions](unions.md).
