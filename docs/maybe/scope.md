# Scope of Wyzely.Functional.Maybe

This library is about **`Maybe`**: a value that may be absent, how to chain operations on it, and how to leave the chain.
It is not meant to be the answer to every situation in which an optional value could be useful.
A coherent surface is a feature: every public method is something a developer has to find, read and rely on for as long as the library exists.

## The admission test for new API

A new public member goes in only if the answer to every question is yes:

1. **Is it about a value that may be absent?**
   Not about collections in general, HTTP, logging, validation, serialization or dependency injection, even if a `Maybe` happens to flow through them.
   (The collection extensions are the exception that proves the rule: each one exists to answer "maybe there is one" and nothing else.)
2. **Is the name self-explanatory, and is it the only name for what it does?**
   A developer who knows other languages or libraries should recognise what it does from the name alone.
   It is not a second name for something the library already has, and the same concept has the same name in `Result` and `Maybe`.
   Convenience is welcome when its name makes clear what it is for.
   An operation that belongs to a family is added for the whole family (every receiver, delegate kind and result form), never for part of it.
3. **Is it worth its place in the surface for good?**
   Writing, testing and documenting an operation is cheap, because the code generator and the tooling do the repetitive part
   (an operation on the railway becomes **nine methods**: sync/`Task`/`ValueTask` receiver times delegate).
   What stays expensive is what every developer who uses the library pays for as long as the operation exists:
   one more entry to find or ignore in IntelliSense and in the documentation,
   and a contract that cannot be changed or removed after 1.0 without a breaking change.
   Ask whether enough developers will recognise the operation and reach for it to justify that.
4. **Does it need a dependency, or does it only matter to some applications?** Then it belongs in a separate package, never in this one.
   This package stays free of dependencies outside the BCL.
5. **Can it be misused in a way an analyzer cannot catch?**
   If it makes it easy to bypass the safety the library provides (`GetValue()` is checked, `Some` rejects `null`), it needs a good reason.

## Deliberately not in this library

- **`Result`, `Either`, `Unit` and other functional types.** They are different concepts.
  `Result` has its own package, and the conversions between `Maybe` and `Result` live in the umbrella package `Wyzely.Functional`,
  not in either of the two ([Interop](../functional/interop.md)).
- **`Sequence` and `Traverse`.** Turning a collection of maybes into a maybe of a collection is the territory of a larger functional library.
  `Choose` covers the common case (keep the values that exist), and a short loop covers "all or nothing".
- **Arithmetic and other helpers on options** (adding, multiplying or summing two maybes).
  A `Zip` with a function does the same in one line (for two to five maybes), and the helpers multiply with every operator.
- **`Zip` beyond five maybes.**
  The limit is a single setting of the code generator; more than five independent maybes is a sign to use query syntax or a small record instead.
- **`Zip` for a `Task` or a `ValueTask` of a maybe.**
  Every argument could be plain, a `Task` or a `ValueTask`, so a complete family has 3 to the power of the number of maybes overloads per form,
  and a partial one leaves holes. Await the tasks first and zip the maybes ([Async](async.md#zip-and-tasks)).
- **`Apply` and other applicative machinery.**
  `Zip` combines independent maybes, and query syntax chains dependent ones; a third style is a vocabulary cost without a use case here.
- **`ToEither`, `ToValidation` and conversions to other libraries' types.**
  An adapter package, if anyone needs it; `ToResult` is the one conversion, because `Result` is part of the same family.
- **Our own union or discriminated-union types.** `Match` and `Switch` are the way to handle both cases on every target.
  Where the *language* has unions (C# 15, `net11.0`), `Maybe<T>` opts in so that a `switch` on the value and `null` is exhaustive;
  that is the compiler's feature, not a type of ours ([C# 15 unions](unions.md)).
- **`Fold`, `Iter` and other collection-style members on a maybe.** `Match`, `Switch` and `ToEnumerable()` cover them.
- **`Maybe.Try` (turning an exception into none).** It would throw the cause away; `Result.Try` keeps it,
  and `ToMaybe()` in the umbrella package drops the error on purpose when only the absence matters. See [Design decisions](design-decisions.md).
- **JSON, EF Core and ASP.NET Core integrations, serialization attributes.** Adapter packages.
- **Logging and telemetry.** The library does not log.

## How to propose a change

Say which of the five questions the change answers and how, and give the name and an example call.
If it adds a railway operation, list the whole family it brings (receivers, delegate kinds and result forms) and the number of methods.
