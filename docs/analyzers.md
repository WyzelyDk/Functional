# Analyzer rule IDs

Every package in this repository that ships analyzers uses the same scheme for the IDs of its diagnostics:

```text
WYZ PPRR
    ││└┴ the rule within the package, 01 to 99
    └┴── the package, 01 to 99
```

`WYZ` identifies Wyzely's analyzers, the first two digits identify the package, and the last two digits identify the rule. The packages that have
analyzers use these ranges:

| Range | Package | Rules |
|---|---|---|
| `WYZ01xx` | `Wyzely.Functional.Result` | [`WYZ0101`](result/analyzers/WYZ0101.md), [`WYZ0102`](result/analyzers/WYZ0102.md), and the suggestions [`WYZ0103`](result/analyzers/WYZ0103.md) to [`WYZ0110`](result/analyzers/WYZ0110.md) |
| `WYZ02xx` | `Wyzely.Functional.Maybe` | [`WYZ0201`](maybe/analyzers/WYZ0201.md) |

A package that has no analyzers uses no range. The next package that ships analyzers takes the next free number, and the table above is
updated in the same pull request.

## Why the ID has to stay put

People write these IDs into their code (`#pragma warning disable`, `[SuppressMessage]`) and into `.editorconfig` files, so a published ID
is part of the public API of the package. An ID is therefore never renamed or reused: a rule that is removed keeps its number, and the
number is not given to another rule. Each rule's page is named after its ID, and the rule links to that page.
