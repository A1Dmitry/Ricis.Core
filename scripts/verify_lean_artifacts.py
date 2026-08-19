#!/usr/bin/env python3
"""Verify the versioned Lean evidence registry without compiling Lean sources."""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "FormalVerification" / "Lean" / "Artifacts" / "manifest.json"
REQUIRED_FIELDS = {
    "id",
    "status",
    "source",
    "description",
    "origin",
    "testIds",
    "theoremNames",
    "leanToolchain",
    "generatedBy",
    "generatedFrom",
    "forbiddenMarkers",
    "knowledgeSource",
}
ALLOWED_STATUSES = {"KernelChecked", "RegressionChecked", "AuditOnly", "RenderedOnly"}


def fail(message: str) -> None:
    raise SystemExit(f"LEAN_ARTIFACT_VERIFY_FAIL: {message}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--compile", action="store_true", help="compile every registered source with the pinned Lean project")
    arguments = parser.parse_args()

    if not MANIFEST.is_file():
        fail(f"manifest not found: {MANIFEST}")

    try:
        document = json.loads(MANIFEST.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        fail(f"invalid manifest JSON: {error}")

    if document.get("schema") != "ricis-lean-evidence/v1":
        fail("unsupported manifest schema")
    artifacts = document.get("artifacts")
    if not isinstance(artifacts, list) or not artifacts:
        fail("manifest must contain a non-empty artifacts array")

    ids: set[str] = set()
    for artifact in artifacts:
        if not isinstance(artifact, dict):
            fail("each artifact must be an object")
        missing = REQUIRED_FIELDS - artifact.keys()
        if missing:
            fail(f"{artifact.get('id', '<unknown>')} missing fields: {sorted(missing)}")

        artifact_id = artifact["id"]
        if artifact_id in ids:
            fail(f"duplicate artifact id: {artifact_id}")
        ids.add(artifact_id)

        status = artifact["status"]
        if status not in ALLOWED_STATUSES:
            fail(f"{artifact_id} has unsupported status: {status}")

        source_value = artifact["source"]
        source = (ROOT / source_value).resolve()
        if ROOT not in source.parents:
            fail(f"{artifact_id} source escapes repository: {source_value}")
        if not source.is_file():
            fail(f"{artifact_id} source missing: {source_value}")

        content = source.read_text(encoding="utf-8")
        for marker in artifact["forbiddenMarkers"]:
            if marker in content:
                fail(f"{artifact_id} contains forbidden marker: {marker}")

        if status == "KernelChecked" and not artifact["theoremNames"]:
            fail(f"{artifact_id} KernelChecked entry has no theorem names")
        if status == "AuditOnly" and "NOT KERNEL VERIFIED" not in content:
            fail(f"{artifact_id} AuditOnly source lacks explicit boundary")
        if not artifact["testIds"]:
            fail(f"{artifact_id} has no provenance test IDs")
        knowledge_source = artifact["knowledgeSource"]
        if not isinstance(knowledge_source, dict):
            fail(f"{artifact_id} knowledgeSource must be an object")
        if knowledge_source.get("mandatoryForModelStudy") is not True:
            fail(f"{artifact_id} must be marked mandatoryForModelStudy")
        if knowledge_source.get("role") != "mandatory-project-knowledge-source":
            fail(f"{artifact_id} has invalid knowledgeSource role")

    if arguments.compile:
        lean_root = ROOT / "FormalVerification" / "Lean"
        for artifact in artifacts:
            source = (ROOT / artifact["source"]).resolve()
            result = subprocess.run(
                ["lake", "env", "lean", str(source)],
                cwd=lean_root,
                text=True,
                capture_output=True,
                check=False,
            )
            if result.returncode != 0:
                sys.stderr.write(result.stdout)
                sys.stderr.write(result.stderr)
                fail(f"Lean compilation failed for {artifact['id']}")
            print(f"LEAN_COMPILE_PASS: {artifact['id']}")

    print(f"LEAN_ARTIFACT_VERIFY_PASS: {len(artifacts)} artifacts")
    return 0


if __name__ == "__main__":
    sys.exit(main())
