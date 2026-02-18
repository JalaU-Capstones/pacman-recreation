# Deployment Guide

## Flathub Submission

### Prerequisites
- Runtime: org.freedesktop.Platform 25.08
- SDK: org.freedesktop.Sdk 25.08
- SDK Extension: org.freedesktop.Sdk.Extension.dotnet9

### Generate Sources
Run the `tools/generate-nuget-sources.py` script to generate the `generated-sources.json` file.

### Flathub Directory Structure
- `io.github.jalaucapstones.pacman-recreation.yaml`
- `io.github.jalaucapstones.pacman-recreation.desktop`
- `io.github.jalaucapstones.pacman-recreation.metainfo.xml`
- `generated-sources.json`
- Icons (64x64.png, 128x128.png, etc.)

### Update Commit Hash
After each release, update the `commit` field in the manifest with the new commit hash.

### Submission Process
1. Fork `flathub/flathub`.
2. Create a branch with the App ID.
3. Copy the files from the `flathub` directory to the fork.
4. Create a Pull Request.
5. Wait for review and address feedback.

### Local Testing
```bash
flatpak install flathub org.freedesktop.Platform//25.08
flatpak install flathub org.freedesktop.Sdk//25.08
flatpak install flathub org.freedesktop.Sdk.Extension.dotnet9//25.08
flatpak install flathub org.freedesktop.Platform.ffmpeg-full//24.08

flatpak-builder --force-clean --user --install --ccache --repo=repo-dir build-dir io.github.jalau_capstones.pacman-recreation.yaml

flatpak run io.github.jalau_capstones.pacman-recreation

flatpak run --command=flatpak-builder-lint org.flatpak.Builder \
  manifest io.github.jalau_capstones.pacman-recreation.yaml
  
flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo repo-dir
```
