"""Baut für jedes Package unter Packages/ ein eigenes Release-Zip
und trägt neue Versionen in die gemeinsame index.json (VPM-Listing) ein.

Pro Package und Version entsteht genau ein GitHub-Release mit dem Tag
"<package-name>-<version>". Existiert das Release schon, wird es übersprungen.
"""
import hashlib
import json
import os
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACKAGES_DIR = ROOT / "Packages"
INDEX_PATH = ROOT / "index.json"
OUT_DIR = ROOT / "_release"
REPO = os.environ["REPO"]


def release_exists(tag: str) -> bool:
    result = subprocess.run(
        ["gh", "release", "view", tag, "--repo", REPO],
        capture_output=True,
    )
    return result.returncode == 0


def build_zip(package_dir: Path, zip_path: Path) -> str:
    zip_path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for file in sorted(package_dir.rglob("*")):
            if file.is_file():
                zf.write(file, file.relative_to(package_dir).as_posix())
    return hashlib.sha256(zip_path.read_bytes()).hexdigest()


def main() -> None:
    index = json.loads(INDEX_PATH.read_text(encoding="utf-8"))
    index.setdefault("packages", {})

    for manifest_path in sorted(PACKAGES_DIR.glob("*/package.json")):
        package_dir = manifest_path.parent
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        name = manifest["name"]
        version = manifest["version"]
        tag = f"{name}-{version}"
        versions = index["packages"].setdefault(name, {"versions": {}})["versions"]

        if release_exists(tag):
            print(f"{tag}: Release existiert schon, übersprungen")
            continue

        zip_name = f"{name}-{version}.zip"
        zip_path = OUT_DIR / zip_name
        sha256 = build_zip(package_dir, zip_path)

        subprocess.run(
            [
                "gh", "release", "create", tag, str(zip_path),
                "--repo", REPO,
                "--title", f"{manifest.get('displayName', name)} {version}",
                "--notes", f"{manifest.get('displayName', name)} {version}",
            ],
            check=True,
        )

        entry = dict(manifest)
        entry["url"] = f"https://github.com/{REPO}/releases/download/{tag}/{zip_name}"
        entry["zipSHA256"] = sha256
        versions[version] = entry
        print(f"{tag}: Release erstellt und in index.json eingetragen")

    INDEX_PATH.write_text(
        json.dumps(index, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )


if __name__ == "__main__":
    main()
