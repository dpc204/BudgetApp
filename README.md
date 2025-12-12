# FantumBudget

A modern budgeting application built with Blazor .NET 10 and .NET Aspire.

## Architecture

- **Frontend**: Blazor Web App (Budget.Web)
- **Backend API**: ASP.NET Core Minimal API with MediatR and Carter (Budget.Api)
- **Database**: SQL Server with Entity Framework Core
- **Hosting**: Azure Container Apps with .NET Aspire

## Authentication

FantumBudget is migrating from ASP.NET Core Identity to **Microsoft Entra ID** for enterprise authentication and authorization.

### Authentication Documentation

- **[Authentication Migration Plan](Documentation/Authentication-Migration-Plan.md)** - Complete migration roadmap
- **[Phase 1: Entra ID Setup Guide](Documentation/Phase1-EntraID-Setup.md)** - Step-by-step setup instructions
- **[Configuration Template](Documentation/entra-config-template.json)** - Configuration reference
- **[Setup Script](scripts/Setup-EntraApp.ps1)** - PowerShell automation for Entra app registration
- **[Troubleshooting Azure Authentication](Documentation/Troubleshooting-Azure-Authentication.md)** - Fix common authentication errors

### Quick Start - Authentication Setup

1. Run the setup script to create Entra app registration:
   ```powershell
   cd scripts
   .\Setup-EntraApp.ps1
   ```

2. Follow the output instructions to configure your appsettings.json

3. See [Phase1-EntraID-Setup.md](Documentation/Phase1-EntraID-Setup.md) for detailed instructions

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB for development)
- Azure CLI (for deployment)
- Visual Studio 2022 or VS Code

### Local Development

1. Clone the repository:
   ```bash
   git clone https://github.com/dpc204/BudgetApp.git
   cd BudgetApp
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run with .NET Aspire:
   ```bash
   dotnet run --project Budget.AppHost
   ```

4. Access the application:
   - Web App: https://localhost:7141
   - API: http://localhost:5146
   - Aspire Dashboard: https://localhost:17214

## Project Structure

```
BudgetApp/
├── Budget.Web/           # Blazor Web App
├── Budget.Client/        # Blazor WebAssembly components
├── Budget.Api/           # Backend API with MediatR
├── Budget.DB/            # Entity Framework Core DbContext
├── Budget.Shared/        # Shared models and utilities
├── Budget.AppHost/       # .NET Aspire orchestration
├── ServiceDefaults/      # Shared service configurations
├── Documentation/        # Project documentation
└── scripts/             # Automation scripts
```

## Azure Deployment

See the following documentation for Azure deployment:

- [Azure Deployment Configuration](Documentation/AZURE_DEPLOYMENT_CONFIG.md)
- [Azure Environment Detection](Documentation/AZURE_ENVIRONMENT_DETECTION.md)
- [Container Apps Plan Types](Documentation/CONTAINER_APPS_PLAN_TYPES.md)

### Deploy to Azure

1. Deploy the application:
   ```bash
   azd up
   ```

2. After deployment completes, add the redirect URI for authentication:
   ```powershell
   cd scripts
   .\Add-RedirectUri.ps1 -RedirectUri "https://YOUR-APP-URL.azurecontainerapps.io/signin-oidc"
   ```
   Replace `YOUR-APP-URL` with the Container Apps URL from the deployment output.

3. For troubleshooting authentication issues, see:
   - [Troubleshooting Azure Authentication](Documentation/Troubleshooting-Azure-Authentication.md)
   - [Scripts README](scripts/README.md)

## Documentation

- [Architecture Overview](Architecture.md)
- [Authentication Migration Plan](Documentation/Authentication-Migration-Plan.md)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Next Steps](Documentation/next-steps.md)

## Features

- Budget tracking and management
- Category-based expense organization
- Real-time data updates
- Role-based access control (Admin, PowerUser, User)
- Azure SQL backup functionality
- Responsive UI with MudBlazor

## Technologies

- **Framework**: .NET 10
- **Frontend**: Blazor Web App, MudBlazor
- **Backend**: ASP.NET Core Minimal API
- **Patterns**: MediatR (CQRS), Carter (Endpoint routing)
- **Database**: Entity Framework Core, SQL Server
- **Cloud**: Azure Container Apps, .NET Aspire
- **Authentication**: Microsoft Entra ID (Azure AD)

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Create an issue in the GitHub repository
- See documentation in the `/Documentation` folder
- Check the [troubleshooting guide](Documentation/Phase1-EntraID-Setup.md#troubleshooting)
