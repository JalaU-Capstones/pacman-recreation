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

### ARM64 Support

The Flatpak manifest supports both x86_64 and ARM64 architectures.
Build command for ARM64:

```bash
flatpak-builder --force-clean --arch=aarch64 \
  --user --install build-dir-arm64 \
  io.github.jalaucapstones.pacman-recreation.yaml
```

The manifest automatically handles architecture-specific .NET runtime packs.

## Global Leaderboard Database Setup

1. SSH into server:
   ```bash
   ssh -i pacman-server-key.pem ubuntu@pacmanserver.codewithbotina.com
   ```

2. Create database directory:
   ```bash
   sudo mkdir -p /var/lib/pacman-server
   sudo chown ubuntu:ubuntu /var/lib/pacman-server
   ```

3. Initialize database:
   ```bash
   cd /var/lib/pacman-server
   sqlite3 global_leaderboard.db < schema.sql
   ```

4. Set permissions:
   ```bash
   chmod 644 global_leaderboard.db
   ```

5. Update server service to include leaderboard:
   ```bash
   sudo systemctl restart pacman-server
   ```

6. Verify database is working:
   ```bash
   sqlite3 global_leaderboard.db "SELECT * FROM GlobalLeaderboard;"
   ```

## Winget Deployment (Windows)

### Prerequisites
- Winget Package Manager (Windows 10 1809+)
- Valid Microsoft Store developer account (if using Store distribution)
- Code signing certificate (recommended)

### Build Release
```bash
dotnet publish src/PacmanGame/PacmanGame.csproj -c Release -r win-x64 --self-contained
```

### Installation
Users can install via:
```powershell
winget install JalaU-Capstones.PacmanRecreation
```
