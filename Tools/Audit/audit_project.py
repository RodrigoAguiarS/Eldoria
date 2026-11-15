"""Read-only repository inventory. No third-party modules or Unity required.

Run from any directory: python Tools/Audit/audit_project.py --output Reports/ProjectAudit/inventory.json
An existing output is refused so historical evidence is never silently overwritten.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import subprocess
from urllib.parse import unquote


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args]).decode("utf-8")


def inventory(root, planned_output=None):
    names = git(root, "ls-files", "--cached", "--others", "--exclude-standard", "-z")
    paths = sorted({Path(n) for n in names.split("\0") if n and (root / n).is_file()
                    and (root / n).resolve() != planned_output})
    hashes = defaultdict(list)
    files, documents, missing_links = [], [], []
    for path in paths:
        data = (root / path).read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        entry = {"path": path.as_posix(), "bytes": len(data), "sha256": digest}
        files.append(entry)
        if data and path.suffix != ".meta":
            hashes[digest].append(path.as_posix())
        if path.suffix == ".md":
            content = data.decode("utf-8-sig")
            documents.append({**entry, "lines": len(content.splitlines()),
                              "headings": re.findall(r"^#{1,6} (.+)$", content, re.M)})
            # Inline Markdown links only, including those in historical instructions.
            for match in re.finditer(r"!?\[[^\]\n]*\]\((<[^>]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\)", content):
                target = unquote(match.group(1).strip("<>"))
                if re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target) or target.startswith("#"):
                    continue
                destination = (root / path.parent / target.split("#")[0]).resolve()
                if not destination.exists() and destination != planned_output:
                    missing_links.append({"source": path.as_posix(), "target": target,
                                          "line": content[:match.start()].count("\n") + 1})
    copies = []
    for source_dir, installed_dir in [
        ("Tools/Lemo3DGenerator/unity", "Assets/_Project/Art/Models/Generated/_Scripts"),
        ("Tools/LemoSpriteLab/unity", "Assets/_Project/Art/Sprites/Prototypes/_SpriteLab"),
    ]:
        for source in sorted((root / source_dir).rglob("*")):
            if not source.is_file() or source.suffix not in (".cs", ".asmdef"):
                continue
            target = root / installed_dir / source.relative_to(root / source_dir)
            copies.append({"source": source.relative_to(root).as_posix(),
                           "installed": target.relative_to(root).as_posix(),
                           "identical": target.is_file() and source.read_bytes() == target.read_bytes()})
    return {"schema": 1, "head": git(root, "rev-parse", "HEAD").strip(),
            "branch": git(root, "branch", "--show-current").strip(),
            "git_status": git(root, "status", "--short"),
            "file_count": len(files), "markdown_count": len(documents),
            "markdown_bytes": sum(d["bytes"] for d in documents),
            "extensions": dict(sorted(Counter(p.suffix.lower() for p in paths).items())),
            "files": files, "documents": documents,
            "duplicate_groups": [v for v in hashes.values() if len(v) > 1],
            "installed_copies": copies, "missing_inline_link_paths": missing_links,
            "limits": ["Hashes prove identical bytes, not redundancy or runtime non-use.",
                       "Links: inline path existence only; anchors, bare paths and external URLs are not validated.",
                       "Ignored caches and local assistant settings are excluded; this is not AssetDatabase dependency analysis.",
                       "The planned output is excluded from its own inventory/hash and may be linked by the audit document."]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    output = args.output.resolve()
    if output.exists():
        raise FileExistsError(f"Preserve existing evidence; choose another output: {output}")
    report = inventory(root, output)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
        stream.write("\n")
    print(json.dumps({k: report[k] for k in ("file_count", "markdown_count", "markdown_bytes")}, ensure_ascii=True))
    print(f"Duplicate groups: {len(report['duplicate_groups'])}; divergent/missing installed copies: "
          f"{sum(not c['identical'] for c in report['installed_copies'])}; "
          f"missing inline link paths: {len(report['missing_inline_link_paths'])}")


if __name__ == "__main__":
    main()
