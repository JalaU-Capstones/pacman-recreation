"""
generate-nuget-sources.py

Generates generated-sources.json for offline NuGet restore inside Flatpak builds.
Includes both app packages AND .NET 9 runtime packs required for --self-contained builds.

Usage:
    cd /path/to/flathub-repo
    python3 tools/generate-nuget-sources.py

Requirements:
    pip install aiohttp

Output:
    generated-sources.json
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

# .NET 9 runtime packs required for --self-contained linux-x64 publish.
# These are NOT in packages.lock.json because dotnet resolves them at publish time.
# Version must match the .NET SDK version used in the Flatpak build.
# Check with: dotnet --version  (in the build environment)
DOTNET_VERSION = "9.0.3"  # Update if SDK extension uses a different patch version

RUNTIME_PACKS = [
    f"microsoft.netcore.app.runtime.linux-x64",
    f"microsoft.aspnetcore.app.runtime.linux-x64",
    f"microsoft.netcore.app.host.linux-x64",
]

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


async def fetch_package(session, name, version, semaphore):
    """Download a NuGet package and return its source entry."""
    url = f"{NUGET_BASE_URL}/{name.lower()}/{version.lower()}/{name.lower()}.{version.lower()}.nupkg"
    async with semaphore:
        try:
            async with session.get(url, timeout=aiohttp.ClientTimeout(total=120)) as resp:
                if resp.status == 404:
                    print(f"  NOT FOUND: {name} {version}")
                    return None
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
        sys.exit(1)

    print(f"Project: {csproj}")

    # Step 1: Get app packages
    lock_file = csproj.parent / "packages.lock.json"
    if lock_file.exists():
        app_packages = get_packages_from_lock_file(lock_file)
    else:
        print("\nNo packages.lock.json found. Add this to PacmanGame.csproj:")
        print("  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>")
        print("Then run: dotnet restore src/PacmanGame/PacmanGame.csproj\n")
        app_packages = get_packages_via_restore(csproj)

    # Step 2: Add .NET runtime packs (required for --self-contained linux-x64)
    runtime_packages = [(name, DOTNET_VERSION) for name in RUNTIME_PACKS]

    all_packages = app_packages + runtime_packages

    # Deduplicate
    seen = set()
    unique = []
    for name, version in all_packages:
        key = f"{name.lower()}.{version.lower()}"
        if key not in seen:
            seen.add(key)
            unique.append((name, version))

    print(f"\nApp packages:     {len(app_packages)}")
    print(f"Runtime packs:    {len(runtime_packages)}")
    print(f"Total unique:     {len(unique)}")
    print(f"\nDownloading and hashing (this takes a few minutes)...")

    semaphore = asyncio.Semaphore(8)
    connector = aiohttp.TCPConnector(limit=16)

    async with aiohttp.ClientSession(connector=connector) as session:
        tasks = [fetch_package(session, name, version, semaphore) for name, version in unique]
        results = await asyncio.gather(*tasks)

    # Separate found vs not-found
    sources = sorted(
        [r for r in results if r is not None],
        key=lambda x: x["dest-filename"]
    )
    missing = [unique[i] for i, r in enumerate(results) if r is None]

    output_path = SCRIPT_DIR / "generated-sources.json"
    with open(output_path, "w") as f:
        json.dump(sources, f, indent=2)
        f.write("\n")

    print(f"\nWrote {len(sources)} entries to: {output_path}")

    if missing:
        print(f"\nWARNING: {len(missing)} packages not found on NuGet:")
        for name, version in missing:
            print(f"  {name} {version}")
        print("\nIf these are runtime packs, update DOTNET_VERSION in this script.")
        print("Check the actual .NET version in the SDK extension:")
        print("  flatpak run --command=dotnet org.freedesktop.Sdk//24.08 --version")
    else:
        print("All packages found and hashed.")

    print()
    print("Next step:")
    print("  flatpak-builder --force-clean --user --install build-dir manifest.yaml")


if __name__ == "__main__":
    asyncio.run(main())
