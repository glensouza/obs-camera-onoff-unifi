# Quick Start Guide

This guide will help you get the UniFi Camera Control system up and running in under 10 minutes.

## Prerequisites Check

Before starting, ensure you have:
- [ ] Docker Desktop installed and running
- [ ] Access to your UniFi Controller
- [ ] Your UniFi admin credentials
- [ ] Your switch MAC address

## Step-by-Step Setup

### 1. Get the Switch MAC Address

1. Open your UniFi Controller web interface
2. Navigate to **Devices**
3. Click on your PoE switch
4. Copy the **MAC Address** (format: `aa:bb:cc:dd:ee:ff`)

### 2. Configure Environment

```bash
# Clone the repository
git clone https://github.com/glensouza/obs-camera-onoff-unifi.git
cd obs-camera-onoff-unifi

# Copy the example environment file
cp .env.example .env

# Edit .env with your details
nano .env  # or use your preferred editor
```

Update these values in `.env`:
```env
UNIFI_HOST=https://192.168.1.1:8443     # Your UniFi Controller URL
UNIFI_USERNAME=admin                     # Your UniFi username
UNIFI_PASSWORD=YourPassword123           # Your UniFi password
UNIFI_SWITCH_MAC=aa:bb:cc:dd:ee:ff      # Your switch MAC address
```

### 3. Start the Services

```bash
# Start all containers
docker-compose up -d

# Wait about 30 seconds for services to initialize
sleep 30

# Check that all services are running
docker-compose ps
```

You should see 2 containers running:
- `unifi-camera-app`
- `unifi-camera-azurite`

### 4. Test the Application

Open your web browser and navigate to:

**Application**: http://localhost:8080

You should see:
- A dark-themed control panel
- Three camera cards (Camera 1, Camera 2, Camera 3)
- Toggle switches for each camera
- Status indicators showing if ports are on/off

### 5. Configure OBS

1. Open **OBS Studio**
2. Window → Docks → Custom Browser Docks...
3. Click the "+" button to add a new dock
4. Enter a name (e.g. "Camera Control")
5. Set the URL to `http://localhost:8080`
6. Set width to ~400 and height to ~800
7. Click **Apply**

### 6. Test the Controls

1. In the OBS dock or web browser, toggle a camera switch
2. Watch the status indicator change
3. Verify the actual port on your switch changes state
4. Check your camera power

## Troubleshooting

### Can't see the application?

```bash
# Check if app is running
docker-compose logs app

# Restart app
docker-compose restart app
```

### UniFi connection errors?

1. Verify you can access the UniFi Controller from the same machine
2. Check SSL settings: Set `UNIFI_IGNORE_SSL=true` in `.env`
3. Ensure the switch MAC address is correct
4. Verify your credentials work by logging into the UniFi web UI

### No cameras showing up?

```bash
# Check Azurite is running
docker-compose ps azurite

# Check app logs for table storage errors
docker-compose logs app | grep -i table

# Reset the database (WARNING: Deletes all data)
docker-compose down -v
docker-compose up -d
```

## Next Steps

- **Deploy to Mac Mini**: See [Deployment Guide](DEPLOYMENT.md)
- **Configure auto-start**: See Docker documentation for running on boot


- Check the logs: `docker-compose logs -f`
- Review the [README.md](README.md) for detailed documentation
- Open an issue on GitHub if you encounter problems

## Success!

If you can see the frontend and toggle cameras, you're all set! 🎉

The system will:
- Auto-refresh port status every 5 seconds
- Show real-time updates when you toggle ports
- Remember port configurations between restarts
