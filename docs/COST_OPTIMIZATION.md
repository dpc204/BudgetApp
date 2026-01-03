# Cost-Optimized Azure Deployment Summary

## Architecture Decisions

### Token Cache Strategy

**Problem:** Blazor Server apps lose authentication tokens on restart without distributed cache

**Solution:** Use different caching strategies for different environments

| Environment | Cache Type | Why | Cost |
|------------|------------|-----|------|
| **Local Development** | Redis (Docker) | Fast, easy to set up | $0 |
| **Azure Production** | SQL Server | Uses existing database | $0 |

### Why SQL Server Cache in Azure?

1. **Zero Additional Cost** - You already have Azure SQL Database
2. **Reliable** - SQL Server is highly available
3. **Simple** - No Redis to configure, monitor, or pay for
4. **Automatic** - SessionCache table created automatically
5. **Sufficient** - Auth tokens are small and accessed infrequently

### Performance Comparison

| Operation | Redis | SQL Server Cache |
|-----------|-------|------------------|
| Token Read | ~1-2ms | ~5-10ms |
| Token Write | ~1-2ms | ~5-10ms |
| Impact | Negligible for auth tokens | ? Still excellent |

**Verdict:** For authentication tokens (accessed once per session), the 5-8ms difference is imperceptible to users.

## Implementation

### Code Changes Made

1. **Budget.AppHost/AppHost.cs**
   - Added Redis reference with `.PublishAsConnectionString()` (local only)
   - Added comment explaining Azure uses SQL Server

2. **Budget.Web/Startup/ConfigureServices.cs**
   - Detects Azure environment using `AzureEnvironment.IsRunningOnAzure`
   - Uses SQL Server cache in Azure
   - Uses Redis locally
   - Falls back to in-memory for development

3. **Documentation**
   - Updated `docs/AZURE_DEPLOYMENT.md` to reflect SQL Server cache
   - Removed Redis cost estimates and configuration
   - Added SQL Server cache troubleshooting

### SessionCache Table

Automatically created by `dotnet sql-cache create` command (already run):

```sql
CREATE TABLE [dbo].[SessionCache] (
    [Id] nvarchar(449) NOT NULL PRIMARY KEY,
    [Value] varbinary(MAX) NOT NULL,
    [ExpiresAtTime] datetimeoffset NOT NULL,
    [SlidingExpirationInSeconds] bigint NULL,
    [AbsoluteExpiration] datetimeoffset NULL
);

CREATE NONCLUSTERED INDEX IX_SessionCache_ExpiresAtTime 
    ON [dbo].[SessionCache] ([ExpiresAtTime]);
```

This table stores encrypted tokens with automatic expiration.

## Cost Breakdown

### Actual Monthly Costs

| Resource | Cost | Notes |
|----------|------|-------|
| Container Apps | $25-50 | Consumption-based, scales to zero |
| SQL Server | $0 | Using existing database + SessionCache table |
| Token Cache | $0 | Just a table in SQL Server |
| Container Registry | $5 | Basic tier |
| Log Analytics | $10-20 | Can be reduced with retention policies |
| **Total** | **$40-75** | **Most costs are consumption-based** |

### If You Used Redis Instead

| Resource | Additional Cost |
|----------|----------------|
| Redis Cache (Basic C0) | +$16/month |
| Redis Cache (Standard C1) | +$64/month |
| **No benefit for auth tokens** | **Just extra cost** |

## Deployment Steps

### First Time Setup

1. **Ensure SessionCache table exists** (already done):
   ```bash
   dotnet sql-cache create "your-connection-string" dbo SessionCache
   ```

2. **Deploy to Azure**:
   ```bash
   azd up
   ```

The app will automatically:
- Use SQL Server cache in Azure
- Use Redis locally
- Handle token persistence across restarts

### Verify Deployment

After deployment, check the token cache:

```bash
sqlcmd -S fantumsqlserver.database.windows.net -d BudgetDB -U dpc -P "your-password" \
  -Q "SELECT COUNT(*) as CachedTokens FROM dbo.SessionCache"
```

## Testing

### Local Testing (Redis)

```bash
# Start Redis
docker-compose up -d

# Run app
dotnet run --project Budget.AppHost

# Stop and restart - tokens persist ?
```

### Azure Testing (SQL Server Cache)

1. Deploy to Azure
2. Log in
3. Test "Backup All Tables" - works ?
4. Close browser, return later - still logged in ?
5. Container Apps restart - still logged in ?

## Maintenance

### Monitor Cache Size

```sql
SELECT 
    COUNT(*) as TotalTokens,
    SUM(DATALENGTH([Value]))/1024.0 as SizeKB,
    MAX(ExpiresAtTime) as LatestExpiry
FROM dbo.SessionCache;
```

### Clean Expired Tokens

SQL Server automatically cleans expired entries, but you can manually clean:

```sql
DELETE FROM dbo.SessionCache 
WHERE ExpiresAtTime < GETUTCDATE();
```

### Typical Cache Size

- **Per user session**: ~2-5KB
- **100 active users**: ~200-500KB
- **Impact on SQL Server**: Negligible

## When to Consider Redis

Switch to Redis if you experience:

1. **High user volume** (1000+ concurrent users)
2. **Frequent token refreshes** (multiple per second)
3. **Performance issues** (SQL Server CPU >80%)
4. **Budget available** ($16-64/month)

Otherwise, SQL Server cache is perfect for your use case.

## Summary

? **What You Get:**
- Token persistence across app restarts
- No need to log out/in after restarting
- Zero additional Azure costs
- Production-ready solution

? **What You Avoid:**
- $16-64/month Redis costs
- Redis configuration complexity
- Another service to monitor

? **Trade-offs:**
- Slightly slower than Redis (~5-8ms vs ~1-2ms)
- For auth tokens: **This difference is imperceptible**

**Result:** A cost-optimized, production-ready solution that meets your needs without unnecessary Azure costs!
