#!/usr/bin/env python3
"""Translate a validated English semantic LaTeX exemplar into locale-specific external JSON assets."""
import json
import os
import sys
from pathlib import Path
from openai import OpenAI

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Logging" / "Templates" / "navier-stokes-ricis.en-US.exemplar.json"
TARGETS = {
    "de-DE": "German as used in Germany",
    "hi-IN": "Hindi in Devanagari script",
    "ms-MY": "Malay as used in Malaysia",
}
PRESERVE_KEYS = {
    "documentId", "statusKey", "sectionId", "kind", "presentation", "evidenceStatus",
    "claimId", "ruleId", "phase", "status", "language", "number", "includeTableOfContents",
}
PRESERVE_TOKENS = ["RICIS", "SP1", "SP2", "SP3", "SP4", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A10", "Deferred", "KernelChecked", "T0", "T1", "T2", "T3", "E_local", "0_S", "infinity_S", "NS-01", "NS-02", "NS-03", "NS-04", "NS-05", "NS-06", "NS-07"]


def prompt(locale: str, language_name: str, source_json: str) -> str:
    return f"""Translate the JSON document below from English into {language_name} for an academic RICIS LaTeX report.

Return exactly one valid JSON object. You MUST preserve the complete object shape exactly: every key, every array, every array length, every number, every boolean, and every enum-like value. Do not summarize, collapse, omit, reorder, or merge sections, children, proof steps, validation rows, abstracts, conclusion steps, or epilogue steps. Preserve the exact values for these keys: {sorted(PRESERVE_KEYS)}. Preserve mathematical symbols, formulas, identifiers, status tokens, and these tokens exactly: {PRESERVE_TOKENS}. Translate only human-readable narrative and visible academic labels. Set every abstract object language field to '{locale}' only if it is an English-language human-readable source value; otherwise preserve the required-key rule. Do not add Markdown, commentary, fields, or claims. Keep the explicit evidence boundary and Deferred/KernelChecked limitation honest.

SOURCE JSON:
{source_json}"""


def validate_shape(source, translated, path="$"):
    if isinstance(source, dict):
        if not isinstance(translated, dict) or set(source) != set(translated):
            raise ValueError(f"Schema mismatch at {path}")
        for key in source:
            validate_shape(source[key], translated[key], f"{path}.{key}")
    elif isinstance(source, list):
        if not isinstance(translated, list) or len(source) != len(translated):
            raise ValueError(f"Array mismatch at {path}")
        for index, (source_item, translated_item) in enumerate(zip(source, translated)):
            validate_shape(source_item, translated_item, f"{path}[{index}]")
    elif isinstance(source, bool):
        if translated is not source:
            raise ValueError(f"Boolean mismatch at {path}")
    elif isinstance(source, int):
        if translated != source:
            raise ValueError(f"Number mismatch at {path}")


def validate_preserved(source, translated, key=None, path="$"):
    if isinstance(source, dict):
        for current_key in source:
            validate_preserved(source[current_key], translated[current_key], current_key, f"{path}.{current_key}")
    elif key in PRESERVE_KEYS and source != translated:
        raise ValueError(f"Required invariant changed at {path}: {source!r} != {translated!r}")


def main() -> int:
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    client = OpenAI()
    for locale, language_name in TARGETS.items():
        response = client.chat.completions.create(
            model="gpt-5",
            messages=[
                {"role": "system", "content": "You are a precise academic translator. Preserve JSON shape and evidence limitations."},
                {"role": "user", "content": prompt(locale, language_name, json.dumps(source, ensure_ascii=False))},
            ],
            max_completion_tokens=16000,
            response_format={"type": "json_object"},
        )
        content = response.choices[0].message.content
        if not content:
            raise RuntimeError(f"Empty translation response for {locale}")
        translated = json.loads(content)
        validate_shape(source, translated)
        validate_preserved(source, translated)
        target = ROOT / "Logging" / "Templates" / f"navier-stokes-ricis.{locale}.exemplar.json"
        target.write_text(json.dumps(translated, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(target)
    return 0


if __name__ == "__main__":
    sys.exit(main())
