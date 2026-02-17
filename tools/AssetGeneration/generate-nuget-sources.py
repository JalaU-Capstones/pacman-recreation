"""
generate-nuget-sources.py

Generates generated-sources.json for offline NuGet restore inside Flatpak builds.

IMPORTANT: Each entry in the output JSON has "dest": "nuget-sources" so that
flatpak-builder places the .nupkg files in the nuget-sources/ directory,
which dotnet restore reads with --source nuget-sources.

Usage:
    cd /path/to/flathub-repo
    python3 tools/generate-nuget-sources.py

Requirements:
    pip install aiohttp

Output:
    generated-sources.json  (include this directly in your manifest sources)
"""

import asyncio
import hashlib
import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

try:
    import aiohttp
except ImportError:
    print("Error: aiohttp not installed. Run: pip install aiohttp")
    sys.exit(1)

NUGET_DEST = "nuget-sources"
NUGET_BASE_URL = "https://api.nuget.org/v3-flatcontainer"
SCRIPT_DIR = Path(__file__).parent.resolve()

CANDIDATE_CSPROJ = [
    SCRIPT_DIR / "src" / "PacmanGame" / "PacmanGame.csproj",
    SCRIPT_DIR.parent / "src" / "PacmanGame" / "PacmanGame.csproj",
    Path.cwd() / "src" / "PacmanGame" / "PacmanGame.csproj",
    ]


def find_csproj():
    for path in CANDIDATE_CSPROJ:
        if path.exists():
            return path
    return None


def get_packages_from_lock_file(lock_path):
    print(f"Reading lock file: {lock_path}")
    with open(lock_path) as f:
        data = json.load(f)

    packages = {}
    for _runtime, deps in data.get("dependencies", {}).items():
        for name, info in deps.items():
            version = info.get("resolved", "")
            if version:
                packages[name.lower()] = (name, version)

    return list(packages.values())


def get_packages_via_restore(csproj_path):
    print("Running dotnet restore to collect packages...")
    with tempfile.TemporaryDirectory() as tmpdir:
        subprocess.run(
            ["dotnet", "restore", str(csproj_path),
             "--runtime", "linux-x64",
             "--packages", tmpdir,
             "--verbosity", "quiet"],
            check=False,
        )

        packages = {}
        for root, dirs, files in os.walk(tmpdir):
            for filename in files:
                if filename.endswith(".nupkg"):
                    stem = filename[:-6]
                    parts = stem.rsplit(".", 1)
                    if len(parts) == 2 and parts[1][0].isdigit():
                        name_part, version_part = parts
                        key = name_part.lower()
                        if key not in packages:
                            packages[key] = (name_part, version_part)

        return list(packages.values())


async def fetch_sha512(session, name, version, semaphore):
    url = f"{NUGET_BASE_URL}/{name.lower()}/{version.lower()}/{name.lower()}.{version.lower()}.nupkg"
    async with semaphore:
        try:
            async with session.get(url, timeout=aiohttp.ClientTimeout(total=60)) as resp:
                if resp.status != 200:
                    print(f"  SKIP {name} {version} (HTTP {resp.status})")
                    return None
                data = await resp.read()
                sha512 = hashlib.sha512(data).hexdigest()
                return {
                    "type": "file",
                    "url": url,
                    "sha512": sha512,
                    "dest": NUGET_DEST,
                    "dest-filename": f"{name.lower()}.{version.lower()}.nupkg"
                }
        except Exception as e:
            print(f"  ERROR {name} {version}: {e}")
            return None


async def main():
    print("NuGet Sources Generator for Flatpak")
    print("=" * 50)

    csproj = find_csproj()
    if not csproj:
        print("Error: PacmanGame.csproj not found.")
        print("Run this script from the flathub repo or project root.")
        sys.exit(1)

    print(f"Project: {csproj}")

    lock_file = csproj.parent / "packages.lock.json"

    if lock_file.exists():
        packages = get_packages_from_lock_file(lock_file)
    else:
        print("\nNo packages.lock.json found. To create it:")
        print("  1. Add to PacmanGame.csproj:")
        print("     <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>")
        print("  2. Run: dotnet restore src/PacmanGame/PacmanGame.csproj")
        print("  3. Commit packages.lock.json to the repo")
        print("\nFalling back to dotnet restore scan (less reliable)...")
        packages = get_packages_via_restore(csproj)

    if not packages:
        print("Error: No packages found.")
        sys.exit(1)

    print(f"\nFound {len(packages)} unique packages.")
    print("Downloading and computing SHA512 hashes (this may take a few minutes)...")

    semaphore = asyncio.Semaphore(8)
    connector = aiohttp.TCPConnector(limit=16)

    async with aiohttp.ClientSession(connector=connector) as session:
        tasks = [
            fetch_sha512(session, name, version, semaphore)
            for name, version in packages
        ]
        results = await asyncio.gather(*tasks)

    sources = sorted(
        [r for r in results if r is not None],
        key=lambda x: x["dest-filename"]
    )

    output_path = SCRIPT_DIR / "generated-sources.json"
    with open(output_path, "w") as f:
        json.dump(sources, f, indent=2)
        f.write("\n")

    print(f"\nDone. Written {len(sources)} entries to: {output_path}")
    print()

    # Verify output has correct dest
    with open(output_path) as f:
        check = json.load(f)
    wrong_dest = [e for e in check if e.get("dest") != NUGET_DEST]
    if wrong_dest:
        print(f"WARNING: {len(wrong_dest)} entries have wrong dest field!")
    else:
        print(f"All entries have correct dest: {NUGET_DEST!r}")

    print()
    print("Next steps:")
    print("  flatpak install flathub org.freedesktop.Sdk.Extension.dotnet9//24.08")
    print("  flatpak-builder --force-clean --user --install build-dir manifest.yaml")


if __name__ == "__main__":
    asyncio.run(main())
