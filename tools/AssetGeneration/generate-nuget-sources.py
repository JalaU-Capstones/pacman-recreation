"""
generate-nuget-sources.py
Generates generated-sources.json with all NuGet packages pre-downloaded
for offline Flatpak builds.

This is the standard approach used by all .NET apps on Flathub.
Run this script BEFORE building or submitting to Flathub.

Usage:
    cd /path/to/flathub-repo
    python3 tools/generate-nuget-sources.py

Requirements:
    pip install aiohttp

Output:
    generated-sources.json  (add this as a source in your manifest)
"""

import asyncio
import hashlib
import json
import os
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

try:
    import aiohttp
except ImportError:
    print("Error: aiohttp not installed.")
    print("Run: pip install aiohttp")
    sys.exit(1)

NUGET_URL = "https://api.nuget.org/v3-flatcontainer"
NUGET_SOURCE_DIR = "nuget-sources"

# Detect project root relative to this script
SCRIPT_DIR = Path(__file__).parent.resolve()
# This script lives in flathub repo root tools/ or directly in root
PROJECT_CSPROJ = None

# Try to find the csproj - adjust path as needed
CANDIDATE_PATHS = [
    SCRIPT_DIR / "nuget-packages-lock.json",
    SCRIPT_DIR.parent / "src" / "PacmanGame" / "packages.lock.json",
]


async def get_package_sha512(session, name, version):
    """Download package and compute SHA512."""
    url = f"{NUGET_URL}/{name.lower()}/{version.lower()}/{name.lower()}.{version.lower()}.nupkg"
    async with session.get(url) as response:
        if response.status != 200:
            print(f"  Warning: Could not download {name} {version} (HTTP {response.status})")
            return None, None
        data = await response.read()
        sha512 = hashlib.sha512(data).hexdigest()
        return url, sha512


async def process_packages(packages):
    """Download all packages and collect their metadata."""
    sources = []
    connector = aiohttp.TCPConnector(limit=10)
    async with aiohttp.ClientSession(connector=connector) as session:
        tasks = []
        for name, version in packages:
            tasks.append((name, version, get_package_sha512(session, name, version)))

        for name, version, coro in tasks:
            print(f"  Processing {name} {version}...")
            url, sha512 = await coro
            if url and sha512:
                sources.append({
                    "type": "file",
                    "url": url,
                    "sha512": sha512,
                    "dest": NUGET_SOURCE_DIR,
                    "dest-filename": f"{name.lower()}.{version.lower()}.nupkg"
                })

    return sources


def get_packages_from_lock_file(lock_file_path):
    """Parse packages.lock.json to get package list."""
    with open(lock_file_path) as f:
        lock_data = json.load(f)

    packages = []
    dependencies = lock_data.get("dependencies", {})
    for runtime_key, deps in dependencies.items():
        for package_name, package_info in deps.items():
            resolved = package_info.get("resolved", "")
            if resolved:
                packages.append((package_name, resolved))

    return packages


def get_packages_from_dotnet_restore(project_path):
    """Run dotnet restore and collect packages from the NuGet cache."""
    print("Running dotnet restore to collect package list...")
    with tempfile.TemporaryDirectory() as tmpdir:
        result = subprocess.run(
            [
                "dotnet", "restore",
                str(project_path),
                "--runtime", "linux-x64",
                "--packages", tmpdir,
                "--verbosity", "normal"
            ],
            capture_output=True,
            text=True
        )

        packages = []
        for root, dirs, files in os.walk(tmpdir):
            for filename in files:
                if filename.endswith(".nupkg"):
                    parts = filename[:-6].rsplit(".", 2)
                    if len(parts) >= 2:
                        # Convention: name.version.nupkg
                        # Find split point between name and version
                        full = filename[:-6]
                        # Version starts with digit
                        idx = full.rfind(".")
                        version = full[idx+1:]
                        name = full[:idx]
                        packages.append((name, version))

        return packages


def generate_sources_from_csproj(csproj_path):
    """Parse csproj and lock files to build package list."""
    lock_file = csproj_path.parent / "packages.lock.json"

    if lock_file.exists():
        print(f"Found lock file: {lock_file}")
        return get_packages_from_lock_file(lock_file)

    print(f"No lock file found at {lock_file}")
    print("Falling back to dotnet restore...")
    return get_packages_from_dotnet_restore(csproj_path)


def find_csproj():
    """Search for PacmanGame.csproj relative to script location."""
    search_paths = [
        SCRIPT_DIR / "src" / "PacmanGame" / "PacmanGame.csproj",
        SCRIPT_DIR.parent / "src" / "PacmanGame" / "PacmanGame.csproj",
        Path.cwd() / "src" / "PacmanGame" / "PacmanGame.csproj",
    ]
    for path in search_paths:
        if path.exists():
            return path
    return None


async def main():
    print("NuGet Sources Generator for Flatpak")
    print("=" * 50)

    csproj = find_csproj()
    if not csproj:
        print("Error: Could not find PacmanGame.csproj")
        print("Run this script from the flathub repo root or the project root.")
        sys.exit(1)

    print(f"Project: {csproj}")

    packages = generate_sources_from_csproj(csproj)

    if not packages:
        print("Error: No packages found.")
        print("Make sure the project builds locally first:")
        print("  dotnet restore src/PacmanGame/PacmanGame.csproj")
        sys.exit(1)

    print(f"Found {len(packages)} packages. Downloading and hashing...")
    sources = await process_packages(packages)

    output_file = SCRIPT_DIR / "generated-sources.json"
    with open(output_file, "w") as f:
        json.dump(sources, f, indent=2)

    print(f"\nDone. Generated {output_file} with {len(sources)} sources.")
    print("\nNext steps:")
    print("  1. Add 'generated-sources.json' as a source in your manifest")
    print("  2. Add '--source nuget-sources' to dotnet restore command")
    print("  3. Test with: flatpak-builder --force-clean --user --install build-dir manifest.yaml")


if __name__ == "__main__":
    asyncio.run(main())
