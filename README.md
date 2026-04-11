# Device Management System

A full-stack application for managing devices built with .NET 9, MongoDB, Angular, and Docker.

## Project Overview

The Device Management System is a web application that allows users to:
- **Register and authenticate** with secure login/registration
- **Manage devices** - create, read, update, and delete device records
- **Organize users** - manage user profiles and permissions
- **Store data** - persist all information in MongoDB

### System Architecture

```
┌─────────────────────────────────────────────────────────┐
│           Angular Frontend (Port 4200)                   │
│       DeviceManagement.UI (Standalone App)              │
└──────────────────┬──────────────────────────────────────┘
                   │
                   │ HTTP/REST API
                   │
┌──────────────────▼──────────────────────────────────────┐
│         .NET 9 API Backend (Port 5000)                   │
│    DeviceManagement.Api (ASP.NET Core)                  │
│  ├── Controllers (Auth, Users, Devices)                 │
│  ├── Services (Business Logic)                           │
│  └── Models (Data Structures)                            │
└──────────────────┬──────────────────────────────────────┘
                   │
                   │ MongoDB Driver
                   │
┌──────────────────▼──────────────────────────────────────┐
│         MongoDB Database (Port 27017)                    │
│     (Running in Docker Container)                       │
└─────────────────────────────────────────────────────────┘
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 9
- **Runtime**: .NET 9
- **Database**: MongoDB
- **Language**: C#

### Frontend
- **Framework**: Angular 21
- **Language**: TypeScript
- **Styling**: CSS
- **Build Tool**: Angular CLI

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Package Manager**: npm (Frontend), NuGet (Backend)
- **Testing**: xUnit (Backend), Vitest (Frontend)

## Prerequisites

Before running the project, ensure you have installed:

- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Node.js** (v18+) - [Download](https://nodejs.org/)
- **.NET 9 SDK** (optional, for local development) - [Download](https://dotnet.microsoft.com/download)

## Quick Start (Docker - Recommended)

### 1. Build and Start Services

```bash
# Navigate to project root
cd DeviceManagement

# Build and start all services (API, Frontend, MongoDB)
docker compose up --build
```

### 2. Access the Application

- **Frontend**: http://localhost:4200
- **API**: http://localhost:5000
- **MongoDB**: localhost:27017 (internal only)

### 3. Stop Services

```bash
docker compose down
```

---

## Local Development Setup

If you want to run components locally without Docker:

### Backend Setup (.NET API)

#### Prerequisites
- .NET 9 SDK installed
- MongoDB running (locally or via Docker)

#### Steps

1. **Start MongoDB only** (if not running locally):
   ```bash
   docker run -d -p 27017:27017 --name mongodb mongo:latest
   ```

2. **Navigate to API folder**:
   ```bash
   cd DeviceManagement.Api
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Run the API**:
   ```bash
   dotnet run
   ```

   The API will start on `https://localhost:5001` and `http://localhost:5000`

### Frontend Setup (Angular)

#### Prerequisites
- Node.js v18+ installed
- API running (either locally or via Docker)

#### Steps

1. **Navigate to UI folder**:
   ```bash
   cd DeviceManagement.UI
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Start development server**:
   ```bash
   npm start
   ```

   The app will open automatically at `http://localhost:4200`

---

## Project Structure

```
DeviceManagement/
├── docker-compose.yml              # Orchestrates all services
├── DBSetup/
│   └── init-mongo.js               # MongoDB initialization script
├── DeviceManagement.Api/           # Backend API (.NET 9)
│   ├── Program.cs                  # Application entry point
│   ├── appsettings.json            # Configuration
│   ├── Controllers/                # API endpoints
│   │   ├── AuthController.cs       # Login/Register
│   │   ├── DevicesController.cs    # Device CRUD
│   │   └── UsersController.cs      # User management
│   ├── Services/                   # Business logic
│   │   ├── AuthService.cs
│   │   ├── DeviceService.cs
│   │   ├── UserService.cs
│   │   └── MongoDbService.cs       # Database connection
│   ├── Models/                     # Data models
│   │   ├── Device.cs
│   │   ├── User.cs
│   │   └── Auth/
│   │       ├── LoginRequest.cs
│   │       ├── LoginResponse.cs
│   │       └── RegisterRequest.cs
│   └── Dockerfile                  # API container definition
├── DeviceManagement.UI/            # Frontend (Angular)
│   ├── src/
│   │   ├── main.ts                 # Application bootstrap
│   │   ├── server.ts               # SSR server
│   │   ├── app/
│   │   │   ├── app.routes.ts       # Routing configuration
│   │   │   ├── components/         # Reusable components
│   │   │   ├── services/           # HTTP & business logic
│   │   │   ├── guards/             # Route guards (auth)
│   │   │   └── interceptors/       # HTTP interceptors
│   │   └── environments/           # Environment configs
│   ├── package.json                # npm dependencies
│   ├── angular.json                # Angular build config
│   ├── tsconfig.json               # TypeScript config
│   └── Dockerfile                  # Frontend container definition
├── DeviceManagement.Tests/         # Backend integration tests
│   ├── CustomWebApplicationFactory.cs
│   ├── DevicesControllerTests.cs
│   ├── UsersControllerTests.cs
│   └── IntegrationTestCollection.cs
└── DeviceManagement.sln            # Visual Studio solution file
```

---

## How the Application Works

### 1. Authentication Flow

```
User → [Login/Register] → AuthController → AuthService → MongoDB
                                              ↓
                                    JWT Token Generated
                     ↓
        Angular stores token in localStorage
                     ↓
        Token sent with every API request via interceptor
```

### 2. Device Management Flow

```
Frontend (Angular)
    ↓ (HTTP Request with Auth Token)
DevicesController (API)
    ↓
DeviceService (Business Logic)
    ↓
MongoDbService (Database Access)
    ↓
MongoDB (Storage)
    ↓ (Response with Device Data)
Frontend (Display & Update UI)
```

### 3. User Management

- Users register with email and password
- Credentials are validated and stored securely in MongoDB
- Upon login, a JWT token is issued
- Token is used to authorize subsequent API requests
- User can manage devices only when authenticated

---

## Available Scripts

### Frontend
```bash
npm start          # Start dev server (http://localhost:4200)
npm build          # Build for production
npm test           # Run unit tests
npm run watch      # Build with watch mode
```

### Backend
```bash
dotnet run                    # Start development server
dotnet build                  # Build the project
dotnet test                   # Run tests
dotnet publish -c Release     # Create production build
```

### Docker
```bash
docker compose up             # Start all services
docker compose up --build     # Rebuild and start
docker compose down           # Stop all services
docker compose logs -f        # Follow logs
```

---

## Environment Configuration

### Backend (`DeviceManagement.Api/appsettings.json`)
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://mongodb:27017",
    "DatabaseName": "DeviceManagement"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "DeviceManagementAPI",
    "Audience": "DeviceManagementApp",
    "ExpirationMinutes": 60
  }
}
```

### Frontend (`DeviceManagement.UI/src/environments/environment.ts`)
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

---

## Running Tests

### Backend Integration Tests
```bash
cd DeviceManagement.Tests
dotnet test
```

### Frontend Unit Tests
```bash
cd DeviceManagement.UI
npm test
```

---

## Troubleshooting

### MongoDB Connection Failed
- Ensure Docker is running: `docker ps`
- Restart Docker: `docker compose down && docker compose up`

### Port Already in Use
- API (5000/5001): Kill process or change port in `appsettings.json`
- Frontend (4200): Kill process or use `ng serve --port 4300`
- MongoDB (27017): Change port in `docker-compose.yml`

### Dependencies Not Installing
```bash
# Angular
npm cache clean --force
rm -rf node_modules package-lock.json
npm install

# .NET
dotnet nuget locals all --clear
dotnet restore
```

### CORS Issues
- Ensure API is running
- Check `Program.cs` for CORS policy configuration
- Verify API URL in Angular environment config

---

## Deployment

### Docker Hub Registry
```bash
# Build and push API
docker build -t your-registry/device-management-api:latest ./DeviceManagement.Api
docker push your-registry/device-management-api:latest

# Build and push Frontend
docker build -t your-registry/device-management-ui:latest ./DeviceManagement.UI
docker push your-registry/device-management-ui:latest
```

### Production Environment
For production deployment, update:
1. `appsettings.Production.json` (Backend)
2. `environment.prod.ts` (Frontend)
3. Docker Compose or Kubernetes configurations

---

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Commit changes: `git commit -m 'Add feature'`
3. Push to branch: `git push origin feature/your-feature`
4. Submit a pull request

---

## Support

For issues or questions:
1. Check this README
2. Review application logs: `docker compose logs`
3. Check GitHub issues
4. Contact the development team

---

**Last Updated**: April 2026
**Version**: 1.0.0
