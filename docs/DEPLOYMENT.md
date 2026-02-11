# AWS EC2 Free Tier Deployment Guide

This guide provides step-by-step instructions for deploying the Pac-Man Multiplayer Relay Server to the AWS EC2 Free Tier.

## 1. Prerequisites

- An AWS account eligible for the Free Tier.
- An SSH client (e.g., OpenSSH, PuTTY).
- The .NET 9.0 SDK installed on your local machine.

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
ExecStart=/usr/bin/dotnet /home/ubuntu/pacman-server/PacmanGame.Server.dll
Restart=always
RestartSec=10
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

## 12. Update Client Configuration

In the client application source code, update the server IP in `src/PacmanGame/Helpers/Constants.cs`:

```csharp
public const string MultiplayerServerIP = "<EC2_PUBLIC_IP>";
public const int MultiplayerServerPort = 9050;
```

## 13. Update Server (After Code Changes)

To deploy a new version of the server:

1.  On your **local machine**, publish the updated server:
    ```bash
    cd src/PacmanGame.Server
    dotnet publish -c Release -r linux-x64 --self-contained false -o publish
    ```
2.  Upload the new files:
    ```bash
    scp -i /path/to/your-key.pem -r publish/* ubuntu@<EC2_PUBLIC_IP>:~/pacman-server/
    ```
3.  On the **EC2 instance**, restart the service:
    ```bash
    sudo systemctl restart pacman-server
    ```

## 14. Monitoring and Troubleshooting

-   **System Logs:** `sudo journalctl -u pacman-server --no-pager -n 100`
-   **Resource Usage:** `htop`
-   **Disk Space:** `df -h`
-   **Connectivity Issues:** Double-check the security group rules in the AWS EC2 console.

## 15. Cost Management

The AWS Free Tier for EC2 includes:
- 750 hours per month of a `t2.micro` or `t3.micro` instance.
- 30 GB of EBS storage.
- 15 GB of data transfer out per month.

**After 12 months, or if you exceed the free tier limits, you will be charged.** To avoid unexpected costs, you can stop the instance when it is not in use.

## 16. Optional: Elastic IP (Static IP)

To get a static IP address that persists between instance restarts:

1.  In the EC2 Console, navigate to **Elastic IPs** and allocate a new address.
2.  Associate the Elastic IP with your EC2 instance.

An Elastic IP is free as long as it is associated with a running instance.

## 17. Backup Strategy

Create a snapshot of your instance's volume for backup:

1.  In the EC2 Console, select your instance.
2.  Go to **Actions > Image and templates > Create image**.
3.  This creates an Amazon Machine Image (AMI) which can be used to restore your server.
