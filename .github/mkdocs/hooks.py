"""MkDocs hook: makes links from a documentation page to a file outside docs/ work on the site.

The pages under docs/ are written for GitHub, where a relative link such as `../../src/Maybe/README.md` or
`../../samples/Functional.Samples/CheckoutExample.cs` opens the file in the repository. The site only contains docs/, so such a link
would be broken there. This hook rewrites every relative link that leaves docs/ to the same file on GitHub (`<repo_url>/blob/main/...`,
or `/tree/main/...` for a folder) and keeps the anchor. Links inside docs/, absolute links and links inside fenced code blocks are left
alone. The source files are not changed, so the links keep working on GitHub, and `.github/scripts/Test-MarkdownLinks.ps1` still checks
that their targets exist.
"""

from __future__ import annotations

import os
import posixpath
import re

BRANCH = "main"

_FENCE = re.compile(r"^\s*(```|~~~)")
_LINK = re.compile(r"(\]\()([^)\s]+)((?:\s+\"[^\"]*\")?\))")
_ABSOLUTE = re.compile(r"^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|#|/)")


def _rewrite(target: str, page_dir: str, docs_dir: str, repo_root: str, repo_url: str) -> str:
    if _ABSOLUTE.match(target):
        return target

    path, hash_sign, anchor = target.partition("#")

    if not path:
        return target

    # The target relative to the repository root, for example "src/Maybe/README.md".
    in_repo = posixpath.normpath(posixpath.join(docs_dir, page_dir, path))

    if in_repo == docs_dir or in_repo.startswith(docs_dir + "/") or in_repo.startswith(".."):
        return target

    kind = "tree" if os.path.isdir(os.path.join(repo_root, in_repo)) else "blob"

    return f"{repo_url.rstrip('/')}/{kind}/{BRANCH}/{in_repo}{hash_sign}{anchor}"


def on_page_markdown(markdown, page, config, **_):
    repo_root = os.path.dirname(os.path.abspath(config.config_file_path))
    docs_dir = posixpath.normpath(os.path.relpath(config.docs_dir, repo_root).replace(os.sep, "/"))
    page_dir = posixpath.dirname(page.file.src_uri)
    repo_url = config.repo_url

    lines = markdown.split("\n")
    in_fence = False

    for index, line in enumerate(lines):
        if _FENCE.match(line):
            in_fence = not in_fence

            continue

        if in_fence:
            continue

        lines[index] = _LINK.sub(
            lambda match: match.group(1)
            + _rewrite(match.group(2), page_dir, docs_dir, repo_root, repo_url)
            + match.group(3),
            line,
        )

    return "\n".join(lines)
