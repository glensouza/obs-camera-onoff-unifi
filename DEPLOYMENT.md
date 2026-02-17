# Deployment Guide for Mac Mini

This guide covers deploying the UniFi Camera Control system to a Mac Mini using GitHub Actions and a self-hosted runner.

## Overview

The deployment workflow:
1. Code is pushed to the `main` branch
2. GitHub Actions triggers on the self-hosted runner (Mac Mini)
3. The runner pulls the latest code
4. Docker Compose builds and starts the containers
5. The application is accessible on your local network

## Prerequisites

### On Mac Mini

- macOS (tested on macOS 12+)
- Docker Desktop for Mac installed and running
- Network access to UniFi Controller
- Static IP or reserved DHCP address (recommended)

### On GitHub

- Admin access to the repository
- Ability to add secrets and configure runners

## Part 1: Mac Mini Setup

### 1.1 Install Docker Desktop

1. Download [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop)
2. Install and open Docker Desktop
3. Go through the initial setup
4. Verify installation:
   ```bash
   docker --version
   docker-compose --version
   ```

### 1.2 Configure Docker to Start on Boot

1. Open Docker Desktop
2. Go to **Settings** → **General**
3. Check ✓ **Start Docker Desktop when you log in**
4. Click **Apply & Restart**

### 1.3 Create Working Directory

```bash
# Create a directory for the application
mkdir -p ~/github-runners
cd ~/github-runners
```

## Part 2: GitHub Self-Hosted Runner Setup

### 2.1 Get Runner Token

1. Go to your GitHub repository
2. Click **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Select **macOS** as the OS
5. Copy the token from the configuration command (you'll need it in step 2.3)

### 2.2 Download and Configure Runner

```bash
# Navigate to runners directory
cd ~/github-runners

# Create actions-runner directory
mkdir actions-runner && cd actions-runner

# Download the latest runner package for macOS
# Check https://github.com/actions/runner/releases for the latest version
curl -o actions-runner-osx-x64-2.313.0.tar.gz -L \
  https://github.com/actions/runner/releases/download/v2.313.0/actions-runner-osx-x64-2.313.0.tar.gz

# Extract the installer
tar xzf ./actions-runner-osx-x64-2.313.0.tar.gz

# Configure the runner
./config.sh --url https://github.com/YOUR_USERNAME/obs-camera-onoff-unifi --token YOUR_TOKEN_HERE
```

When prompted:
- **Runner name**: Press Enter for default or type `mac-mini-runner`
- **Runner group**: Press Enter for default
- **Labels**: Press Enter for default or add custom labels
- **Work folder**: Press Enter for default

### 2.3 Install Runner as Service

```bash
# Install the service (runs on boot)
sudo ./svc.sh install

# Start the service
sudo ./svc.sh start

# Check status
sudo ./svc.sh status
```

### 2.4 Verify Runner is Connected

1. Go back to GitHub → Settings → Actions → Runners
2. You should see your runner with a green "Idle" status

## Part 3: Configure GitHub Secrets

Add these repository secrets for secure configuration:

1. Go to your repository on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each of the following:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `UNIFI_HOST` | UniFi Controller URL | `https://192.168.1.1:8443` |
| `UNIFI_USERNAME` | UniFi admin username | `admin` |
| `UNIFI_PASSWORD` | UniFi admin password | `YourSecurePassword123` |
| `UNIFI_SITE` | UniFi site name | `default` |
| `UNIFI_SWITCH_MAC` | Switch MAC address | `aa:bb:cc:dd:ee:ff` |

**Important**: Never commit these values to your repository!

## Part 4: Deploy the Application

### 4.1 Manual Deployment

Trigger the workflow manually:

1. Go to **Actions** tab in GitHub
2. Select **Deploy to Mac Mini** workflow
3. Click **Run workflow**
4. Select branch `main`
5. Click **Run workflow**

Watch the workflow execute in real-time.

### 4.2 Automatic Deployment

Simply push to the `main` branch:

```bash
git add .
git commit -m "Update application"
git push origin main
```

The workflow will automatically trigger.

### 4.3 Monitor Deployment

1. In GitHub Actions, watch the workflow progress
2. Check for any errors in the logs
3. The workflow will show:
   - Checkout code
   - Create .env file
   - Stop existing containers
   - Build and start services
   - Display deployment info

## Part 5: Access the Application

### On the Same Network

Once deployed, access the application from any device on your network:

- **Frontend**: `http://MAC_MINI_IP:8080`
- **API**: `http://MAC_MINI_IP:7071/api`
- **Swagger**: `http://MAC_MINI_IP:7071/api/swagger/ui`

To find your Mac Mini's IP address:
```bash
ifconfig | grep "inet " | grep -v 127.0.0.1
```

### Configure Static IP (Recommended)

1. Go to **System Preferences** → **Network**
2. Select your active network connection
3. Click **Advanced** → **TCP/IP**
4. Change **Configure IPv4** to **Manually**
5. Set a static IP (e.g., `192.168.1.100`)
6. Set subnet mask (usually `255.255.255.0`)
7. Set router address (your gateway IP)
8. Click **OK** and **Apply**

Or use DHCP with a reserved address in your router settings.

## Part 6: Maintenance

### View Logs

SSH into your Mac Mini and run:

```bash
cd ~/github-runners/actions-runner/_work/obs-camera-onoff-unifi/obs-camera-onoff-unifi
docker-compose logs -f
```

### Restart Services

```bash
docker-compose restart
```

### Update Application

Just push new code to main branch, and it will auto-deploy.

### Stop Services

```bash
docker-compose down
```

### Remove Everything

```bash
docker-compose down -v  # Removes containers and volumes
```

## Part 7: Configure Auto-Start on Mac Boot

The runner service is already configured to start on boot. To ensure Docker starts:

1. System Preferences → Users & Groups → Login Items
2. Add Docker Desktop to login items

Or use a launch agent:

```bash
# Create launch agent
cat > ~/Library/LaunchAgents/com.docker.start.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.docker.start</string>
    <key>ProgramArguments</key>
    <array>
        <string>/Applications/Docker.app/Contents/MacOS/Docker</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
EOF

# Load the launch agent
launchctl load ~/Library/LaunchAgents/com.docker.start.plist
```

## Troubleshooting

### Runner Not Connecting

```bash
# Check runner service
sudo ./svc.sh status

# View runner logs
tail -f ~/github-runners/actions-runner/_diag/*.log
```

### Deployment Fails

1. Check workflow logs in GitHub Actions
2. Verify all secrets are set correctly
3. SSH into Mac Mini and check Docker:
   ```bash
   docker ps
   docker-compose logs
   ```

### Can't Access from Other Devices

1. Check Mac firewall settings:
   - System Preferences → Security & Privacy → Firewall
   - Add Docker to allowed applications
2. Verify Mac Mini IP address
3. Test connectivity: `ping MAC_MINI_IP`

### Port Already in Use

```bash
# Check what's using the port
sudo lsof -i :8080
sudo lsof -i :7071

# Stop the conflicting service or change ports in docker-compose.yml
```

## Security Considerations

1. **Firewall**: Configure Mac firewall to only allow necessary connections
2. **Network**: Consider placing Mac Mini on a separate VLAN
3. **Updates**: Keep macOS, Docker, and the application updated
4. **Secrets**: Never commit secrets to the repository
5. **SSH**: Use key-based authentication for SSH access
6. **HTTPS**: Consider adding SSL/TLS in production

## Next Steps

- Set up monitoring and alerts
- Configure backup for Azurite data
- Add authentication to the frontend
- Set up SSL certificates for HTTPS
- Configure port forwarding if remote access is needed (use VPN for security)

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review GitHub Actions workflow logs
3. Check Docker logs on Mac Mini
4. Open an issue on GitHub with relevant log output
