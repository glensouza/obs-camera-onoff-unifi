# API Testing Guide

This guide explains how to test the UniFi Camera Control API using Swagger UI and other tools.

## Accessing Swagger UI

Once the application is running, access Swagger UI at:

**Local**: http://localhost:7071/api/swagger/ui
**Network**: http://YOUR_MAC_MINI_IP:7071/api/swagger/ui

## Available Endpoints

### 1. Get All Ports Status

**GET** `/api/ports/status`

Returns the status of all configured camera ports.

#### Using Swagger UI:
1. Click on **GET /api/ports/status**
2. Click **Try it out**
3. Click **Execute**

#### Expected Response (200 OK):
```json
[
  {
    "portNumber": 1,
    "portName": "Camera 1",
    "isEnabled": true,
    "poeEnabled": true
  },
  {
    "portNumber": 2,
    "portName": "Camera 2",
    "isEnabled": true,
    "poeEnabled": false
  },
  {
    "portNumber": 3,
    "portName": "Camera 3",
    "isEnabled": false,
    "poeEnabled": false
  }
]
```

#### Using curl:
```bash
curl http://localhost:7071/api/ports/status
```

---

### 2. Get Single Port Status

**GET** `/api/ports/{portNumber}/status`

Returns the status of a specific port.

#### Using Swagger UI:
1. Click on **GET /api/ports/{portNumber}/status**
2. Click **Try it out**
3. Enter port number (1, 2, or 3) in the `portNumber` field
4. Click **Execute**

#### Expected Response (200 OK):
```json
{
  "portNumber": 1,
  "portName": "Camera 1",
  "isEnabled": true,
  "poeEnabled": true
}
```

#### Error Response (404 Not Found):
```json
{
  "error": "Port 5 not found"
}
```

#### Using curl:
```bash
curl http://localhost:7071/api/ports/1/status
```

---

### 3. Set Port State (Turn On/Off)

**POST** `/api/ports/{portNumber}/state`

Controls the PoE power state of a camera port.

#### Using Swagger UI:
1. Click on **POST /api/ports/{portNumber}/state**
2. Click **Try it out**
3. Enter port number in the `portNumber` field
4. Edit the request body:
   ```json
   {
     "portNumber": 1,
     "enable": true
   }
   ```
   - Set `enable` to `true` to turn on
   - Set `enable` to `false` to turn off
5. Click **Execute**

#### Request Body:
```json
{
  "portNumber": 1,
  "enable": true
}
```

#### Expected Response (200 OK):
```json
{
  "success": true,
  "message": "Port 1 enabled"
}
```

#### Error Response (500 Internal Server Error):
```json
{
  "success": false,
  "error": "Failed to set port state"
}
```

#### Using curl to turn ON:
```bash
curl -X POST http://localhost:7071/api/ports/1/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 1, "enable": true}'
```

#### Using curl to turn OFF:
```bash
curl -X POST http://localhost:7071/api/ports/2/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 2, "enable": false}'
```

---

## Testing Scenarios

### Scenario 1: Turn on all cameras before livestream

```bash
# Turn on Camera 1
curl -X POST http://localhost:7071/api/ports/1/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 1, "enable": true}'

# Turn on Camera 2
curl -X POST http://localhost:7071/api/ports/2/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 2, "enable": true}'

# Turn on Camera 3
curl -X POST http://localhost:7071/api/ports/3/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 3, "enable": true}'

# Wait a moment for cameras to power on
sleep 5

# Verify all are on
curl http://localhost:7071/api/ports/status
```

### Scenario 2: Turn off all cameras after livestream

```bash
# Turn off all cameras
for port in 1 2 3; do
  curl -X POST http://localhost:7071/api/ports/$port/state \
    -H "Content-Type: application/json" \
    -d "{\"portNumber\": $port, \"enable\": false}"
done

# Verify all are off
curl http://localhost:7071/api/ports/status
```

### Scenario 3: Toggle a single camera

```bash
# Get current status
curl http://localhost:7071/api/ports/1/status

# Turn off
curl -X POST http://localhost:7071/api/ports/1/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 1, "enable": false}'

# Wait
sleep 3

# Turn back on
curl -X POST http://localhost:7071/api/ports/1/state \
  -H "Content-Type: application/json" \
  -d '{"portNumber": 1, "enable": true}'
```

---

## Testing with Postman

### Import Collection

1. Open Postman
2. Click **Import**
3. Create a new collection
4. Add the following requests:

#### Request 1: Get All Ports
- Method: GET
- URL: `http://localhost:7071/api/ports/status`

#### Request 2: Get Port Status
- Method: GET
- URL: `http://localhost:7071/api/ports/1/status`
- Variables: `portNumber` = 1

#### Request 3: Turn On Port
- Method: POST
- URL: `http://localhost:7071/api/ports/1/state`
- Headers: `Content-Type: application/json`
- Body (raw JSON):
  ```json
  {
    "portNumber": 1,
    "enable": true
  }
  ```

#### Request 4: Turn Off Port
- Method: POST
- URL: `http://localhost:7071/api/ports/1/state`
- Headers: `Content-Type: application/json`
- Body (raw JSON):
  ```json
  {
    "portNumber": 1,
    "enable": false
  }
  ```

---

## Testing with JavaScript

```javascript
// Get all ports status
async function getAllPorts() {
  const response = await fetch('http://localhost:7071/api/ports/status');
  const data = await response.json();
  console.log('All ports:', data);
}

// Get single port status
async function getPortStatus(portNumber) {
  const response = await fetch(`http://localhost:7071/api/ports/${portNumber}/status`);
  const data = await response.json();
  console.log(`Port ${portNumber}:`, data);
}

// Turn port on
async function turnPortOn(portNumber) {
  const response = await fetch(`http://localhost:7071/api/ports/${portNumber}/state`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ portNumber, enable: true })
  });
  const data = await response.json();
  console.log('Result:', data);
}

// Turn port off
async function turnPortOff(portNumber) {
  const response = await fetch(`http://localhost:7071/api/ports/${portNumber}/state`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ portNumber, enable: false })
  });
  const data = await response.json();
  console.log('Result:', data);
}

// Usage
getAllPorts();
getPortStatus(1);
turnPortOn(1);
turnPortOff(2);
```

---

## Troubleshooting API Issues

### API Returns 404

**Problem**: Endpoint not found

**Solutions**:
1. Verify the URL is correct
2. Check backend is running: `docker-compose ps backend`
3. View backend logs: `docker-compose logs backend`
4. Restart backend: `docker-compose restart backend`

### API Returns 500

**Problem**: Internal server error

**Common Causes**:
1. Can't connect to UniFi Controller
   - Check `UNIFI_HOST` is correct
   - Verify network connectivity
   - Check credentials are valid

2. Can't connect to Azurite
   - Check Azurite is running: `docker-compose ps azurite`
   - Restart Azurite: `docker-compose restart azurite`

3. Invalid port number
   - Only ports 1, 2, and 3 are configured by default

**Solution**:
Check the backend logs for specific error messages:
```bash
docker-compose logs backend | tail -50
```

### CORS Errors in Browser

**Problem**: CORS policy blocking requests

**Solution**:
The API already includes CORS headers. If you still see errors:
1. Check you're using the correct URL
2. Verify the backend is running
3. Check browser console for specific error
4. Try using curl instead to isolate the issue

### Connection Refused

**Problem**: Can't connect to localhost:7071

**Solutions**:
1. Verify backend is running: `docker ps | grep backend`
2. Check port mapping: `docker-compose ps`
3. Try using `127.0.0.1` instead of `localhost`
4. Check firewall settings
5. Restart Docker Desktop

### Slow Response Times

**Problem**: API takes a long time to respond

**Causes**:
1. UniFi Controller is slow or unreachable
2. Network latency
3. First request after startup (authentication)

**Solutions**:
1. Check network connectivity to UniFi Controller
2. Monitor backend logs for delays
3. Subsequent requests should be faster (uses cached auth)

---

## Performance Testing

### Load Test with curl

```bash
# Send 100 requests to get all ports
for i in {1..100}; do
  curl -s http://localhost:7071/api/ports/status > /dev/null &
done
wait

# Send mixed requests
for i in {1..50}; do
  port=$((RANDOM % 3 + 1))
  curl -s http://localhost:7071/api/ports/$port/status > /dev/null &
done
wait
```

### Monitor Response Times

```bash
# Test response time
time curl http://localhost:7071/api/ports/status

# Verbose output
curl -w "\nTotal time: %{time_total}s\n" http://localhost:7071/api/ports/status
```

---

## Best Practices

1. **Check Status Before Toggling**: Always get current status before changing state
2. **Wait Between Toggles**: Allow 2-3 seconds between turning a port off and on
3. **Handle Errors Gracefully**: Always check response status and handle errors
4. **Use Appropriate HTTP Methods**: GET for reading, POST for changing state
5. **Log API Calls**: Keep track of when ports are toggled for debugging
6. **Monitor Backend Health**: Regularly check backend logs for issues

---

## Next Steps

- Automate camera control with scripts
- Integrate with streaming software
- Create custom dashboards
- Set up monitoring and alerts
- Build mobile app integration
