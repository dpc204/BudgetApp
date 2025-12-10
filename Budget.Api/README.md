# Budget.Api - Microsoft Entra ID Configuration

This API supports **dual authentication**:
1. **Microsoft Entra ID JWT Bearer** tokens (for production/Entra-authenticated users)
2. **Custom JWT tokens** (for local development and backward compatibility)

## Microsoft Entra ID Setup

### Required Configuration

The API requires these Azure Active Directory (Entra ID) values to validate JWT tokens from Budget.Web:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-app-id"
  }
}
```

### Using User Secrets (Development)

For local development, store these values in User Secrets:

```bash
cd Budget.Api

# Set Entra ID configuration
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "your-api-app-id"
dotnet user-secrets set "AzureAd:Domain" "yourtenant.onmicrosoft.com"
```

### Getting Configuration Values

These values come from the **Phase 1** Entra ID app registration:

1. **TenantId**: Azure Portal → Azure Active Directory → Overview → Tenant ID
2. **ClientId**: The Application (client) ID of the **Budget API** app registration (NOT the Budget.Web client ID)
3. **Domain**: Your Azure AD tenant domain (e.g., `contoso.onmicrosoft.com`)

**Note:** Budget.Api needs its own app registration in Entra ID with:
- Exposed API scope (e.g., `api://budget-api/access`)
- App roles defined: Admin, PowerUser, User
- Budget.Web configured as an authorized client application

See Phase 1 documentation for complete Entra ID setup.

### Using Azure Key Vault (Production)

For production deployments, store secrets in Azure Key Vault:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Secrets in Key Vault should use `--` separator (converted to `:` automatically):
- `AzureAd--TenantId`
- `AzureAd--ClientId`
- `AzureAd--Domain`

## Authorization Policies

The API provides role-based authorization policies:

### Available Policies

| Policy | Required Role(s) | Description |
|--------|-----------------|-------------|
| `AdminOnly` | Admin | Full administrative access |
| `PowerUserOrAbove` | PowerUser, Admin | Advanced features |
| `AuthenticatedUser` | User, PowerUser, Admin | Basic authenticated access |

### Using Policies in Endpoints

```csharp
// Require Admin role
app.MapGet("/api/admin/users", () => { })
   .RequireAuthorization("AdminOnly");

// Require PowerUser or Admin
app.MapPost("/api/reports", () => { })
   .RequireAuthorization("PowerUserOrAbove");

// Require any authenticated user
app.MapGet("/api/profile", () => { })
   .RequireAuthorization("AuthenticatedUser");
```

## Dual Authentication Support

The API accepts both:
1. **Entra ID JWT tokens** (from Budget.Web after Phase 2/3 implementation)
2. **Custom JWT tokens** (from local `/api/auth/login` endpoint)

This allows gradual migration and backward compatibility.

### Token Forwarding from Budget.Web

Budget.Web automatically forwards Entra ID access tokens in the `Authorization` header:

```
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6...
```

The API validates these tokens against Microsoft Entra ID.

## Testing Authentication

### 1. Test with Entra ID Token (via Budget.Web)

1. Sign in to Budget.Web using Microsoft
2. Budget.Web calls Budget.Api with Entra access token
3. API validates token and extracts roles

### 2. Test with Custom JWT Token (local development)

```bash
# Register a user
curl -X POST https://localhost:7001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Login and get token
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Use token in requests
curl https://localhost:7001/api/protected-endpoint \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Troubleshooting

### "AzureAd:ClientId not configured - Entra ID authentication disabled"

This warning appears when Entra ID configuration is missing. The API will only accept custom JWT tokens in this state.

**Solution:** Configure AzureAd values in User Secrets or Key Vault.

### "401 Unauthorized" with valid Entra token

**Possible causes:**
1. API's ClientId doesn't match the API app registration
2. Token audience doesn't match API scope
3. API app registration missing in Azure AD

**Solution:** Verify Phase 1 configuration and ensure Budget.Api has its own app registration.

### Token validation fails

**Check:**
1. TenantId matches your Azure AD tenant
2. ClientId matches the API app registration (not Budget.Web's client ID)
3. Token hasn't expired (tokens typically valid for 1 hour)
4. Roles are correctly assigned in Entra ID

### HTTPS certificate errors in development

```bash
dotnet dev-certs https --trust
```

## Security Best Practices

1. **Never commit secrets** to source control
2. **Use User Secrets** for local development
3. **Use Azure Key Vault** for production
4. **Rotate secrets** regularly (at least annually)
5. **Monitor authentication failures** in application logs
6. **Use HTTPS** for all API endpoints
7. **Validate token audiences** to prevent token reuse attacks

## Related Documentation

- [Budget.Web README](../Budget.Web/README.md) - Web app Entra ID configuration
- [Phase 1 Documentation](#) - Entra ID app registration setup
- [Microsoft.Identity.Web Documentation](https://github.com/AzureAD/microsoft-identity-web/wiki)
- [Azure AD Token Reference](https://docs.microsoft.com/azure/active-directory/develop/access-tokens)

## Migration Path

### Current State (Phase 3)
- ✅ Dual authentication active (Entra + custom JWT)
- ✅ Role-based policies configured
- ✅ Budget.Web forwards Entra tokens

### Future (Phase 4)
- Remove custom JWT authentication
- Remove local Identity database
- Entra ID as single source of truth for authentication
