# Deployment Guide

**Release target:** v1.0.1

## Flathub Submission

### Prerequisites
- Runtime: org.freedesktop.Platform 25.08
- SDK: org.freedesktop.Sdk 25.08
- SDK Extension: org.freedesktop.Sdk.Extension.dotnet9

### Generate Sources
Run the `tools/generate-nuget-sources.py` script to generate the `generated-sources.json` file.

### Flathub Directory Structure
- `io.github.jalau_capstones.pacman-recreation.yaml`
- `io.github.jalau_capstones.pacman-recreation.desktop`
- `io.github.jalau_capstones.pacman-recreation.metainfo.xml`
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
  io.github.jalau_capstones.pacman-recreation.yaml
```

The manifest automatically handles architecture-specific .NET runtime packs.

### ARM64 (aarch64) Requirements

- Keep `org.freedesktop.Sdk.Extension.dotnet9` enabled (required for building on Flathub infra).
- Ensure the manifest does not hardcode x86_64-only .NET packs or RID-specific paths.
- Validate both architectures locally when possible:
  - `flatpak-builder --arch=aarch64 ...`
  - `flatpak-builder --arch=x86_64 ...`

## Server Deployment (EC2 + Cloudflare + Custom DNS)

This project ships a UDP relay server (`src/PacmanGame.Server`) that also hosts the Global Top 10 storage service.

### 1) EC2 Instance

- Recommended: Ubuntu 22.04 LTS, t3.small (or larger if load increases).
- Security group rules:
  - UDP `9050` (game relay + leaderboard messages)
  - TCP `22` (SSH) restricted to your IP
- Optional: Use IPv6 and add an AAAA record for better connectivity.

### 2) Publish Server

From the repo root:

```bash
dotnet publish src/PacmanGame.Server/PacmanGame.Server.csproj -c Release -r linux-x64 --self-contained -o artifacts/server
```

Copy `artifacts/server` to the instance (example using `scp`):

```bash
scp -r artifacts/server/* ubuntu@YOUR_SERVER_IP:/home/ubuntu/pacman-server
```

### 3) Systemd Service

On the instance:

```bash
sudo mkdir -p /opt/pacman-server
sudo cp -r /home/ubuntu/pacman-server/* /opt/pacman-server/
sudo useradd --system --no-create-home --shell /usr/sbin/nologin pacmanserver || true
sudo chown -R pacmanserver:pacmanserver /opt/pacman-server
```

Create `/etc/systemd/system/pacman-server.service`:

```ini
[Unit]
Description=Pacman Recreation Relay Server
After=network.target

[Service]
Type=simple
User=pacmanserver
WorkingDirectory=/opt/pacman-server
ExecStart=/opt/pacman-server/PacmanGame.Server
Restart=always
RestartSec=2
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable pacman-server
sudo systemctl restart pacman-server
sudo systemctl status pacman-server --no-pager
```

### 4) Cloudflare DNS / Custom Domain

- Create an `A` record: `pacmanserver` -> `YOUR_SERVER_IP`.
- For UDP, Cloudflare proxying typically does not apply (unless using Cloudflare Spectrum).
  - Set the record to DNS-only (grey cloud) unless you have Spectrum configured.
- Verify resolution:
  - `dig pacmanserver.yourdomain.com A`

## Global Leaderboard Database Setup

1. SSH into server:
   ```bash
   ssh -i pacman-server-key.pem ubuntu@pacmanserver.yourdomain.com
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

### Desktop Shortcut (Icon)
On first run, the app creates Desktop and Start Menu shortcuts (best-effort).
As of `v1.0.1`, shortcut icons resolve correctly by pointing the shortcut icon to `Assets/icon.ico` in the install/run directory (copied on publish).

### Installation
Users can install via:
```powershell
winget install CodeWithBotina.PacmanRecreation
```

### Publishing Steps (winget-pkgs)

1. Produce a stable installer artifact:
   - Recommended: MSIX (signed) or a ZIP + installer (Inno Setup / WiX).
2. Create/update the WinGet manifest:
   - Use `wingetcreate new` or `komac` to generate manifests from the release URL.
3. Submit a PR to `microsoft/winget-pkgs`:
   - Version must match the Git tag (e.g., `v1.0.1`).
   - SHA256 must match the published asset.
4. Validate locally before PR:
   - `winget validate --manifest <path>`
   - `winget install --manifest <path>`
