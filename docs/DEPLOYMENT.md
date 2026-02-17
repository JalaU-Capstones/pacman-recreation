# AWS EC2 Free Tier Deployment Guide

This guide provides step-by-step instructions for deploying the Pac-Man Multiplayer Relay Server to the AWS EC2 Free Tier.

## 1. Prerequisites

* An AWS account eligible for the Free Tier.
* An SSH client (e.g., OpenSSH, PuTTY).
* The .NET 9.0 SDK installed on your local machine.

## 2. AWS Account Setup

1.  Create a new AWS account at [aws.amazon.com/free](https://aws.amazon.com/free).
2.  Follow the on-screen instructions to verify your email address and add a payment method. A credit/debit card is required for verification but will not be charged for services covered by the Free Tier.
3.  Log in to the AWS Management Console and select a home region (e.g., `us-east-1` for North America, `sa-east-1` for South America).

## 3. Create EC2 Instance

1.  Navigate to the EC2 Dashboard and click **Launch Instance**.
2.  **Name:** `pacman-relay-server`
3.  **Application and OS Images (AMI):** Select **Ubuntu Server 24.04 LTS (HVM)**, ensuring it is marked as "Free tier eligible."
4.  **Instance type:** Choose **t2.micro** (1 vCPU, 1 GB RAM), which is eligible for the Free Tier.
5.  **Key pair (login):** Create a new key pair or select an existing one. Download the `.pem` file and store it securely.
6.  **Network settings:**
    - Click **Edit**.
    - **Security group name:** `pacman-server-sg`
    - **Inbound security groups rules:**
        - **Rule 1 (SSH):**
            - **Type:** SSH
            - **Source type:** My IP (This will automatically fill in your current IP address)
        - **Rule 2 (Relay Server):**
            - Click **Add security group rule**.
            - **Type:** Custom UDP
            - **Port range:** 9050
            - **Source:** Anywhere (0.0.0.0/0)
7.  **Configure storage:** The default 30 GB `gp3` General Purpose SSD is included in the Free Tier.
8.  Click **Launch instance**.

## 4. Connect to EC2 Instance

1.  Set the correct permissions for your key file:
    ```bash
    chmod 400 /path/to/your-key.pem
    ```
2.  Connect to the instance using SSH:
    ```bash
    ssh -i /path/to/your-key.pem ubuntu@<EC2_PUBLIC_IP>
    ```
    Replace `<EC2_PUBLIC_IP>` with the Public IPv4 address of your instance, found in the EC2 console.

## 5. Install .NET 9.0 Runtime

Execute the following commands on your EC2 instance:

```bash
# Update package lists and upgrade existing packages
sudo apt update && sudo apt upgrade -y

# Install prerequisites
sudo apt install -y wget apt-transport-https

# Register the Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install the .NET 9.0 runtime
sudo apt update
sudo apt install -y dotnet-runtime-9.0

# Verify the installation
dotnet --version
```

## 6. Build and Publish Server

On your **local machine**, from the solution's root directory:

```bash
# Navigate to the server project
cd src/PacmanGame.Server

# Publish the server for Linux x64
dotnet publish -c Release -r linux-x64 --self-contained false -o publish
```

## 7. Upload to EC2

From your **local machine**, use `scp` to upload the published files to the EC2 instance:

```bash
scp -i /path/to/your-key.pem -r src/PacmanGame.Server/publish ubuntu@<EC2_PUBLIC_IP>:~/pacman-server
```

## 8. Configure Systemd Service

On your **EC2 instance**, create a service file to manage the server process:

```bash
sudo nano /etc/systemd/system/pacman-server.service
```

Paste the following content into the editor:

```ini
[Unit]
Description=Pac-Man Multiplayer Relay Server
After=network.target

[Service]
Type=simple
User=ubuntu
WorkingDirectory=/home/ubuntu/pacman-server
ExecStart=/home/ubuntu/.dotnet/dotnet /home/ubuntu/pacman-server/PacmanGame.Server.dll
Restart=always
RestartSec=10
Environment=DOTNET_ROOT=/home/ubuntu/.dotnet
Environment=PATH=/home/ubuntu/.dotnet:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Save the file and exit (`Ctrl+X`, `Y`, `Enter`).

Now, enable and start the service:

```bash
sudo systemctl daemon-reload
sudo systemctl enable pacman-server
sudo systemctl start pacman-server
sudo systemctl status pacman-server
```

## 9. Configure Firewall (UFW)

Enable the Uncomplicated Firewall (UFW) on the EC2 instance:

```bash
sudo ufw allow 22/tcp    # Allow SSH
sudo ufw allow 9050/udp  # Allow relay server traffic
sudo ufw enable
sudo ufw status
```

## 10. Verify Server is Running

1.  Check if the server is listening on port 9050:
    ```bash
    sudo netstat -tulpn | grep 9050
    ```
2.  View the server logs in real-time:
    ```bash
    sudo journalctl -u pacman-server -f
    ```

## 11. Get Public IP for Client Configuration

You can find the public IP in the EC2 console, or by running this command on the instance:

```bash
curl http://checkip.amazonaws.com
```

## 12. Setup DDNS with Cloudflare (Recommended - FREE Alternative to Elastic IP)

**Problem:** AWS EC2 instances receive a new public IP address every time they are stopped and started. Elastic IPs incur charges (~$3.60/month) when the instance is stopped.
**Solution:** Use Dynamic DNS (DDNS) with Cloudflare to automatically update a domain (e.g., `pacmanserver.codewithbotina.com`) whenever the instance starts.

### 12.1. Create DNS Record in Cloudflare

1. Log in to the [Cloudflare Dashboard](https://dash.cloudflare.com).
2. Select your domain.
3. Navigate to **DNS settings** and add a new **A record**:
* **Type:** `A`
* **Name:** `pacmanserver`
* **IPv4 address:** `1.1.1.1` (This is temporary; the script will update it).
* **Proxy status:** **DNS Only** (⚠️ IMPORTANT: The cloud icon must be gray).
* **TTL:** `Auto` or `1 minute`.


4. Save the record.

### 12.2. Get Cloudflare API Credentials

1. **Zone ID:** Located on the domain **Overview** page in the right sidebar.
2. **API Token:**
* Go to **My Profile > API Tokens**.
* Click **Create Token** and use the **Edit zone DNS** template.
* **Permissions:** `Zone / DNS / Edit`.
* **Zone Resources:** `Include / Specific zone / <your-domain>`.
* Copy and store the token securely.



### 12.3. Install DDNS Script on EC2

Connect to your EC2 instance and create the update script:

```bash
sudo nano /usr/local/bin/update-cloudflare-dns.sh

```

Paste the following content, replacing the placeholders with your actual credentials:

```bash
#!/bin/bash

# Cloudflare credentials
ZONE_ID="YOUR_ZONE_ID_HERE"
API_TOKEN="YOUR_API_TOKEN_HERE"
RECORD_NAME="pacmanserver.codewithbotina.com"

# 1. Get current public IP
CURRENT_IP=$(curl -s https://api.ipify.org)

if [ -z "$CURRENT_IP" ]; then
    echo "[ERROR] Could not detect public IP"
    exit 1
fi

echo "[INFO] Detected IP: $CURRENT_IP"

# 2. Get DNS Record ID from Cloudflare
RECORD_ID=$(curl -s -X GET "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records?name=$RECORD_NAME" \
    -H "Authorization: Bearer $API_TOKEN" \
    -H "Content-Type: application/json" | grep -Po '"id":"\K[^"]*' | head -1)

if [ -z "$RECORD_ID" ]; then
    echo "[ERROR] Could not find DNS record for $RECORD_NAME"
    exit 1
fi

# 3. Update the DNS record in Cloudflare
curl -s -X PUT "https://api.cloudflare.com/client/v4/zones/$ZONE_ID/dns_records/$RECORD_ID" \
    -H "Authorization: Bearer $API_TOKEN" \
    -H "Content-Type: application/json" \
    --data "{\"type\":\"A\",\"name\":\"$RECORD_NAME\",\"content\":\"$CURRENT_IP\",\"ttl\":1,\"proxied\":false}"

echo ""
echo "[SUCCESS] DNS updated: $RECORD_NAME -> $CURRENT_IP"

```

Make the script executable:

```bash
sudo chmod +x /usr/local/bin/update-cloudflare-dns.sh

```

### 12.4. Automate DNS Update on Boot

Create a systemd service to run the script at startup:

```bash
sudo nano /etc/systemd/system/cloudflare-ddns.service

```

Paste the following configuration:

```ini
[Unit]
Description=Cloudflare DDNS Update Service
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
ExecStartPre=/bin/sleep 10
ExecStart=/usr/local/bin/update-cloudflare-dns.sh

[Install]
WantedBy=multi-user.target

```

Enable the service:

```bash
sudo systemctl daemon-reload
sudo systemctl enable cloudflare-ddns.service

```

## 13. Update Client Configuration

Update `src/PacmanGame/Helpers/Constants.cs` in the client application to use your domain name instead of a static IP address:

```csharp
// The domain name will resolve to the correct IP even after server restarts.
public const string MultiplayerServerIP = "pacmanserver.codewithbotina.com";
public const int MultiplayerServerPort = 9050;

```

## 14. Update Server (After Code Changes)

To deploy updates, publish the server locally and upload the files to the EC2 instance. Then, restart the server service:

```bash
sudo systemctl restart pacman-server

```

## 15. Monitoring and Troubleshooting

* **Check DDNS Logs:** `sudo journalctl -u cloudflare-ddns.service`
* **Verify DNS Resolution:** `dig +short pacmanserver.codewithbotina.com`
* **Server Logs:** `sudo journalctl -u pacman-server -f`

## 16. Cost Management

* **DDNS Savings:** Using this script instead of an Elastic IP avoids the $0.005/hour charge incurred when the instance is stopped.
* **Free Tier:** Monitor usage to stay within the 750 hours/month limit for `t2.micro` instances.

## 17. (Optional) Elastic IP (Static IP)

📌 **Note:** If you configured DDNS in Section 12, an Elastic IP is **not** required. This section is only for users who prefer a static IP over a dynamic domain setup.

## 18. Backup Strategy

Create snapshots of your instance volume periodically to prevent data loss.

---

#### Section: Linux Distribution via Flathub

**Complete step-by-step guide for submitting to Flathub:**

1. **Prerequisites**
   - Flatpak and flatpak-builder installed
   - GitHub account
   - Fork of flathub/flathub repository

2. **Create Flatpak Manifest**
   - File location: `flatpak/com.codewithbotina.PacmanRecreation.yaml`
   - Include complete manifest with:
     - App ID: `com.codewithbotina.PacmanRecreation`
     - Runtime: `org.freedesktop.Platform` version `24.08` (current LTS)
     - SDK: `org.freedesktop.Sdk`
     - Runtime version: Latest stable
     - Command: `pacman-recreation`
     - Finish args (permissions):
       - `--share=network` (multiplayer UDP)
       - `--socket=wayland` (rendering)
       - `--socket=fallback-x11` (X11 fallback)
       - `--device=dri` (GPU acceleration)
       - `--socket=pulseaudio` (audio)
       - `--filesystem=xdg-data/pacman-recreation:create` (SQLite database)
     - Modules:
       - .NET 9.0 SDK download
       - Build commands for `dotnet publish -r linux-x64 --self-contained`
       - Install commands to copy files to `/app/bin`

3. **Create Desktop Entry**
   - File location: `flatpak/com.codewithbotina.PacmanRecreation.desktop`
   - Include:
     - Name: Pacman Recreation
     - Comment: Classic Pac-Man game with multiplayer
     - Exec: `pacman-recreation`
     - Icon: `com.codewithbotina.PacmanRecreation`
     - Categories: Game;ArcadeGame;
     - Terminal: false

4. **Create AppStream Metadata**
   - File location: `flatpak/com.codewithbotina.PacmanRecreation.metainfo.xml`
   - Include:
     - Name, summary, description
     - Screenshots (at least 2)
     - Releases section (version history)
     - Content rating (OARS)
     - Developer info

5. **Prepare Icons**
   - Required sizes: 64x64, 128x128, 256x256, 512x512
   - Location: `flatpak/icons/`
   - Format: PNG
   - Naming: `com.codewithbotina.PacmanRecreation.png`

6. **Test Locally**
   ```bash
   # Install required runtime and SDK (if not already installed)
   flatpak install -y flathub org.freedesktop.Platform//24.08
   flatpak install -y flathub org.freedesktop.Sdk//24.08

   # Build and install the application
   flatpak-builder --force-clean --user --install build-dir \
     flatpak/com.codewithbotina.PacmanRecreation.yaml
   
   # Run the application
   flatpak run com.codewithbotina.PacmanRecreation
   ```
   **Note:** The flatpak-builder command will automatically download the runtime and SDK if not already installed, but manually installing them first can provide better error messages.

7. **Submit to Flathub**
   - Fork https://github.com/flathub/flathub
   - Create branch: `add-pacman-recreation`
   - Add manifest to root of fork
   - Open Pull Request to flathub/flathub
   - Title: "Add Pacman Recreation"
   - Description: Brief description of the app
   - Wait for review (typically 1-2 weeks)
   - Address reviewer feedback
   - Once merged, app appears on Flathub within 24 hours

8. **Update Process**
   - For updates, open PR to your app's repo in flathub org
   - Update manifest with new version and commit hash
   - No need to fork flathub/flathub again

---

#### Section: Windows Distribution via Winget

**Complete step-by-step guide for submitting to Winget:**

1. **Prerequisites**
   - GitHub account
   - Fork of microsoft/winget-pkgs repository
   - Published release on GitHub with Windows executable

2. **Build Windows Executable**
   ```bash
   dotnet publish src/PacmanGame/PacmanGame.csproj \
     -c Release \
     -r win-x64 \
     --self-contained \
     -o artifacts/windows
   
   cd artifacts/windows
   zip -r PacmanRecreation-Windows-v1.0.0.zip *
   ```

3. **Create GitHub Release**
   - Go to GitHub repository → Releases → Create new release
   - Tag: `v1.0.0`
   - Title: `Pacman Recreation v1.0.0`
   - Description: Changelog for v1.0.0
   - Upload `PacmanRecreation-Windows-v1.0.0.zip`
   - Publish release
   - Copy download URL of the zip file

4. **Calculate SHA256 Hash**
   ```bash
   sha256sum PacmanRecreation-Windows-v1.0.0.zip
   ```
   Save this hash for the manifest.

5. **Create Winget Manifest**
   - Fork https://github.com/microsoft/winget-pkgs
   - Create directory: `manifests/c/CodeWithBotina/PacmanRecreation/1.0.0/`
   - Create 4 YAML files:
     - `CodeWithBotina.PacmanRecreation.yaml` (version manifest)
     - `CodeWithBotina.PacmanRecreation.installer.yaml` (installer details)
     - `CodeWithBotina.PacmanRecreation.locale.en-US.yaml` (English metadata)
     - `CodeWithBotina.PacmanRecreation.locale.es-CO.yaml` (Spanish metadata)

6. **Manifest Content**
   
   **Version Manifest:**
   ```yaml
   PackageIdentifier: CodeWithBotina.PacmanRecreation
   PackageVersion: 1.0.0
   DefaultLocale: en-US
   ManifestType: version
   ManifestVersion: 1.6.0
   ```
   
   **Installer Manifest:**
   ```yaml
   PackageIdentifier: CodeWithBotina.PacmanRecreation
   PackageVersion: 1.0.0
   InstallerType: zip
   InstallerSha256: <SHA256_HASH_HERE>
   Installers:
     - Architecture: x64
       InstallerUrl: https://github.com/JalaU-Capstones/pacman-recreation/releases/download/v1.0.0/PacmanRecreation-Windows-v1.0.0.zip
   ManifestType: installer
   ManifestVersion: 1.6.0
   ```
   
   **Locale Manifest (en-US):**
   ```yaml
   PackageIdentifier: CodeWithBotina.PacmanRecreation
   PackageVersion: 1.0.0
   PackageLocale: en-US
   Publisher: Code With Botina
   PackageName: Pacman Recreation
   License: MIT
   ShortDescription: Classic Pac-Man game with multiplayer support
   Description: A faithful recreation of the classic Pac-Man arcade game...
   Moniker: pacman-recreation
   Tags:
     - game
     - pacman
     - multiplayer
     - arcade
   ManifestType: defaultLocale
   ManifestVersion: 1.6.0
   ```

7. **Validate Manifest**
   ```bash
   winget validate manifests/c/CodeWithBotina/PacmanRecreation/1.0.0/
   ```

8. **Submit to Winget**
   - Commit manifests to your fork
   - Open Pull Request to microsoft/winget-pkgs
   - Title: "New package: CodeWithBotina.PacmanRecreation version 1.0.0"
   - Description: Brief description of the package
   - Wait for automated validation (passes in minutes)
   - Wait for human review (typically 1-3 days)
   - Once merged, package is available via `winget install CodeWithBotina.PacmanRecreation`

9. **Update Process**
   - For updates, create new directory for new version
   - Update all 4 YAML files with new version, URL, hash
   - Open PR with title: "Update: CodeWithBotina.PacmanRecreation version X.Y.Z"
