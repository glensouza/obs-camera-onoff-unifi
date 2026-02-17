# UniFi Camera Control System - Project Summary

## Purpose
Control UniFi PoE switch ports to power PTZ Optics cameras on/off for church livestream events.

## Architecture

### Blazor Server App (.NET 10)
- **Technology**: Blazor Server with interactive server-side rendering
- **Framework**: .NET 10
- **Storage**: Azure Table Storage (Azurite for local dev)

### Infrastructure
- **Containerization**: Docker + Docker Compose
- **Database**: Azurite (Azure Storage Emulator)
- **Deployment**: GitHub Actions with self-hosted runner

## Quick Start

1. Clone and configure: copy `.env.example` to `.env`
2. Start services: `docker-compose up -d`
3. Access application at http://localhost:8080

## OBS Integration

1. Open OBS Studio
2. Window -> Docks -> Custom Browser Docks...
3. Add dock with URL: http://localhost:8080
4. Set width ~400, height ~800

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 |
| UI Framework | Blazor Server |
| Real-time | SignalR |
| Database | Azure Table Storage |
| Local Storage | Azurite |
| Container Runtime | Docker |
| Orchestration | Docker Compose |
| CI/CD | GitHub Actions |

## Documentation

| Guide | Purpose |
|-------|---------|
| README.md | Complete setup and usage |
| QUICKSTART.md | 10-minute setup guide |
| DEPLOYMENT.md | Mac Mini deployment with GitHub Actions |
| TROUBLESHOOTING.md | Common issues and solutions |

## License

MIT
