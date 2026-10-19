# Contributing to Wyzely.Functional

Thank you for taking the time to help. Bug reports, questions, documentation fixes and code are all welcome, and this page is the
shortest route to a contribution that gets merged.

By taking part you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Ways to contribute

- **Report a bug.** Open an issue with the package and version, the smallest code that shows the problem, what you expected, and what
  happened instead. A failing test is the best bug report there is.
- **Ask a question or suggest an idea.** Open an issue first, before writing code. These packages have a written scope and decline most
  additions on purpose, so it is cheaper for everyone to agree on the idea before there is a pull request.
- **Improve the documentation.** Fixes to wording, examples and links need no prior discussion; open a pull request.
- **Fix a bug or add a feature.** Follow the steps below.

Security problems should not be reported in a public issue. Use the repository's **Security** tab (private vulnerability reporting) so
that a fix can be released before the details are public.

## Before you propose a change

Read the scope of the package you have in mind ([Result](docs/result/scope.md)). Every new public member has to pass its admission
test, because a public API can never be taken back. In the issue, say which question of the scope your change answers and show the few lines
someone has to write today without it.

## Getting started

You need the [.NET 11 SDK](https://dotnet.microsoft.com/download) (a release candidate for now; `global.json` pins the exact version) and the .NET 8 and 10 runtimes, which the tests of those target frameworks run on.

```shell
git clone https://github.com/WyzelyDk/Functional.git
cd Functional
dotnet build -c Release
dotnet test -c Release
```

[Building and contributing](docs/result/contributing.md) has the details: the project layout, the generated async variants and how to
regenerate them, and the benchmarks.

## Making a change

1. **Fork** the repository and create a branch from `main` with a short, descriptive name (`fix-ensure-null-error`, `docs-async-page`).
2. **Write the failing test first.** A change of behavior starts with a test that fails at run time, then the code that makes it pass.
3. **Keep the change small and focused.** One concern per pull request; unrelated clean-ups go in their own.
4. **Follow the existing style.** `.editorconfig` and the analyzers are enforced: Release builds treat warnings as errors, and every
   public and internal member has XML documentation.
5. **Do not edit generated files by hand.** Change the catalog in `src/Result.CodeGeneration` and regenerate; a test fails if the generated
   files are out of date.
6. **Measure before you claim speed.** A performance change comes with a benchmark, and a measurement that contradicts the expectation wins.
7. **Update the documentation.** README examples are tests, so change the test that holds a snippet when you change the snippet. The
   runnable projects in `samples/` (Result, Maybe and Functional) are tests too (`test/Samples.Tests`): if you change behavior that a sample
   prints, update the sample and its test.

## Pull requests

- Fill in the pull request template and link the issue it closes.
- The build, the tests on every target framework, the Markdown link check and the documentation site build must pass. How CI decides what to run is explained in
  [CI and releasing](docs/releasing.md).
- Write commit messages in the imperative, with a short first line that says what changed (`Fix Ensure when the error is null`) and, when
  the reason is not obvious, a paragraph that says why.
- A maintainer reviews every pull request. Expect questions, and feel free to push back; a change that is declined is usually declined
  because of the scope, not because of the quality of the work.
- Pull requests are squash- or rebase-merged, never merged with a merge commit, so `main` stays linear.

## License

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE) that covers the project.
