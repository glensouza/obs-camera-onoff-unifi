// Configuration - Update this with your backend URL
const API_BASE_URL = '/api'; // Using nginx proxy

// Auto-refresh interval (in milliseconds)
// Increased to 20 seconds to reduce network traffic during long livestream sessions
const REFRESH_INTERVAL = 20000; // 20 seconds

let refreshTimer = null;
let isUpdating = false; // Flag to prevent concurrent updates

// Show status message
function showStatus(message, type = 'info') {
    const statusElement = document.getElementById('status-message');
    statusElement.textContent = message;
    statusElement.className = `status-message ${type}`;
    
    // Auto-hide after 3 seconds for success/info messages
    if (type !== 'error') {
        setTimeout(() => {
            statusElement.textContent = '';
            statusElement.className = 'status-message';
        }, 3000);
    }
}

// Fetch port status
async function fetchPortStatus(portNumber) {
    try {
        const response = await fetch(`${API_BASE_URL}/ports/${portNumber}/status`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    } catch (error) {
        console.error(`Error fetching port ${portNumber} status:`, error);
        showStatus(`Error fetching port ${portNumber} status: ${error.message}`, 'error');
        return null;
    }
}

// Fetch all ports status
async function fetchAllPortsStatus() {
    try {
        const response = await fetch(`${API_BASE_URL}/ports/status`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    } catch (error) {
        console.error('Error fetching all ports status:', error);
        showStatus(`Error fetching ports status: ${error.message}`, 'error');
        return [];
    }
}

// Set port state
async function setPortState(portNumber, enable) {
    try {
        const response = await fetch(`${API_BASE_URL}/ports/${portNumber}/state`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ portNumber, enable })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        
        if (result.success) {
            showStatus(result.message, 'success');
            // Refresh status after a short delay
            setTimeout(() => updatePortStatus(portNumber), 1000);
        } else {
            showStatus(result.error || 'Failed to update port state', 'error');
        }

        return result;
    } catch (error) {
        console.error('Error setting port state:', error);
        showStatus(`Error setting port state: ${error.message}`, 'error');
        return { success: false, error: error.message };
    }
}

// Create camera card HTML
function createCameraCard(portStatus) {
    const card = document.createElement('div');
    card.className = `camera-card ${portStatus.poeEnabled ? 'active' : 'inactive'}`;
    card.id = `camera-${portStatus.portNumber}`;

    card.innerHTML = `
        <div class="camera-header">
            <div class="camera-name">${portStatus.portName}</div>
            <div class="camera-status">
                <div class="status-indicator ${portStatus.poeEnabled ? 'on' : 'off'}"></div>
                <span class="status-text">${portStatus.poeEnabled ? 'ON' : 'OFF'}</span>
            </div>
        </div>
        
        <div class="camera-info">
            <div class="info-row">
                <span class="info-label">Port Number:</span>
                <span class="info-value">${portStatus.portNumber}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Link Status:</span>
                <span class="info-value">${portStatus.isEnabled ? 'Connected' : 'Disconnected'}</span>
            </div>
            <div class="info-row">
                <span class="info-label">PoE Status:</span>
                <span class="info-value">${portStatus.poeEnabled ? 'Enabled' : 'Disabled'}</span>
            </div>
        </div>
        
        <div class="camera-controls">
            <label class="toggle-switch">
                <input type="checkbox" 
                       ${portStatus.poeEnabled ? 'checked' : ''} 
                       data-port="${portStatus.portNumber}"
                       id="toggle-${portStatus.portNumber}">
                <span class="slider"></span>
            </label>
            <span style="flex: 1; display: flex; align-items: center; padding-left: 10px;">
                ${portStatus.poeEnabled ? 'Turn Off' : 'Turn On'}
            </span>
        </div>
    `;

    return card;
}

// Update single port status
async function updatePortStatus(portNumber) {
    const status = await fetchPortStatus(portNumber);
    if (status) {
        const card = document.getElementById(`camera-${portNumber}`);
        if (card) {
            const newCard = createCameraCard(status);
            card.replaceWith(newCard);
        }
    }
}

// Update all ports status
async function updateAllPortsStatus() {
    // Prevent concurrent executions
    if (isUpdating) {
        console.log('Update already in progress, skipping...');
        return;
    }

    isUpdating = true;
    const container = document.getElementById('camera-controls');
    
    try {
        // Show loading state only if container is empty
        if (container.children.length === 0) {
            container.innerHTML = '<div class="loading">Loading camera status</div>';
        }
        
        const statuses = await fetchAllPortsStatus();
        
        if (statuses.length === 0) {
            container.innerHTML = '<div class="loading">No cameras configured or error loading data</div>';
            return;
        }

        // Clear container
        container.innerHTML = '';
        
        // Sort by port number
        statuses.sort((a, b) => a.portNumber - b.portNumber);
        
        // Create cards for each port
        statuses.forEach(status => {
            const card = createCameraCard(status);
            container.appendChild(card);
        });
    } finally {
        isUpdating = false;
    }
}

// Handle toggle switch
async function handleToggle(portNumber, enable) {
    const toggle = document.getElementById(`toggle-${portNumber}`);
    toggle.disabled = true;
    
    showStatus(`${enable ? 'Enabling' : 'Disabling'} port ${portNumber}...`, 'info');
    
    try {
        const result = await setPortState(portNumber, enable);
        
        if (!result || !result.success) {
            // Revert toggle if failed
            toggle.checked = !enable;
        }
    } catch (error) {
        // On error, revert the toggle
        toggle.checked = !enable;
        showStatus(`Failed to ${enable ? 'enable' : 'disable'} port ${portNumber}.`, 'error');
    } finally {
        toggle.disabled = false;
        // Always refresh status to ensure UI accuracy
        await updateAllPortsStatus();
    }
}

// Start auto-refresh
function startAutoRefresh() {
    if (refreshTimer) {
        clearInterval(refreshTimer);
    }
    
    refreshTimer = setInterval(() => {
        updateAllPortsStatus();
    }, REFRESH_INTERVAL);
}

// Stop auto-refresh
function stopAutoRefresh() {
    if (refreshTimer) {
        clearInterval(refreshTimer);
        refreshTimer = null;
    }
}

// Initialize the app
async function init() {
    showStatus('Loading camera controls...', 'info');
    await updateAllPortsStatus();
    startAutoRefresh();
    
    // Setup refresh button
    document.getElementById('refresh-all').addEventListener('click', () => {
        showStatus('Refreshing...', 'info');
        updateAllPortsStatus();
    });

    // Use event delegation for toggle switches instead of inline handlers
    document.getElementById('camera-controls').addEventListener('change', (event) => {
        if (event.target.type === 'checkbox' && event.target.dataset.port) {
            const portNumber = parseInt(event.target.dataset.port, 10);
            const enable = event.target.checked;
            handleToggle(portNumber, enable);
        }
    });
}

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    stopAutoRefresh();
});

// Start the app when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}
