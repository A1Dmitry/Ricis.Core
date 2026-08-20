#!/usr/bin/env python3
"""Verify that the Ricis.Core solution contains every repository-owned C# project.

This guard deliberately treats the .sln file as the solution membership contract.
It checks only source project files and excludes generated build directories.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

PROJECT_PATTERN = re.compile(
    r'^Project\("\{[^}]+\}"\)\s*=\s*"[^"]+",\s*"([^"]+\.csproj)",\s*"\{[^}]+\}"$',
    re.MULTILINE,
)
EXCLUDED_DIRECTORIES = {".git", "bin", "obj"}


def relative_project_paths(root: Path) -> set[str]:
    projects: set[str] = set()
    for project in root.rglob("*.csproj"):
        relative = project.relative_to(root)
        if EXCLUDED_DIRECTORIES.intersection(relative.parts):
            continue
        projects.add(relative.as_posix())
    return projects


def solution_project_paths(solution: Path) -> list[str]:
    return [match.replace("\\", "/") for match in PROJECT_PATTERN.findall(solution.read_text(encoding="utf-8"))]


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    solution = root / "Ricis.Core.sln"
    if not solution.is_file():
        print(f"SOLUTION_PROJECT_VERIFY_FAIL: solution is missing: {solution}")
        return 1

    repository_projects = relative_project_paths(root)
    solution_projects = solution_project_paths(solution)
    solution_project_set = set(solution_projects)

    missing = sorted(repository_projects - solution_project_set)
    nonexistent = sorted(path for path in solution_project_set if not (root / path).is_file())
    duplicates = sorted({path for path in solution_projects if solution_projects.count(path) > 1})

    if missing or nonexistent or duplicates:
        print("SOLUTION_PROJECT_VERIFY_FAIL")
        if missing:
            print("Missing from Ricis.Core.sln:")
            print("\n".join(f"  - {path}" for path in missing))
        if nonexistent:
            print("Solution entries without a project file:")
            print("\n".join(f"  - {path}" for path in nonexistent))
        if duplicates:
            print("Duplicate solution entries:")
            print("\n".join(f"  - {path}" for path in duplicates))
        return 1

    print(f"SOLUTION_PROJECT_VERIFY_PASS: {len(repository_projects)} projects")
    return 0


if __name__ == "__main__":
    sys.exit(main())
