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
