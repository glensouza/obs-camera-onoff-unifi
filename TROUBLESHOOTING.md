# Troubleshooting Guide

Common issues and solutions for the UniFi Camera Control System.

## Table of Contents
- [Installation Issues](#installation-issues)
- [Backend Issues](#backend-issues)
- [Frontend Issues](#frontend-issues)
- [UniFi Connection Issues](#unifi-connection-issues)
- [Docker Issues](#docker-issues)
- [OBS Integration Issues](#obs-integration-issues)
- [Deployment Issues](#deployment-issues)

---

## Installation Issues

### Docker Compose fails to start

**Error**: `Cannot connect to the Docker daemon`

**Solution**:
```bash
# Check if Docker Desktop is running
docker ps

# If not running, start Docker Desktop
# On Mac: Open Docker Desktop from Applications
# On Windows: Start Docker Desktop from Start Menu

# Verify Docker is running
docker --version
docker-compose --version
```

### Port conflicts

**Error**: `port is already allocated`

**Solution**:
```bash
# Find what's using the port
sudo lsof -i :8080
sudo lsof -i :7071

# Option 1: Stop the conflicting service
# Option 2: Change ports in docker-compose.yml
# Change "8080:80" to "8081:80" for frontend
# Change "7071:80" to "7072:80" for backend
```

---

## Backend Issues

### Backend container keeps restarting

**Check logs**:
```bash
docker-compose logs backend
```

**Common causes**:

1. **Can't connect to Azurite**
   ```bash
   # Verify Azurite is running
   docker-compose ps azurite
   
   # Restart Azurite
   docker-compose restart azurite
   
   # Check Azurite logs
   docker-compose logs azurite
   ```

2. **Configuration error**
   ```bash
   # Check .env file exists and is correct
   cat .env
   
   # Verify all required variables are set
   grep UNIFI_ .env
   ```

3. **Build failed**
   ```bash
   # Rebuild the backend
   docker-compose build backend
   
   # Check for build errors
   docker-compose up --build backend
   ```

### API returns 500 errors

**Check backend logs**:
```bash
docker-compose logs backend | grep -i error
```

**Common solutions**:

1. **UniFi connection error**
   - Verify UNIFI_HOST is reachable
   - Check credentials are correct
   - Ensure SSL certificate issues are handled with `UNIFI_IGNORE_SSL=true`

2. **Table Storage error**
   - Restart Azurite: `docker-compose restart azurite`
   - Check connection string in logs
   - Reset storage: `docker-compose down -v && docker-compose up -d`

### Swagger UI not loading

**URL**: http://localhost:7071/api/swagger/ui

**Solutions**:
```bash
# 1. Check backend is running
docker-compose ps backend

# 2. Check backend logs for errors
docker-compose logs backend

# 3. Verify the port
curl http://localhost:7071/api/swagger/ui

# 4. Restart backend
docker-compose restart backend
```

---

## Frontend Issues

### Frontend shows blank page

**Solutions**:

1. **Check nginx is running**
   ```bash
   docker-compose ps frontend
   docker-compose logs frontend
   ```

2. **Check file permissions**
   ```bash
   ls -la src/frontend/
   # Files should be readable
   ```

3. **Check browser console**
   - Open browser DevTools (F12)
   - Look for JavaScript errors
   - Check Network tab for failed requests

### "No cameras configured" message

**Causes**:
- Backend is not running
- Backend is not reachable
- Port configurations not initialized

**Solutions**:
```bash
# 1. Verify backend is accessible
curl http://localhost:7071/api/ports/status

# 2. Check backend logs
docker-compose logs backend

# 3. Initialize configuration
# The backend should auto-initialize on first run
# If not, restart backend:
docker-compose restart backend
```

### Cameras not updating

**Symptoms**: Status doesn't change when toggling

**Solutions**:

1. **Check browser console for errors**
   - Open DevTools (F12)
   - Check Console and Network tabs

2. **Verify API is working**
   ```bash
   # Test get status
   curl http://localhost:7071/api/ports/status
   
   # Test set state
   curl -X POST http://localhost:7071/api/ports/1/state \
     -H "Content-Type: application/json" \
     -d '{"portNumber": 1, "enable": true}'
   ```

3. **Check CORS settings**
   - Backend should include CORS headers
   - Check Network tab in DevTools for CORS errors

---

## UniFi Connection Issues

### Can't connect to UniFi Controller

**Error in logs**: `Connection refused` or `SSL certificate error`

**Solutions**:

1. **Verify controller URL**
   ```bash
   # Test connection from the same machine
   curl -k https://YOUR_UNIFI_IP:8443
   
   # Should get HTML response
   ```

2. **Check credentials**
   - Log into UniFi web UI with the same credentials
   - Ensure user has admin access
   - Create a dedicated API user if needed

3. **SSL certificate issues**
   ```bash
   # In .env file, ensure:
   UNIFI_IGNORE_SSL=true
   
   # Restart backend
   docker-compose restart backend
   ```

4. **Network connectivity**
   ```bash
   # Ping the controller
   ping YOUR_UNIFI_IP
   
   # Check if port is open
   nc -zv YOUR_UNIFI_IP 8443
   ```

### Wrong switch MAC address

**Error**: Port commands don't work

**Solution**:
```bash
# Find your switch MAC address:
# 1. Log into UniFi Controller web UI
# 2. Go to Devices
# 3. Click on your switch
# 4. Copy the MAC address (format: aa:bb:cc:dd:ee:ff)

# Update .env
UNIFI_SWITCH_MAC=aa:bb:cc:dd:ee:ff

# Restart backend
docker-compose restart backend
```

### Authentication fails

**Error in logs**: `Authentication failed: Unauthorized`

**Solutions**:

1. **Check credentials**
   ```bash
   # Verify in .env
   cat .env | grep UNIFI_USERNAME
   cat .env | grep UNIFI_PASSWORD
   ```

2. **Test login manually**
   - Open UniFi Controller web UI
   - Try logging in with the same credentials
   - If it fails, reset password in UniFi

3. **Check user permissions**
   - User must be Super Administrator
   - Or create a dedicated API user with full access

---

## Docker Issues

### Container exited unexpectedly

```bash
# View container status
docker-compose ps

# Check specific container logs
docker-compose logs [service-name]

# Restart service
docker-compose restart [service-name]

# Rebuild and restart
docker-compose up -d --build [service-name]
```

### Volumes not persisting data

```bash
# List volumes
docker volume ls

# Inspect Azurite volume
docker volume inspect obs-camera-onoff-unifi_azurite-data

# Remove and recreate volumes (WARNING: Deletes all data)
docker-compose down -v
docker-compose up -d
```

### Out of disk space

```bash
# Check Docker disk usage
docker system df

# Clean up unused resources
docker system prune

# Remove all unused images, containers, volumes
docker system prune -a --volumes
```

---

## OBS Integration Issues

### Browser source shows "Loading..."

**Solutions**:

1. **Check URL is correct**
   - Should be: `http://localhost:8080`
   - Or: `http://YOUR_MAC_MINI_IP:8080`

2. **Verify frontend is accessible**
   ```bash
   curl http://localhost:8080
   ```

3. **Refresh browser source**
   - Right-click source → Interact
   - Or right-click source → Refresh

### Browser source is blank

**Solutions**:

1. **Check OBS browser source settings**
   - Width: 800-1920
   - Height: 600-1080
   - FPS: 30
   - Check "Shutdown source when not visible" is UNCHECKED

2. **Check OBS logs**
   - Help → Log Files → View Current Log
   - Look for browser source errors

3. **Test in regular browser first**
   - Open http://localhost:8080 in Chrome/Firefox
   - If it works there but not in OBS, it's an OBS issue

### Toggle switches don't work in OBS

**Solution**:
- Right-click the browser source
- Click **Interact**
- This opens an interactive window where clicks work
- Or set up hotkeys to trigger API calls directly

---

## Deployment Issues

### GitHub Actions workflow fails

**Check workflow logs**:
1. Go to repository on GitHub
2. Click Actions tab
3. Click on the failed workflow
4. Expand failed step to see error

**Common issues**:

1. **Runner offline**
   ```bash
   # On Mac Mini, check runner status
   cd ~/github-runners/actions-runner
   sudo ./svc.sh status
   
   # Restart runner
   sudo ./svc.sh restart
   ```

2. **Docker not running**
   ```bash
   # On Mac Mini
   docker ps
   
   # Start Docker Desktop if not running
   ```

3. **Secrets not configured**
   - Go to GitHub repo → Settings → Secrets
   - Verify all UNIFI_* secrets are set
   - Secrets must match exactly

### Self-hosted runner not connecting

```bash
# Check runner status
cd ~/github-runners/actions-runner
sudo ./svc.sh status

# View runner logs
tail -f _diag/*.log

# Restart runner
sudo ./svc.sh stop
sudo ./svc.sh start

# Reinstall if needed
sudo ./svc.sh uninstall
sudo ./svc.sh install
sudo ./svc.sh start
```

### Deployment succeeds but app not accessible

**Solutions**:

1. **Check containers on Mac Mini**
   ```bash
   # SSH into Mac Mini
   ssh user@mac-mini-ip
   
   # Check containers
   docker ps
   
   # View logs
   docker-compose logs
   ```

2. **Check Mac Mini IP address**
   ```bash
   ifconfig | grep "inet " | grep -v 127.0.0.1
   ```

3. **Check firewall**
   ```bash
   # Mac firewall might block ports
   # System Preferences → Security & Privacy → Firewall
   # Add Docker to allowed applications
   ```

---

## General Debugging

### Enable verbose logging

**Backend**:
Edit `src/backend/host.json`:
```json
{
  "version": "2.0",
  "logging": {
    "logLevel": {
      "default": "Debug"
    }
  }
}
```

**Frontend**:
Open browser DevTools console (F12) - all logs appear there

**Docker**:
```bash
# View all logs with timestamps
docker-compose logs -f --timestamps

# View last 100 lines
docker-compose logs --tail=100

# View specific service
docker-compose logs -f backend
```

### Reset everything

```bash
# Stop all containers
docker-compose down

# Remove volumes (deletes all data)
docker-compose down -v

# Remove all images
docker-compose down --rmi all

# Rebuild from scratch
docker-compose build --no-cache
docker-compose up -d
```

### Check connectivity

```bash
# From frontend to backend
docker-compose exec frontend wget -O- http://backend:80/api/ports/status

# From backend to Azurite
docker-compose exec backend ping azurite

# From backend to UniFi
docker-compose exec backend curl -k https://YOUR_UNIFI_IP:8443
```

---

## Getting Help

If none of these solutions work:

1. **Collect information**:
   ```bash
   # Save all logs
   docker-compose logs > debug.log
   
   # System info
   docker version >> debug.log
   docker-compose version >> debug.log
   uname -a >> debug.log
   ```

2. **Check existing issues**:
   - Search GitHub issues for similar problems

3. **Open a new issue**:
   - Include the debug.log
   - Describe what you've tried
   - Include error messages
   - Mention your OS and Docker version

4. **Community support**:
   - Check the README.md
   - Review the API_TESTING.md guide
   - Try the QUICKSTART.md steps again

---

## Prevention Tips

1. **Use stable versions**
   - Pin Docker image versions in docker-compose.yml
   - Keep Docker Desktop updated

2. **Regular backups**
   - Backup Azurite data volume
   - Save your .env file securely

3. **Monitor logs**
   - Check logs periodically: `docker-compose logs --tail=50`
   - Watch for warnings

4. **Test before production**
   - Always test locally first
   - Verify in a test environment
   - Have a rollback plan

5. **Documentation**
   - Keep notes of customizations
   - Document your specific setup
   - Update configuration examples
