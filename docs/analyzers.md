# Analyzer rule IDs

Every package in this repository that ships analyzers uses the same scheme for the IDs of its diagnostics:

```text
WYZ PPRR
    ││└┴ the rule within the package, 01 to 99
    └┴── the package, 01 to 99
```

`WYZ` identifies Wyzely's analyzers, the first two digits identify the package, and the last two digits identify the rule.
The packages that have analyzers use these ranges:

| Range | Package | Rules |
|---|---|---|
| `WYZ01xx` | `Wyzely.Functional.Result` | [`WYZ0101`](result/analyzers/WYZ0101.md), [`WYZ0102`](result/analyzers/WYZ0102.md), and the suggestions [`WYZ0103`](result/analyzers/WYZ0103.md) to [`WYZ0110`](result/analyzers/WYZ0110.md) and [`WYZ0112`](result/analyzers/WYZ0112.md) to [`WYZ0120`](result/analyzers/WYZ0120.md), and [`WYZ0111`](result/analyzers/WYZ0111.md) (a value interpolated into the message of an error) |
| `WYZ02xx` | `Wyzely.Functional.Maybe` | [`WYZ0201`](maybe/analyzers/WYZ0201.md), and the suggestions [`WYZ0202`](maybe/analyzers/WYZ0202.md) to [`WYZ0208`](maybe/analyzers/WYZ0208.md) |

A package that has no analyzers uses no range.
The next package that ships analyzers takes the next free number, and the table above is updated in the same pull request.

## Which compiler can load them

The analyzers and their code fixes are built against Roslyn 5.0, the compiler of the .NET 10 SDK (from 10.0.100) and of Visual Studio 2026,
so that a project for the long-term-support .NET can use them with every SDK it can be built with.
They are packed into `analyzers/dotnet/roslyn5.0/cs`: the SDK gives them to a compiler of that version or newer, and gives an older compiler nothing.
(A compiler refuses an analyzer that was built against a newer Roslyn than itself, with the warning `CS9057`, which is an error when warnings are errors;
the version folder keeps that from happening.)

The version is a promise: it is the property `AnalyzerRoslynVersion` in `Directory.Packages.props`, raised only on purpose and announced in the release notes.
`.github/scripts/Test-LtsConsumer.ps1` builds a project against the packed packages with every .NET 10 SDK on every CI build and before every release.

## Why the ID has to stay put

People write these IDs into their code (`#pragma warning disable`, `[SuppressMessage]`) and into `.editorconfig` files,
so a published ID is part of the public API of the package. An ID is therefore never renamed or reused: a rule that is removed keeps its number,
and the number is not given to another rule. Each rule's page is named after its ID, and the rule links to that page.
