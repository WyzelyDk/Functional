# Scope of Wyzely.Functional.Maybe

This library is about **`Maybe`**: a value that may be absent, how to chain operations on it, and how to leave the chain. It is not meant to
be the answer to every situation in which an optional value could be useful. A small, coherent surface is a feature: every public method
costs documentation, tests, analyzers and, for anything on the railway, nine generated variants.

## The admission test for new API

A new public member goes in only if the answer to every question is yes:

1. **Is it about a value that may be absent?** Not about collections in general, HTTP, logging, validation, serialization or dependency
   injection, even if a `Maybe` happens to flow through them. (The collection extensions are the exception that proves the rule: each one
   exists to answer "maybe there is one" and nothing else.)
2. **Does the core need it, or can a user write it in a few lines with the public API?** If three lines of `Bind`/`Match` do it, it
   does not belong here. Convenience that is merely "nice" is declined, because it can never be removed again.
3. **Is the cost proportional?** An operation on the railway becomes **nine methods** (sync/`Task`/`ValueTask` receiver times
   delegate), plus tests and documentation. Ask whether the operation is used often enough to carry that.
4. **Does it need a dependency, or does it only matter to some applications?** Then it belongs in a separate package, never in this one.
   This package stays free of dependencies outside the BCL.
5. **Can it be misused in a way an analyzer cannot catch?** If it makes it easy to bypass the safety the library provides
   (`GetValue()` is checked, `Some` rejects `null`), it needs a good reason.

## Deliberately not in this library

- **`Result`, `Either`, `Unit` and other functional types.** They are different concepts. `Result` has its own package, and the conversions
  between `Maybe` and `Result` live in the umbrella package `Wyzely.Functional`, not in either of the two
  ([Interop](../functional/interop.md)).
- **`Sequence` and `Traverse`.** Turning a collection of maybes into a maybe of a collection is the territory of a larger functional
  library. `Choose` covers the common case (keep the values that exist), and a short loop covers "all or nothing".
- **Arithmetic and other helpers on options** (adding, multiplying or summing two maybes). A `Zip` with a function does the same in
  one line (for two to five maybes), and the helpers multiply with every operator.
- **`Zip` beyond five maybes.** The limit is a single setting of the code generator; more than five independent maybes is a sign to use query syntax or a
  small record instead.
- **`Apply` and other applicative machinery.** `Zip` combines independent maybes, and query syntax chains dependent ones; a third style
  is a vocabulary cost without a use case here.
- **`ToEither`, `ToValidation` and conversions to other libraries' types.** An adapter package, if anyone needs it; `ToResult` is the one
  conversion, because `Result` is part of the same family.
- **Our own union or discriminated-union types.** `Match` and `Switch` are the way to handle both cases on every target. Where the *language* has
  unions (C# 15, `net11.0`), `Maybe<T>` opts in so that a `switch` on the value and `null` is exhaustive; that is the compiler's feature, not a type of
  ours ([C# 15 unions](unions.md)).
- **`Fold`, `Iter` and other collection-style members on a maybe.** `Match`, `Switch` and `ToEnumerable()` cover them.
- **`Maybe.Try` (turning an exception into none).** It would throw the cause away; `Result.Try` keeps it, and `ToMaybe()` in the umbrella
  package drops the error on purpose when only the absence matters. See [Design decisions](design-decisions.md).
- **JSON, EF Core and ASP.NET Core integrations, serialization attributes.** Adapter packages.
- **Logging and telemetry.** The library does not log.

## How to propose a change

Say which of the five questions the change answers and how, and show the three lines a user would write today without it. If it
adds a railway operation, say how often it is needed, because that decides whether it earns its nine variants.
