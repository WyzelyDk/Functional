# CI and releasing

For maintainers. It describes how pull requests are checked, how a package gets to nuget.org, how that works with several packages in one
repository, and what to set up once on GitHub and nuget.org. The GitHub organization is `WyzelyDk` and the repository is `Functional`; the solution (`Wyzely.Functional.slnx`), the root namespace and the
package IDs (`Wyzely.Functional`, `Wyzely.Functional.Result` and so on) carry the product name.

## The model in one paragraph

Development is trunk-based: `main` is always releasable, work happens in short-lived branches, and nothing reaches `main` except through
a pull request that **builds without errors or warnings and passes every test**. A package is released by pushing a **tag on `main`**; the
tag names the package and, through [MinVer](https://github.com/adamralph/minver), its version. A workflow builds and tests that commit again,
packs exactly that package, and publishes it to nuget.org without any stored password.

## Pull requests: `.github/workflows/ci.yml`

| Job | Runs | What it proves |
|---|---|---|
| `changes` | always | Whether anything **other than documentation** changed (see below). A push to `main` always counts as code. |
| `docs` | always | Every relative link in the Markdown files points at something that exists, and the documentation site builds with `mkdocs build --strict` (no warning). |
| `validate` | when code changed | Every packable project has its own, well-formed release tag prefix; the change detection passes its own tests; the workflows pass `actionlint`. |
| `build-and-test` (Ubuntu, SDK 11) | when code changed | A clean Release build with `-warnaserror`, then every test project on every target framework it has (net8.0, net10.0, net11.0), and a pack smoke test. |
| `sonarcloud` (Ubuntu) | when code changed | The SonarCloud analysis: the scanner wraps a build and the tests with coverage, and the job fails when the quality gate fails (see [SonarCloud](#sonarcloud)). |
| `aot` (Ubuntu) | when code changed | Publishes all three libraries as a Native AOT app (no trim or AOT warning allowed) and runs the binary. |
| `pages` | after a push to `main` | Deploys the website that `build-and-test` packaged (see [The website](#the-website)). Not part of the gate. |
| `CI` (the gate) | always | Succeeds only if the right jobs did. **This is the one check that branch protection requires.** |

### Documentation-only changes

A pull request that changes only documentation does not need a build, and it does not wait for one: `changes` notices, the build and test
jobs are skipped, and `docs` plus the gate finish in well under a minute. The gate accepts "skipped" for the build jobs **only** when no code
changed; it fails if a build job was skipped (or cancelled) although code did change, or if the detection itself failed.

The push that a merged pull request makes to `main` is the exception: it always builds and tests, because it publishes the website and the
website contains the coverage report. That is one build per merge, and the only one: the website is packaged by `build-and-test` itself.

What counts as documentation is listed in one place, `.github/scripts/Test-CodeChanged.ps1`, and tested by `Test-CodeChanged.Tests.ps1`:

| Counts as documentation (skips the build) | Counts as code (runs everything) |
|---|---|
| any `*.md` file, anything under `docs/`, `LICENSE`, `mkdocs.yml`, `.github/mkdocs/` (the `docs` job builds the site anyway) | **`AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`**: they look like documentation but are build inputs of the analyzer |
| `.github/PULL_REQUEST_TEMPLATE.md`, `ISSUE_TEMPLATE/`, `CODEOWNERS`, `FUNDING.yml` | everything under `.github/workflows/` and `.github/scripts/` (the pipeline itself) |
| `.github/dependabot.yml`, `.github/rulesets/` (GitHub reads them; the build does not) | everything under `src/`, `test/`, `benchmarks/` |
| | `Directory.*.props`, `global.json`, `nuget.config`, `.editorconfig`, `.gitattributes`, `.gitignore`, `*.slnx` |
| | **any file the list does not mention**: when in doubt a file is code, so a new kind of file makes CI run until someone decides it is safe |

If a change mixes documentation and code, it is code. If the commit to compare with is unknown (a first push, a new branch), everything runs.
A push to `main` always runs everything (see above).

Decisions behind the pipeline, so they can be revisited deliberately:

- **One workflow for the whole repository that always starts, and no workflow-level `paths:` or `paths-ignore:` filters.** A workflow
  skipped by a path filter never reports a status, and a required check that never reports blocks the pull request forever. Instead the
  workflow always starts and decides inside; the gate is the only required check, so the set of jobs can change without touching the
  repository rules. The same mechanism extends to packages: when there are several, let `changes` also report which package changed and give
  each package's jobs an `if:`, and the gate rule "skipped is fine when that package did not change" stays the same.
- **SDK 11 pinned in `global.json`.** One SDK builds every target framework, for everyone, for CI and for releases (the packages contain the
  `net11.0` asset). While SDK 11 is a release candidate its exact version is pinned there, so a new candidate cannot break every pull request
  unannounced; bump it on purpose. After general availability, pin the stable 11.0.x.
- **Full clones (`fetch-depth: 0`).** MinVer reads the history and the tags; a shallow clone makes versions wrong or warns.
- **Squash or rebase merges, never merge commits**, so that `main` is linear. Rebase a pull request whose commits are worth keeping as
  written; squash the rest. Dependabot keeps the Actions and NuGet packages
  current, and its pull requests go through the same gate.

## The website

`https://wyzelydk.github.io/Functional/` is one GitHub Pages site with two parts, because a repository has only one:

| Path | Content | Built from |
|---|---|---|
| `/` | the documentation site | `mkdocs.yml` and the pages under `docs/`, with [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/) |
| `/coverage/` | the coverage report of `main`, and the badges the READMEs show | the tests of `build-and-test`, merged by ReportGenerator |

After a push to `main`, `build-and-test` builds the documentation site next to the coverage report it has just made and packages both; the
`pages` job deploys them. Nothing is built twice. On a pull request nothing is published, but the `docs` job builds the site with `--strict`,
so a broken anchor or a page missing from the navigation fails the pull request, not the deployment after the merge.

- **The pages stay plain Markdown for GitHub.** A link from a page to a file outside `docs/` (a package README, a sample) is rewritten to the
  file on GitHub by `.github/mkdocs/hooks.py` when the site is built; the source is not changed. A new page must be added to `nav` in
  `mkdocs.yml`, or the strict build fails.
- **The tools are pinned and locked:** the two packages that we depend on in `.github/mkdocs/requirements.in`, everything that is installed (with hashes) in
  `.github/mkdocs/requirements.txt`. Update them on purpose and regenerate the second file (see the comment in it).
- **One-time setup:** Settings -> Pages -> Source: *GitHub Actions*.

To preview the site locally (Python 3):

```shell
python -m venv .venv
.venv/bin/pip install -r .github/mkdocs/requirements.txt     # Windows: .venv\Scripts\pip
.venv/bin/mkdocs serve                                        # http://127.0.0.1:8000/
```

## Releasing: `.github/workflows/publish.yml`

### Tags

Every package has its own tag prefix, the `MinVerTagPrefix` of its project. Today:

| Package | Tag | Example |
|---|---|---|
| `Wyzely.Functional.Result` | `result-v<version>` | `result-v1.2.3`, `result-v1.3.0-preview.1` |
| `Wyzely.Functional.Maybe` | `maybe-v<version>` | `maybe-v1.2.3` |
| `Wyzely.Functional` (the umbrella) | `functional-v<version>` | `functional-v1.2.3` |

A tag with a hyphen in the version (`-preview.1`) is a prerelease on nuget.org and on GitHub. MinVer ignores tags with other prefixes, so
releasing one package never changes the version of another (this was verified: `maybe-v2.0.0` and `v9.9.9` on the same commit did not
affect the version of the Result package). A commit after the tag `result-v1.2.3` builds as `1.2.4-preview.0.1`.

### How to release

1. Merge the pull requests that belong in the release; wait for `CI` to be green on `main`.
2. Tag **main** and push the tag:

   ```shell
   git switch main
   git pull
   git tag result-v0.1.0-preview.1
   git push origin result-v0.1.0-preview.1
   ```

3. In GitHub, open the *Publish* run and **approve the deployment** to the `nuget` environment.
4. The workflow builds, tests, packs, publishes the package and its symbols, and creates a GitHub release whose notes list the pull
   requests since the **previous release of the same package**. nuget.org takes a few minutes to index the new version.

The workflow refuses to publish when the tag is not a well-formed release tag of exactly one package, when the tagged commit is not on
`main`, when the tests fail, or when the package that MinVer produced does not carry the version in the tag.

### SonarCloud

The `sonarcloud` job is part of the gate, so a pull request cannot be merged while the SonarCloud quality gate is red. The organization is
`wyzelydk` and the project key is `WyzelyDk_Functional`; both are in the `begin` step of the job.

- **One-time setup:** the repository is added to SonarCloud, *Automatic Analysis* is off (it cannot be combined with this CI-based
  analysis, and it cannot import coverage), and the secret `SONAR_TOKEN` exists as a repository secret **and** as a Dependabot secret
  (Dependabot's pull requests do not see repository secrets, so without it they fail the job).
- **How it works:** the scanner (pinned in `dotnet-tools.json`) wraps a Release build and the tests; coverage is the Cobertura files that
  `dotnet test --coverage` writes. `sonar.qualitygate.wait=true` makes the `end` step wait for the result and fail when the gate fails.
- **Forks:** the token is not available to a pull request from a fork, so the analysis is skipped there with a warning. A missing token
  anywhere else fails the job.
- **Coverage exclusions** are set in the `begin` step (`sonar.coverage.exclusions`): the samples, the benchmarks, the generator tools and
  the analyzers, which the tests' coverage does not measure.
- **The build is not `-warnaserror` here:** the scanner changes the build and adds the rules of the SonarCloud profile. The strict build is
  the one in `build-and-test`. The rules that the analyzer package leaves off by default but SonarCloud applies are switched on in
  `.editorconfig`, so a local build already reports them.

### Analyzer rules and their release notes

`src/Result.Analyzers` and `src/Maybe.Analyzers` each have an `AnalyzerReleases.Shipped.md` and an `AnalyzerReleases.Unshipped.md`. They are
build inputs of the release-tracking analyzers: a rule that is in neither file fails the build (`RS2000`).

- The rules of the first release are already in `Shipped`, under the heading `## Release 0.1.0`. That heading is only a label: it has to
  be a version number, and it is not compared with the version of the package. Change it when the first version is decided, and keep the
  headings of later releases in ascending order.
- A rule added after that goes into `Unshipped` (a table under `### New Rules`) while it is unreleased. When its release is made, move the
  table into `Shipped` under a new `## Release <version>` heading and leave `Unshipped` with only its header.
- `Result` and `Maybe` are released by separate tags, so each project moves its own entries.

**Never move or reuse a tag.** nuget.org versions are immutable. If a release is wrong, publish the next version and *unlist* the bad one
on nuget.org (a published version cannot be deleted).

### Several packages

- **One tag, one package.** The tag prefix is the only link between a tag and a package, and it lives in the project file. The script
  `.github/scripts/Resolve-Package.ps1` finds the package by asking MSBuild, so no list has to be kept in sync, and CI fails if two
  packages share a prefix or one prefix is the start of another.
- **To add a package:** give its project a unique `<MinVerTagPrefix>name-v</MinVerTagPrefix>`, keep `IsPackable` on, add a package
  README, and nothing else: the workflows pick it up. The trusted publishing policy (below) covers `Wyzely.Functional*`.
- **Release order matters for dependencies.** A package that references another (the umbrella `Wyzely.Functional` references
  `Result` and `Maybe`) records the referenced package's version *at build time*. If `Result` has commits after its last tag, that version
  is something like `1.2.4-preview.0.5`, which does not exist on nuget.org, and the dependent package would be uninstallable. So: tag
  and publish the referenced packages first, and only build the dependent package from a commit where each package it references is exactly on
  its own tag. When the first such package exists, extend the publish workflow with a check that every dependency in the package's
  nuspec exists on nuget.org before pushing.
- **Independent versions.** Packages are versioned separately; a change to `Result` does not force a release of `Maybe`.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `CI` stays "Expected, waiting for status" on a pull request | The pull request does not contain the workflow files, or the workflow did not start (look at the Actions tab). The ruleset requires the check by its name, `CI`. |
| A pull request waits for `CI` forever | A required check name that does not exist, or a workflow skipped by a path filter. Require only `CI`. |
| *Publish* fails at "The tag must be on main" | The tag was created on a branch. Merge first, then tag `main`. |
| *Publish* fails at "The package must carry the version of the tag" | MinVer saw other commits after the tag or a different tag prefix; tag the commit that was built. |
| *Publish* waits and never runs | The `nuget` environment needs a reviewer to approve, or its tag rule does not allow `*-v*`. |
| NuGet login fails | The trusted publishing policy does not match exactly (owner, repository, `publish.yml`, `nuget`), or `NUGET_USER` is an email address instead of the profile name. |
| The `docs` job fails at "Build the documentation site" | A new page is not in `nav` in `mkdocs.yml`, or a link points at a heading that does not exist; the log names the page. |
| `pages` fails at "Deploy to GitHub Pages" | Pages is not set to *GitHub Actions* (Settings -> Pages), or the `github-pages` environment does not allow `main`. |
| Versions look like `0.0.0-preview.0` | The clone was shallow (use `fetch-depth: 0`) or no tag with that package's prefix is reachable. |
