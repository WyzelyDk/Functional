# C# 15 unions

[Back to the Result overview](../../src/Result/README.md)

C# 15 adds union types to the language. On a `net11.0` build, `Result<T>` opts in as a union of its two case types, `T` and `Error`,
using the *union member provider* form of a custom union (a `[Union]` type with a nested `IUnionMembers`), so the compiler can check
that a `switch` handles both:

```csharp
string Describe(Result<int> result) => result switch
{
    int value => $"ok: {value}",
    Error error => $"failed: {error.Message}",
};                                        // no discard arm needed: the compiler knows the two cases
```

Things to know:

- It needs the **`net11.0` target** (and a C# 15 compiler). The package has a `net11.0` asset that carries the union members; a project that
  targets `net10.0` or older gets the same library without them.
- It was built and its tests (`ResultUnionTests`, 7 of them) pass with SDK `11.0.100-rc.2`, which was the C# 15 and .NET 11 release
  candidate when this was written; unions were still in preview in the language, so re-check after the final release. It follows the
  documented shape of custom unions
  ([language reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union)). No `LangVersion` setting
  was needed.
- A `default` result is a success holding `default(T)`, so a `switch` over it matches the value case.
- A union gives the language an **implicit conversion from each case type**, so on `net11.0` a `T` or an `Error` converts to a
  `Result<T>` even though the library itself defines no implicit conversions. That is a property of C# unions, not of this library.
  Use the factories if you want to stay explicit.
- A `Result<Error>` cannot be used as a union, because both cases are the same type.

