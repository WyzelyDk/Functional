# Design decisions

[Back to the Maybe overview](../../src/Maybe/README.md)

Each decision below says what was chosen, why, and what it costs. They are the maintainers' decisions; where the reason is a measurement or a
test, the page says which.

## A struct with a flag, and `default` is none

`Maybe<T>` is a `readonly struct` that holds the value and a `bool`. A class would allocate for every `Some`, and a nullable reference cannot
mark "no value" for a value type. With a flag, creating, passing and chaining maybes allocates nothing, which the
[performance tests](performance.md) check.

The `default` value of a struct is all zeros, so the flag's zero must mean something safe: here it means "no value". That is why there is
**no uninitialized state** to guard against: a field or array slot that was never assigned is a valid none, and nothing needs an
analyzer. The price is a flag next to the value: `Maybe<int>` is 8 bytes, where an `int` is 4.

## `where T : notnull`

A maybe means "a value, or no value". If `T` could itself be `string?`, a `Maybe<string?>` would have two ways of being empty (none, and a
some that holds `null`), and every reader would have to ask which one they got. The constraint keeps one way. The compiler warns when the
constraint is violated, so it works through nullable annotations rather than at run time.

## An implicit conversion where `null` means none

A `T` converts to `Maybe<T>` implicitly, and a `null` reference becomes none. This is a deliberate exception to the rule that
[`Result`](../result/design-decisions.md) has no implicit conversions, and the reasoning is the opposite of the one for `Result`:

- For `Result<bool>`, `return false;` would compile as a success holding `false`, which reads like a failure. The conversion changes the meaning
  of what the reader sees.
- For `Maybe<User>`, `return user;` means "there is this user", and a `null` `user` means "there is no user". That is what `null` already
  says, so the conversion adds nothing a reader can misunderstand, and it lets existing methods that return `User?` move to `Maybe<User>`
  without touching every `return`.

The cost: `Maybe<User> found = repository.Find(id);` silently becomes none when `Find` returns `null`, and that is not visible at the call site. If you want
every conversion to be spelled out, use `Maybe.Some` and `Maybe.From`; the implicit conversion is a convenience, not the only way.

## `Map` turns a `null` result into none

`Map(f)` returns a `Maybe<TResult>`, and a `Maybe` cannot hold `null`. So when `f` returns `null`, the result is none instead of an exception or
a "some" that holds `null`. The delegate is a `Func<T, TResult?>`, which has two effects, both pinned by `ConsumerNullabilityTests`: a lambda that
returns a **nullable reference** (`person => person.Nickname`) compiles without a warning, and `TResult` is inferred as the non-nullable type, so the
result is a `Maybe<string>` and never a `Maybe<string?>`. The same signature is used by the query-syntax `select` and by the `Zip` selectors.

A nullable **value** type behaves the same way, through an overload. With only the signature above, a lambda that returns an `int?` would make the
compiler infer `TResult` as `int?`, which violates the `notnull` constraint (`CS8714`) and would give a `Maybe<int?>`: a nullable reference and a nullable
struct would behave differently. So `Map` (all nine variants), `Select`, the `SelectMany` result selector and the `Zip` selectors have a second overload
constrained to `where TResult : struct` whose delegate returns `TResult?` (that is, `Nullable<TResult>`). C# prefers it for a lambda that returns `int?`,
because `Nullable<TResult>` is more specific than `TResult`, and it never applies to a lambda that returns an `int` or a string, so no existing call
changes. The result is a `Maybe<int>` and a `null` becomes none, exactly as for a nullable reference. Both are pinned by `ConsumerNullabilityTests` and
by generated tests.

What it costs: a delegate that returns `null` by accident is not reported at run time (the chain just ends in none), and `Map` has twice as many
overloads. One limit remains: the implicit *conversion* `Maybe<int> m = nullableInt;` does not compile, because a conversion operator cannot be declared
for `Nullable<T>` without constraining the whole type to structs. Use `Maybe.From(nullableInt)`; a nullable reference converts implicitly.

## Rejected: an implicit conversion from `Maybe<Maybe<T>>` to `Maybe<T>`

We considered an implicit conversion that flattens a `Maybe<Maybe<T>>` into a `Maybe<T>`, and **did not add it**. The reasons:

- **It would not help chains.** A user-defined conversion does not apply to the receiver of an extension method, so
  `nested.Map(...)` would still be a `Maybe<Maybe<T>>` receiver. What a chain needs is already there: `Bind` flattens as it goes, and
  `Flatten()` (plain, `Task` and `ValueTask` receivers) removes a level that arrived from elsewhere.
- **It hides a real modelling question.** A `Maybe<Maybe<T>>` usually means that a `Map` delegate returned a maybe and `Bind` was meant. A conversion
  would make the compiler accept the mistake silently; with `Flatten()` the flattening is visible at the place where it happens.
- **A generic type whose operator mentions a deeper instantiation of itself risks generic-expansion cycles for ahead-of-time compilers.** We
  did **not** verify this concern (we did not build an AOT or trimmed application with such a conversion), so treat it as a possible risk
  that supported the decision, not as a measured fact. The first two reasons are sufficient without it.

The conversion that *does* exist is the one from `T?` to `Maybe<T>` (see above); nothing converts out of a maybe.

## `Some(null)` throws; `From(x)` does not

`Maybe.Some(value)` says "there is a value", so a `null` is an argument error (`ArgumentNullException`, with a message that points to `From`
and `None`). `Maybe.From(value)` says "this may be `null`" and converts `null` to none. Two names for two intentions keep a `null` that
slipped in from becoming a silent none.

## `GetValue()` is a method, guarded by an analyzer

Not a property that throws, for the same reason as on `Result` (see its [design decisions](../result/design-decisions.md)): a method
reads as an operation, and the analyzer [`WYZ0201`](analyzers/WYZ0201.md) reports a call that is not guarded by a check. Prefer `TryGetValue`, `ValueOr`
and `Match`, which need no check.

## A single `IReadOnlyDictionary` overload for `GetValueOrNone`

`Dictionary<TKey, TValue>` implements both `IDictionary<TKey, TValue>` and `IReadOnlyDictionary<TKey, TValue>`. With one overload for each, a call
on a plain `Dictionary` would be ambiguous, and the most common type is the one that must work. The library offers the read-only interface only, which
a `Dictionary` satisfies without a cast. A variable that is typed only as `IDictionary<TKey, TValue>` does not match and uses `TryGetValue` (or a wrapper).

## No `Maybe.Try`

`Result.Try` turns an exception into a failure and keeps the exception as the cause. A `Maybe.Try` would turn it into "no value" and
throw the cause away, which is rarely what you want to do with an exception you did not expect. When only the absence matters, the umbrella package makes
the loss explicit: `Result.Try(...).ToMaybe()` drops the error, and the name says so ([Interop](../functional/interop.md)).

## C# 15 union support, where the compiler has it

On `net11.0` a `Maybe<T>` is a union of `T` and the absence of a value (the `null` pattern), so a `switch` over it can be checked for
completeness. It uses the *union member provider* form (a nested `IUnionMembers` interface), because maybes are created through factory methods and not
through public constructors. The cost is that it exists only for a build with the
.NET 11 SDK, and that the language feature was still in preview when it was written. [Details](unions.md).

## Shared internals, per-package behavior

Guards, the hash helper, the nullable-attribute polyfills and the options holder are shared with `Result` and the umbrella package as **internal source files**
that are compiled into each library (`src/Shared/Internal`), so a consumer cannot reach them and the packages stay independent. The generator engine
that writes the railway and `Zip` overloads is shared the same way.

## Single-word namespaces for the tooling

The public type is `Wyzely.Functional.Maybe` (the static class that holds the factories), in the namespace `Wyzely.Functional`. A *namespace*
beginning with `Wyzely.Functional.Maybe.` would clash with that type, because the compiler would not know whether `Maybe.Something` means the type's
member or a namespace's type. So everything that is not the library itself uses a single word after `Functional`:
`Wyzely.Functional.MaybeAnalyzers`, `Wyzely.Functional.MaybeCodeGeneration` and `Wyzely.Functional.MaybeTests`. The *package* ID is still
`Wyzely.Functional.Maybe`, which is a package name and not a namespace.

## One cached completed task for none

`Task.FromResult(default(Maybe<string>))` allocates, because a `Maybe` of a reference type holds a reference; the runtime already caches the completed task of
`default` for value types without references, so `Maybe<int>` never needed this. The generated fast paths therefore hand out one cached completed task
per type for none (`MaybeTasks<T>.None`, internal), and a `Task` chain over a reference type that ends in none allocates nothing. Tests in `PerformanceTests`
check it; the cost is one static field per instantiated type.

## Functional names, no `Async` suffix

`Map`, `Bind`, `Tap`, ... for every receiver and delegate kind, as on `Result`. The price is the async-lambda note in [Async](async.md).
Eleven operations in nine variants are 99 generated methods, the nullable-value form of `Map` adds 9 and `Contains`, which has no delegate and so three variants, adds 3 (111); `Zip` adds 12 (a tuple form, a selector form and a nullable-value selector form for two to five maybes).

## Configurable defaults, one global switch

Anything that is a matter of taste or environment (how a maybe is shown, the exception message, whether the value may appear in logs, the
`ConfigureAwait` behavior) is a [setting](configuration.md) with a default. The settings are process-wide on purpose.
