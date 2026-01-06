# Redis Setup for Token Cache Persistence

This document explains how to set up and use Redis for persisting authentication tokens across application restarts in the Budget.Web Blazor Server application.

## Overview

Redis is used for **local development only** to provide fast token caching:

- **Local Development**: Redis runs in Docker container (via docker-compose.yml)
- **Azure Deployment**: SQL Server distributed cache (no Redis provisioned)
- **Connection**: Configured in `appsettings.Development.json`

**Important:** Redis is NOT managed by Aspire AppHost. It runs independently via Docker Compose.

## Problem Solved

Without Redis, authentication tokens are stored in memory and lost when the app restarts. This means:
- ? After stopping debugging and restarting, users must log out and back in
- ? Tokens don't survive app restarts in production
- ? Browser cookies remain but token cache is empty ? 401 errors

With Redis:
- ? Tokens persist across app restarts
- ? No need to log out/in after restarting during development
- ? Production-ready token persistence

## Prerequisites

- Docker Desktop installed and running ([Download](https://www.docker.com/products/docker-desktop))

## Quick Start

### 1. Start Redis

From the repository root directory, run:

```bash
docker-compose up -d
```

This starts Redis in the background. You only need to do this once - Redis will start automatically with Docker Desktop.

### 2. Verify Redis is Running

```bash
docker ps
```

You should see a container named `budget-redis` running.

### 3. Test the Connection

```bash
docker exec -it budget-redis redis-cli ping
```

Should respond with: `PONG`

### 4. Start Your Application

Start debugging normally. Tokens will now persist in Redis!

## Testing Token Persistence

1. **Clear browser cookies** (to start fresh)
2. **Start debugging** (F5)
3. **Log in** to your app
4. **Navigate to Maintenance** and click "Backup All Tables" - should work ?
5. **Stop debugging** (Shift+F5)
6. **Start debugging again** (F5) - do NOT clear cookies
7. **Navigate to Maintenance** and click "Backup All Tables" - should still work! ?

## Configuration

### Default Configuration

By default, the app connects to Redis at `localhost:6379` (the Docker Compose default).

### Custom Configuration

To use a different Redis server, add to your `appsettings.json` or user secrets:

```json
{
  "ConnectionStrings": {
    "Redis": "your-redis-server:6379"
  }
}
```

### Azure Redis Cache (Production)

For production using Azure Redis Cache:

1. Create an Azure Redis Cache instance
2. Get the connection string from the Azure Portal
3. Add to your production configuration:

```json
{
  "ConnectionStrings": {
    "Redis": "your-redis-name.redis.cache.windows.net:6380,password=your-key,ssl=True,abortConnect=False"
  }
}
```

## Managing Redis

### View Cached Tokens

```bash
docker exec -it budget-redis redis-cli
keys *
```

### Clear All Tokens

```bash
docker exec -it budget-redis redis-cli FLUSHDB
```

### Stop Redis

```bash
docker-compose down
```

### Stop and Remove Data

```bash
docker-compose down -v
```

## Troubleshooting

### Redis Not Starting

1. Check Docker Desktop is running
2. Check port 6379 is not in use:
   ```bash
   netstat -ano | findstr :6379
   ```
3. View Redis logs:
   ```bash
   docker logs budget-redis
   ```

### Still Getting 401 Errors

1. Clear browser cookies completely
2. Stop the app
3. Clear Redis:
   ```bash
   docker exec -it budget-redis redis-cli FLUSHDB
   ```
4. Restart the app and log in fresh

### "Cannot connect to Redis" Error

1. Verify Redis is running:
   ```bash
   docker ps
   ```
2. If not running:
   ```bash
   docker-compose up -d
   ```
3. Test connection:
   ```bash
   docker exec -it budget-redis redis-cli ping
   ```

## How It Works

1. **User logs in** ? Entra ID issues access token
2. **Token acquired** ? Stored in Redis with key `BudgetApp:{sessionId}`
3. **App restarts** ? Redis data persists
4. **User returns** ? Browser sends session cookie ? Token retrieved from Redis ? API calls work!

## Architecture

```
???????????????         ????????????????         ??????????????
?             ? Login   ?              ? Token   ?            ?
?  Browser    ?????????>?  Budget.Web  ?????????>? Entra ID   ?
?             ?         ? (Blazor)     ?         ?            ?
???????????????         ????????????????         ??????????????
      ?                        ?
      ? Session Cookie         ? Store Token
      ?                        v
      ?                 ????????????????
      ?                 ?              ?
      ?????????????????>?    Redis     ?
        Future Requests ? (Token Cache)?
                        ????????????????
```

## Files Modified

- `docker-compose.yml` - Redis container configuration
- `Budget.Web/Startup/ConfigureServices.cs` - Redis cache registration
- `Budget.Web/Startup/ConfigureIdentity.cs` - Distributed token cache configuration
- `Budget.Web/Budget.Web.csproj` - Added StackExchange.Redis package

## Additional Resources

- [Microsoft Identity Web Token Cache](https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization)
- [StackExchange.Redis Documentation](https://stackexchange.github.io/StackExchange.Redis/)
- [Azure Redis Cache](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/)
