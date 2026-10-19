# Wyzely.Functional

**Small, sharp .NET libraries for functional building blocks in business code: independent, explicit, and honest about their trade-offs.**

This is a set of .NET packages for two things that business code keeps needing: an expected failure that is a value instead of an
exception (`Result`), and an absent value that is not `null` (`Maybe`). You take the pieces you need and nothing else: every package
stands on its own, has no dependencies it does not need, and says in plain words what it will and will not do.

The [README on GitHub](../README.md) is the best place to start: what "functional" means here, the opinions behind the packages,
[where this code comes from](../README.md#where-this-code-comes-from), and which package to install.

## Where to go next

| Package | What it is for | Start here |
|---|---|---|
| `Wyzely.Functional.Result` | An expected failure is a value: `Result` and `Result<T>` | [Getting started](result/getting-started.md) |
| `Wyzely.Functional.Maybe` | An absent value is a value: `Maybe<T>` | [Getting started](maybe/getting-started.md) |
| `Wyzely.Functional` | Both, plus `ToResult` and `ToMaybe` between them | [Getting started](functional/getting-started.md) |

Every analyzer rule has its own page; the IDs are explained in [Analyzer rule IDs](analyzers.md). The
[coverage report](https://wyzelydk.github.io/Functional/coverage/) shows the test coverage of `main`.
