# UniFi Camera Control for OBS

A system to control UniFi PoE switch ports for PTZ Optics cameras during livestream events. Includes a .NET backend API and an OBS-compatible web panel for controlling 3 camera ports.

## Features

- **Backend API**: .NET 10 Azure Functions app with Swagger UI
- **Frontend Panel**: Web-based control panel optimized for OBS Browser Source
- **Database**: Azure Table Storage (Azurite for local development)
- **Port Control**: Turn camera ports on/off via PoE control
- **Status Monitoring**: Real-time port status with auto-refresh
- **Docker Support**: Fully containerized with Docker Compose

## Architecture

```
┌─────────────────┐
│  OBS Browser    │
│     Source      │
│  (Frontend UI)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────┐
│     Nginx       │─────▶│   Backend    │
│   (Reverse      │      │ (.NET Func)  │
│     Proxy)      │      └──────┬───────┘
└─────────────────┘             │
                                ▼
                    ┌───────────────────┐
                    │     Azurite       │
                    │ (Table Storage)   │
                    └───────────────────┘
                                │
                                ▼
                    ┌───────────────────┐
                    │ UniFi Controller  │
                    │   (PoE Switch)    │
                    └───────────────────┘
```

## Prerequisites

### For Local Development
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) (optional, for local function debugging)
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
- **Backend API** on port 7071
- **Frontend** on port 8080

### 4. Access the Application

- **Frontend UI**: http://localhost:8080
- **Backend API**: http://localhost:7071/api
- **Swagger UI**: http://localhost:7071/api/swagger/ui
- **OpenAPI JSON**: http://localhost:7071/api/openapi/v3.json

### 5. View Logs
```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f azurite
```

### 6. Stop Services
```bash
docker-compose down
```

## API Endpoints

### Get All Ports Status
```
GET /api/ports/status
```

Response:
```json
[
  {
    "portNumber": 1,
    "portName": "Camera 1",
    "isEnabled": true,
    "poeEnabled": true
  }
]
```

### Get Single Port Status
```
GET /api/ports/{portNumber}/status
```

### Set Port State
```
POST /api/ports/{portNumber}/state
Content-Type: application/json

{
  "portNumber": 1,
  "enable": true
}
```

## OBS Integration

### Adding to OBS

1. Open OBS Studio
2. Add a new **Browser** source
3. Configure the source:
   - **URL**: `http://localhost:8080` (or your deployment URL)
   - **Width**: 1920
   - **Height**: 1080
   - **FPS**: 30
   - Check "Shutdown source when not visible" (optional)
   - Check "Refresh browser when scene becomes active" (recommended)

### Recommended Settings

For a clean overlay:
- Use a smaller resolution like 800x600 if you want a compact control panel
- Enable "Control audio via OBS" if needed
- Position in a corner or use as a dock panel

## Deployment to Mac Mini

### 1. Setup Self-Hosted Runner

On your Mac Mini, install and configure a GitHub self-hosted runner:

```bash
# Create a directory for the runner
mkdir actions-runner && cd actions-runner

# Download the latest runner package
curl -o actions-runner-osx-x64-2.313.0.tar.gz -L \
  https://github.com/actions/runner/releases/download/v2.313.0/actions-runner-osx-x64-2.313.0.tar.gz

# Extract the installer
tar xzf ./actions-runner-osx-x64-2.313.0.tar.gz

# Configure the runner
./config.sh --url https://github.com/glensouza/obs-camera-onoff-unifi --token YOUR_TOKEN

# Install as a service (optional)
sudo ./svc.sh install

# Start the runner service
sudo ./svc.sh start
```

**Getting the Runner Token:**
1. Go to your GitHub repository
2. Settings → Actions → Runners → New self-hosted runner
3. Copy the token from the configuration command

### 2. Configure GitHub Secrets

Add the following secrets to your GitHub repository:
1. Go to Settings → Secrets and variables → Actions
2. Add these repository secrets:
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

Or trigger manually:
1. Go to Actions tab in GitHub
2. Select "Deploy to Mac Mini"
3. Click "Run workflow"

### 4. Verify Deployment

After the workflow completes, access:
- Frontend: `http://YOUR_MAC_MINI_IP:8080`
- API: `http://YOUR_MAC_MINI_IP:7071/api`
- Swagger: `http://YOUR_MAC_MINI_IP:7071/api/swagger/ui`

## Troubleshooting

### Backend won't start
```bash
# Check backend logs
docker-compose logs backend

# Restart backend only
docker-compose restart backend
```

### Can't connect to UniFi Controller
- Verify `UNIFI_HOST` is correct (include `https://` and port `:8443`)
- Check if SSL errors by setting `UNIFI_IGNORE_SSL=true`
- Ensure Mac Mini can reach the UniFi Controller on the network
- Verify credentials are correct

### Azurite connection issues
```bash
# Restart Azurite
docker-compose restart azurite

# Check Azurite logs
docker-compose logs azurite
```

### Port Configuration Not Saving
- Check Azurite is running: `docker-compose ps`
- Verify Table Storage connection string in backend logs
- Clear Azurite data: `docker-compose down -v` (warning: deletes all data)

### Frontend shows "No cameras configured"
- Check if backend is running: `curl http://localhost:7071/api/ports/status`
- Verify port configurations in Azurite
- Check browser console for errors

## Development

### Running Backend Locally (without Docker)

```bash
cd src/backend

# Install Azurite globally
npm install -g azurite

# Start Azurite
azurite --silent --location ./azurite-data --debug ./azurite-data/debug.log

# In another terminal, run the function app
func start
```

### Building Frontend

The frontend is static HTML/CSS/JS, no build process needed. Just edit the files in `src/frontend/`.

### Testing API with Swagger

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