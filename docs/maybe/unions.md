# C# 15 unions

[Back to the Maybe overview](../../src/Maybe/README.md)

C# 15 adds union types to the language. On a `net11.0` build, `Maybe<T>` opts in as a union of its one case type, `T`, and the absence of a
value, which a pattern sees as `null`. A `switch` that has an arm for `T` and an arm for `null` is therefore exhaustive, and the compiler can
check that you handled both:

```csharp
string Describe(Maybe<int> maybe) => maybe switch
{
    int value => $"some: {value}",
    null => "none",
};                                        // no discard arm needed: the compiler knows the two cases
```

Here `none` *is* the `null` case. That does not mean a maybe holds a `null`: `Maybe<T>` still cannot hold one (`where T : notnull`). The `null`
arm is how the language spells "the union has no case value", and the library tells the compiler that this is what a none is.

## What you get, and when it applies

- **Exhaustiveness checked by the compiler.** Drop the `null` arm, or the `T` arm, and the compiler reports an incomplete `switch`, with no
  discard arm to hide it. `Match` also forces you to handle both cases; the union form is for code that prefers a `switch`, for example
  with nested patterns (`Customer { IsActive: true } customer => ...`).
- **A `default` maybe matches the `null` arm**, because the default of a maybe is none.
- **The type of the case is matched by type**, so for a `Maybe<string>` the arm is `string text => ...`, for a `Maybe<Customer>` it is
  `Customer customer => ...`, and the usual pattern features (property patterns, `when` guards) work inside the arm.
- **An implicit conversion from the case type.** The language gives a union an implicit conversion from each case type. A `Maybe<T>` already
  converted implicitly from `T` (and from `null`), so on `net11.0` nothing new is visible for that conversion. It is a property of C# unions,
  not an extra rule of this library.

## Things to know

- It needs the **`net11.0` target** (and a C# 15 compiler). The package has a `net11.0` asset that carries the union members; a project that
  targets `net10.0` or older gets the same library without them and cannot use the form above.
- It was built, and its tests (`MaybeUnionTests`, 7 of them) pass, with SDK `11.0.100-rc.2`, which was the C# 15 and .NET 11 release
  candidate when this was written. Unions were still in preview in the language then, so re-check after the final release. The
  implementation follows the documented shape of custom unions
  ([language reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union)), in the *union member
  provider* form: a `[Union]` type with a nested `IUnionMembers` interface that the compiler reads, because a maybe is created through the
  factory methods and not through public constructors. No `LangVersion` setting was needed.
- Behavior was checked by tests on `net11.0` only for `Maybe<int>` and `Maybe<string>` (a value type and a reference type). Other
  case types were not tested separately.
- The members of `IUnionMembers` are for the compiler. Use pattern matching, not those members.
- This is **not a replacement for the railway.** `Map`, `Bind`, `Filter` and the rest work the same on every target; the union form only
  adds a second way to take a maybe apart.
- The same support exists on `Result<T>`, with the cases `T` and `Error`: [`Result` and unions](../result/unions.md).

## A runnable example

`samples/Maybe.Samples/UnionExample.cs` has the three arms above with nested patterns. It is compiled only for `net11.0` (the sample project
targets `net10.0` and `net11.0`); see [the samples](../../samples/Maybe.Samples/Program.cs).
