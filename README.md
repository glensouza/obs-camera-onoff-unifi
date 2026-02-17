# UniFi Camera Control for OBS

A Blazor Server application to control UniFi PoE switch ports for PTZ Optics cameras during livestream events. Provides a web-based control panel optimized for OBS Browser Docks.

## Features

- **Blazor Server UI**: Interactive control panel with real-time status updates
- **Database**: Azure Table Storage (Azurite for local development)
- **Port Control**: Turn camera ports on/off via PoE control
- **Status Monitoring**: Real-time port status with auto-refresh
- **Docker Support**: Two-container setup with Docker Compose (app + Azurite)
- **OBS Optimized**: Dark theme designed for OBS custom browser docks

## Architecture

```
┌─────────────────┐
│  OBS Browser    │
│     Dock        │
│  (Blazor UI)    │
└────────┬────────┘
         │ SignalR
         ▼
┌─────────────────┐
│  Blazor Server  │
│  (.NET 10)      │
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌───────────────────┐
│Azurite │ │ UniFi Controller  │
│(Table) │ │   (PoE Switch)    │
└────────┘ └───────────────────┘
```

## Prerequisites

### For Local Development
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- A UniFi Controller with a PoE switch
- Access to your UniFi Controller credentials

### For Deployment (Mac Mini)
- Mac Mini with Docker Desktop installed
- GitHub self-hosted runner configured
- Network access to UniFi Controller

## Local Development Setup

### 1. Clone the Repository
```bash
git clone https://github.com/glensouza/obs-camera-onoff-unifi.git
cd obs-camera-onoff-unifi
```

### 2. Configure Environment Variables
Create a `.env` file from the example:
```bash
cp .env.example .env
```

Edit `.env` with your UniFi Controller details:
```env
UNIFI_HOST=https://192.168.1.1:8443
UNIFI_USERNAME=admin
UNIFI_PASSWORD=your-password
UNIFI_SITE=default
UNIFI_SWITCH_MAC=aa:bb:cc:dd:ee:ff
UNIFI_IGNORE_SSL=true
```

**Finding your Switch MAC Address:**
1. Log into your UniFi Controller
2. Go to Devices
3. Click on your switch
4. Copy the MAC address (usually shown at the top)

### 3. Start Services with Docker Compose
```bash
docker-compose up -d
```

This will start:
- **Azurite** (Azure Storage emulator) on ports 10000-10002
- **Blazor Server App** on port 8080

### 4. Access the Application

Open your browser and navigate to: **http://localhost:8080**

### 5. View Logs
```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f app
docker-compose logs -f azurite
```

### 6. Stop Services
```bash
docker-compose down
```

## OBS Integration

### Adding as a Custom Browser Dock

This UI is intended to be used as an OBS "Custom Browser Dock" (a docked panel inside OBS), not as a scene Browser Source. Using a dock gives you a persistent control panel while you manage scenes and sources.

1. Open OBS Studio
2. Window → Docks → Custom Browser Docks...
3. Click the "+" button to add a new dock
4. Enter a name (e.g. "Camera Control")
5. Set the URL to `http://localhost:8080` (or your deployment URL)
6. Set a reasonable width (e.g. 400) and height (e.g. 800) and click "Apply"

If you prefer a floating panel inside a scene, use a Browser Source and set the URL to the same endpoint.

### Recommended Settings

- Use the dock for persistent access to controls during production
- For a compact dock set width to ~320–480 and height to ~600–1000 depending on your workflow
- If using as a Browser Source in a scene, enable "Refresh browser when scene becomes active" (recommended)

## Deployment to Mac Mini

### 1. Setup Self-Hosted Runner

On your Mac Mini, install and configure a GitHub self-hosted runner:

```bash
mkdir actions-runner && cd actions-runner
curl -o actions-runner-osx-x64-2.313.0.tar.gz -L \
  https://github.com/actions/runner/releases/download/v2.313.0/actions-runner-osx-x64-2.313.0.tar.gz
tar xzf ./actions-runner-osx-x64-2.313.0.tar.gz
./config.sh --url https://github.com/glensouza/obs-camera-onoff-unifi --token YOUR_TOKEN
sudo ./svc.sh install
sudo ./svc.sh start
```

### 2. Configure GitHub Secrets

Add the following secrets to your GitHub repository (Settings → Secrets and variables → Actions):
- `UNIFI_HOST`: Your UniFi Controller URL
- `UNIFI_USERNAME`: UniFi admin username
- `UNIFI_PASSWORD`: UniFi admin password
- `UNIFI_SITE`: UniFi site name (usually "default")
- `UNIFI_SWITCH_MAC`: Your switch MAC address

### 3. Deploy

Push to the main branch or manually trigger the workflow:

```bash
git add .
git commit -m "Deploy application"
git push origin main
```

### 4. Verify Deployment

After the workflow completes, access:
- Application: `http://YOUR_MAC_MINI_IP:8080`

## Troubleshooting

### App won't start
```bash
docker-compose logs app
docker-compose restart app
```

### Can't connect to UniFi Controller
- Verify `UNIFI_HOST` is correct (include `https://` and port `:8443`)
- Check if SSL errors by setting `UNIFI_IGNORE_SSL=true`
- Ensure Mac Mini can reach the UniFi Controller on the network
- Verify credentials are correct

### Azurite connection issues
```bash
docker-compose restart azurite
docker-compose logs azurite
```

### Port Configuration Not Saving
- Check Azurite is running: `docker-compose ps`
- Clear Azurite data: `docker-compose down -v` (warning: deletes all data)

### No cameras showing up
- Check if app is running: `docker-compose ps`
- Check app logs: `docker-compose logs app`
- Check browser console for errors

## Development

### Running Locally (without Docker)

```bash
cd OBSCameraPowerControl

# Install and start Azurite
npm install -g azurite
azurite --silent --location ./azurite-data --debug ./azurite-data/debug.log

# In another terminal, run the app
dotnet run
```

## License

MIT

1. Open http://localhost:7071/api/swagger/ui
2. Expand an endpoint
3. Click "Try it out"
4. Enter parameters
5. Click "Execute"

## Project Structure

```
.
├── .github/
│   └── workflows/
│       └── deploy.yml          # GitHub Actions deployment workflow
├── src/
│   ├── backend/                # .NET Function App
│   │   ├── Functions/          # HTTP Functions
│   │   ├── Models/             # Data models
│   │   ├── Services/           # Business logic
│   │   ├── Dockerfile          # Backend container
│   │   └── *.csproj            # .NET project file
│   └── frontend/               # Web UI
│       ├── index.html          # Main HTML
│       ├── styles.css          # Styling
│       └── app.js              # JavaScript logic
├── docker-compose.yml          # Multi-container orchestration
├── nginx.conf                  # Nginx configuration
├── .env.example                # Environment template
└── README.md                   # This file
```

## Security Notes

- Never commit `.env` files or credentials
- Use strong passwords for UniFi Controller
- Consider using HTTPS in production
- Restrict network access to necessary IPs only
- Keep Docker and dependencies updated

## License

[MIT License](LICENSE)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Support

For issues and questions:
- Open an issue on GitHub
- Check existing issues for solutions
- Review logs for error messages