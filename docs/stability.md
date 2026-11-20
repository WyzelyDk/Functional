# Stability and support

What you can rely on when you take a version of these packages, what can change in a minor release, and what only changes in a major one.
It applies from **1.0.0**. Until then the packages are 0.x, and any release can change the API (the notes of each release on GitHub list what changed).

## Versions

- **Semantic versioning.** A version is `major.minor.patch`. A patch fixes a bug, a minor adds something, a major may break something.
- **One version for all packages.** `Wyzely.Functional.Result`, `Wyzely.Functional.Maybe` and `Wyzely.Functional` are released together by one tag
  ([how](releasing.md#tags)), so they always carry the same version, and you can move all three at once.
- **A version with a hyphen** (`1.2.0-preview.1`) is a prerelease. It makes no promise about the next release.
- **Published versions never change.** nuget.org versions are immutable. A bad release is fixed by the next version, and the bad one is unlisted.

## What is the public API

The public API is what a caller of the package can see: the public types and members, the names of their parameters and the default values of those,
the nullable annotations, the generic constraints, and the attributes that change overload resolution or nullable flow.
It is not described in prose: it is the text in `test/ApiSurface/Approved`, one file per package and target framework, and a test fails when the API
differs from it ([how](releasing.md#the-public-api-and-package-validation)). A change of the API is therefore a visible change in a pull request,
and a rename of a parameter counts, because a caller may pass it by name.

These are **not** part of the public API:

- **The seven JSON guard classes** in the namespace `Wyzely.Functional.Internal` (`ResultJsonGuard`, `ResultOfTJsonGuard`, `ErrorJsonGuard`,
  `ErrorDetailJsonGuard`, `ErrorFrameJsonGuard`, `ErrorKindJsonGuard` and `MaybeJsonGuard`). They are public only so that a source-generated
  `System.Text.Json` context can reach them. They are hidden from IntelliSense (`[EditorBrowsable(Never)]`), and you should not use them:
  they can change or go away in any release.
- **Anything in a namespace that contains `Internal`.** That is the rule: a public type that is only public for a tool goes there, and needs no new sentence.
- **The generated code and its layout.** The operations are part of the API, the files that contain them are not.

## What changes in which release

| Change | Patch | Minor | Major |
|---|---|---|---|
| A bug fix that makes the library do what its documentation says | yes | yes | yes |
| A new public type or member (the admission test in the `scope.md` pages still applies) | | yes | yes |
| A new analyzer rule, reported as a suggestion (info) | | yes | yes |
| A new analyzer rule that is a warning | | | yes |
| A new target framework | | yes | yes |
| A target framework dropped because Microsoft ended support for that .NET | | | yes |
| A public member removed, renamed or given a different signature | | | yes |
| A different default for a setting in `ResultOptions` or `MaybeOptions` | | | yes |
| The compiler version that the analyzers need raised, when an SDK that can build a supported target loses them | | | yes |
| The compiler version that the analyzers need raised, when every such SDK already has the newer compiler | | yes | yes |

### Behavior that is not an API change

The exact text of an exception or an error message is not part of the API, and neither is the order of the entries in a `ToString()` that is not documented.
What the documentation says about behavior is: a failed result stays failed, `default` is a success, the success path allocates nothing
(`PerformanceTests`), and `Combine` reports every error in order.
A change that turns something that silently worked into an exception is a breaking change too, even though no signature changes:
the JSON guard is the example (code that serialized a `Result` and got wrong data now throws), and such a change is made in a major release only.

## Which .NET

The packages are for **modern .NET** only (not .NET Framework, not .NET Standard), and they target **every modern .NET that Microsoft supports, both the
long-term-support (LTS) and the standard-term-support (STS) releases**.

- **Today:** `net10.0` and `net11.0`. The `net11.0` asset adds C# 15 union support and nothing else: the public API of the two assets is the same apart
  from the union declaration of `Result<T>` and `Maybe<T>` (the approved API files of the two target frameworks differ in that only).
- **A new .NET** is added as a target in a minor release after it is released.
- **A .NET that Microsoft no longer supports** is dropped soon after, in a new major release, which also adds the .NET versions that have been released since.
  A project that stays on it stays on the last version that has its asset. The last version with a `netstandard2.0` asset (.NET Framework, Unity, and
  .NET 8 or 9 through it) is 0.3.x. The end-of-support dates are Microsoft's: see Microsoft's .NET support policy for them, they are not repeated here.
- **The current long-term-support .NET always works**, built with every SDK that can build a project for it (for .NET 10, from 10.0.100).
  `Test-LtsConsumer.ps1` builds a project against the packed packages with every installed .NET 10 SDK on every CI build and before every release,
  and checks that the analyzers load and the library runs.

Because a major release follows the end of support of a .NET, how often there is one is decided by Microsoft's schedule and not by this repository.

## The analyzers

- **IDs are permanent.** A rule that has shipped keeps its ID, and the ID is never renamed or given to another rule, because people write IDs into
  `#pragma warning disable`, `[SuppressMessage]` and `.editorconfig` ([why](analyzers.md#why-the-id-has-to-stay-put)). A rule that is removed keeps its number.
- **Severity.** The rules that report a mistake are warnings (`WYZ0101`, `WYZ0102`, `WYZ0111`, `WYZ0201`); the suggestions are info. A rule that exists
  does not become more severe in a minor release. You can set the severity of any rule in `.editorconfig` ([how](result/safety-and-analyzers.md)).
- **The compiler that loads them.** The analyzers are built against Roslyn 5.0, the compiler of the .NET 10 SDK (from 10.0.100), and packed into
  `analyzers/dotnet/roslyn5.0/cs`; an older compiler is given nothing and says nothing ([why](analyzers.md#which-compiler-can-load-them)).
  The property is `AnalyzerRoslynVersion`, it is raised only on purpose, and a raise is always announced in the release notes.

## Deprecation

A public member that is going to be removed is first marked `[Obsolete]`, with a message that says what to use instead, and it stays for **at least one
minor release**; it is removed only in a major release. A member that is marked obsolete does not change behavior.
This is about members. A target framework that is dropped because Microsoft ended support for that .NET is not announced this way ([Which .NET](#which-net)).

## Fixes

A bug fix, and a security fix, goes into the **newest version** only. An older version is not patched; update to the newest one, which is what a minor
release is designed to make safe.

## Where a change is announced

Every release has GitHub release notes that list the pull requests since the previous release. A change that this page puts in a major release, or a
change of behavior without a change of signature, has a heading of its own in the notes.
