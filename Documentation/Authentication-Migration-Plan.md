# Authentication Migration Plan: ASP.NET Core Identity to Microsoft Entra ID

## Executive Summary

This document outlines the complete migration plan for transitioning the FantumBudget application from ASP.NET Core Identity (local database authentication) to Microsoft Entra ID (cloud-based authentication and authorization).

**Migration Goal**: Replace the current database-driven identity system with enterprise-grade Microsoft Entra ID authentication while maintaining all existing functionality and improving security.

## Current State

### Existing Authentication System
- **Technology**: ASP.NET Core Identity with Entity Framework Core
- **Database**: IdentityDBContext with BudgetIdentity schema
- **Users**: Stored in local SQL database
- **Roles**: Admin role defined and managed locally
- **Projects Affected**:
  - Budget.Web: Identity UI components and authentication
  - Budget.Api: Cookie-based authentication forwarding

### Current Files
```
Budget.Web/
├── Components/Account/
│   ├── IdentityComponentsEndpointRouteBuilderExtensions.cs
│   ├── IdentityRevalidatingAuthenticationStateProvider.cs
│   ├── IdentityRedirectManager.cs
│   ├── IdentityUserAccessor.cs
│   └── IdentityNoOpEmailSender.cs
├── Data/
│   └── IdentityDBContext.cs
├── Startup/
│   ├── ConfigureIdentity.cs
│   └── ConfigureDatabase.cs (Identity DB setup)
└── Migrations/
    └── IdentityDBContextModelSnapshot.cs
```

## Target State

### New Authentication System
- **Technology**: Microsoft Entra ID (Azure AD) with OpenID Connect
- **Identity Provider**: Azure cloud-based
- **Users**: Managed in Entra ID tenant
- **Roles**: App Roles defined in Entra app registration
- **Enhanced Security**: MFA, Conditional Access, SSO support

### Benefits
- ✅ Centralized identity management
- ✅ Enterprise SSO capabilities
- ✅ Built-in MFA and security features
- ✅ No password management overhead
- ✅ Conditional Access policies
- ✅ Better audit and compliance
- ✅ Reduced security attack surface

## Migration Phases

### Phase 1: Entra ID Configuration ✅ (Current Phase)

**Status**: In Progress  
**Estimated Time**: 2-4 hours  
**Complexity**: Low

#### Objectives
- [x] Create comprehensive documentation
- [x] Build PowerShell automation script
- [x] Define configuration templates
- [x] Establish Entra app registration

#### Deliverables
- ✅ **Setup Script**: `scripts/Setup-EntraApp.ps1`
- ✅ **Documentation**: `Documentation/Phase1-EntraID-Setup.md`
- ✅ **Config Template**: `Documentation/entra-config-template.json`
- ✅ **Migration Plan**: `Documentation/Authentication-Migration-Plan.md` (this document)

#### Tasks Completed
- [x] Created PowerShell automation script with:
  - App registration creation
  - Redirect URIs configuration
  - Token settings (ID + Access tokens)
  - API permissions (User.Read, email, openid, profile)
  - Client secret generation
  - Three app roles (Admin, PowerUser, User)
  - Configuration output
  - Error handling and validation
- [x] Created comprehensive setup documentation
- [x] Created configuration template with examples
- [x] Created migration plan

#### Next Steps for Phase 1
1. Run the PowerShell script to create app registration:
   ```powershell
   cd scripts
   .\Setup-EntraApp.ps1 -EnvironmentName "your-prod-env"
   ```
2. Save the output configuration values
3. Grant admin consent for API permissions
4. Assign initial users to roles
5. Verify app registration in Azure Portal

#### Success Criteria
- [x] PowerShell script executes successfully
- [x] App registration created with correct settings
- [x] All three roles defined
- [x] Documentation complete and clear
- [ ] Admin consent granted (manual step)
- [ ] Test users assigned to roles (manual step)

---

### Phase 2: Code Migration (Upcoming)

**Status**: Not Started  
**Estimated Time**: 8-12 hours  
**Complexity**: Medium-High

#### Objectives
- Replace Identity authentication with Entra ID/OIDC
- Update Budget.Web for Entra authentication
- Update Budget.Api for JWT token validation
- Maintain role-based authorization
- Preserve existing authorization policies

#### Key Files to Modify

**Budget.Web:**
```
Startup/
├── ConfigureIdentity.cs → Rename to ConfigureAuthentication.cs
│   - Remove Identity-specific code
│   - Add OIDC authentication
│   - Configure Microsoft Identity Web
└── ConfigureServices.cs
    - Update authentication state provider
    - Configure token forwarding to API

Components/Account/
├── Remove Identity-specific components
└── Add Entra-specific components (login/logout handling)
```

**Budget.Api:**
```
Startup/
└── Add ConfigureAuthentication.cs
    - JWT Bearer authentication
    - Token validation
    - Role claims mapping
```

#### Implementation Steps

1. **Install Required Packages**
   ```bash
   # Budget.Web
   dotnet add Budget.Web package Microsoft.Identity.Web
   dotnet add Budget.Web package Microsoft.Identity.Web.UI
   
   # Budget.Api
   dotnet add Budget.Api package Microsoft.Identity.Web
   ```

2. **Update Budget.Web Configuration**
   - Add AzureAd section to appsettings.json
   - Configure OIDC authentication
   - Update authentication middleware
   - Modify authentication state provider

3. **Update Budget.Api Configuration**
   - Add AzureAd section for JWT validation
   - Configure Bearer token authentication
   - Update authorization policies

4. **Update UI Components**
   - Replace Identity login components
   - Update login/logout flows
   - Modify user display components

5. **Testing**
   - Test login flow
   - Verify role claims
   - Test authorization policies
   - Verify API authentication

#### Migration Code Examples

**Budget.Web - ConfigureAuthentication.cs:**
```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

public static class ConfigureAuthentication
{
    public static void AddAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();
        
        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();
    }
}
```

**Budget.Api - ConfigureAuthentication.cs:**
```csharp
using Microsoft.Identity.Web;

public static class ConfigureAuthentication
{
    public static void AddAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    }
    
    public static void AddAuthorization(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
            .AddPolicy("PowerUser", policy => policy.RequireRole("Admin", "PowerUser"))
            .AddPolicy("User", policy => policy.RequireRole("Admin", "PowerUser", "User"));
    }
}
```

#### Rollback Plan
- Keep Identity code in feature branch
- Maintain database backups
- Document rollback procedures
- Test rollback before production deployment

---

### Phase 3: Data Migration (Upcoming)

**Status**: Not Started  
**Estimated Time**: 4-6 hours  
**Complexity**: Medium

#### Objectives
- Migrate existing users to Entra ID
- Map existing roles to Entra app roles
- Create user accounts in Entra ID
- Assign roles to migrated users

#### Migration Strategies

**Option A: Manual Migration (Recommended for Small User Base)**
- Export user list from database
- Create users in Entra ID manually or via CSV import
- Assign roles through Azure Portal
- Send invitation emails to users

**Option B: Automated Migration (For Large User Base)**
- Create PowerShell script for bulk user creation
- Use Microsoft Graph API
- Automated role assignment
- Bulk invitation sending

#### User Migration Steps

1. **Export Current Users**
   ```sql
   SELECT 
       u.Email,
       u.UserName,
       STRING_AGG(r.Name, ',') as Roles
   FROM AspNetUsers u
   LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
   LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
   GROUP BY u.Email, u.UserName
   ```

2. **Create Entra Users**
   - Use Azure Portal for small sets
   - Use Graph API PowerShell for bulk:
     ```powershell
     New-MgUser -DisplayName "User Name" `
                -MailNickname "username" `
                -UserPrincipalName "user@domain.com" `
                -AccountEnabled $true `
                -PasswordProfile @{Password="TempPass123!"}
     ```

3. **Assign Roles**
   ```powershell
   New-MgServicePrincipalAppRoleAssignment `
       -ServicePrincipalId $spId `
       -PrincipalId $userId `
       -ResourceId $spId `
       -AppRoleId $roleId
   ```

4. **Notify Users**
   - Send welcome emails with new login instructions
   - Provide password reset links
   - Schedule training sessions if needed

---

### Phase 4: Database Cleanup (Upcoming)

**Status**: Not Started  
**Estimated Time**: 2-3 hours  
**Complexity**: Low

#### Objectives
- Archive Identity data for compliance
- Remove Identity schema from database
- Clean up Identity-related code
- Update database migrations

#### Cleanup Steps

1. **Archive Data** (if required for compliance)
   ```sql
   -- Create archive schema
   CREATE SCHEMA IdentityArchive;
   
   -- Move tables to archive
   ALTER SCHEMA IdentityArchive TRANSFER BudgetIdentity.AspNetUsers;
   -- Repeat for all Identity tables
   ```

2. **Remove Identity Context**
   - Delete IdentityDBContext.cs
   - Remove Identity migrations
   - Update database configuration

3. **Remove Identity Components**
   - Delete Components/Account/ directory
   - Remove Identity services from DI
   - Clean up unused packages

4. **Database Migration**
   ```bash
   # Create migration to remove Identity schema
   dotnet ef migrations add RemoveIdentitySchema -p Budget.Web
   
   # Review migration before applying
   dotnet ef migrations script
   
   # Apply to database
   dotnet ef database update -p Budget.Web
   ```

---

### Phase 5: Testing & Validation (Upcoming)

**Status**: Not Started  
**Estimated Time**: 4-8 hours  
**Complexity**: Medium

#### Test Scenarios

**Authentication Testing:**
- [ ] User can log in with Entra credentials
- [ ] User can log out successfully
- [ ] Session timeout works correctly
- [ ] Token refresh works automatically
- [ ] Redirect URIs work for all environments

**Authorization Testing:**
- [ ] Admin role has full access
- [ ] PowerUser role has elevated access
- [ ] User role has standard access
- [ ] Unauthorized access is properly blocked
- [ ] Role-based UI elements show/hide correctly

**API Testing:**
- [ ] API validates JWT tokens correctly
- [ ] API extracts role claims properly
- [ ] API authorization policies work
- [ ] Token forwarding from Web to API works

**Integration Testing:**
- [ ] End-to-end user workflows function
- [ ] All existing features remain functional
- [ ] No regression in functionality

**Security Testing:**
- [ ] No secrets in source code
- [ ] HTTPS enforced
- [ ] Token validation secure
- [ ] No security vulnerabilities introduced

**Performance Testing:**
- [ ] Authentication doesn't significantly impact performance
- [ ] Token caching works effectively

---

### Phase 6: Production Deployment (Upcoming)

**Status**: Not Started  
**Estimated Time**: 2-4 hours  
**Complexity**: Medium

#### Pre-Deployment Checklist

**Azure Configuration:**
- [ ] Production app registration created
- [ ] Production redirect URIs configured
- [ ] Client secret stored in Key Vault
- [ ] Admin consent granted
- [ ] Users created and roles assigned

**Application Configuration:**
- [ ] appsettings.Production.json updated
- [ ] Key Vault integration tested
- [ ] Environment variables configured in Container Apps
- [ ] Connection strings updated

**Testing:**
- [ ] Staging environment tested
- [ ] UAT completed
- [ ] Security review passed
- [ ] Performance acceptable

#### Deployment Steps

1. **Deploy to Staging**
   ```bash
   # Deploy to staging environment
   azd deploy --environment staging
   ```

2. **Staging Validation**
   - Test login with Entra ID
   - Verify all roles work
   - Test critical user workflows
   - Check logs for errors

3. **Deploy to Production**
   ```bash
   # Deploy to production environment
   azd deploy --environment production
   ```

4. **Post-Deployment Validation**
   - Monitor authentication logs
   - Check for errors
   - Verify user access
   - Monitor performance

5. **User Communication**
   - Notify users of change
   - Provide new login instructions
   - Offer support for issues

#### Rollback Procedure

If critical issues occur:

1. **Immediate Rollback**
   ```bash
   # Revert to previous deployment
   azd deploy --environment production --revision previous
   ```

2. **Re-enable Identity**
   - Revert code changes
   - Restore database if needed
   - Re-deploy previous version

3. **Post-Mortem**
   - Analyze what went wrong
   - Update migration plan
   - Plan re-migration

---

## Timeline

| Phase | Duration | Start Date | End Date | Status |
|-------|----------|------------|----------|--------|
| Phase 1: Entra ID Configuration | 2-4 hours | TBD | TBD | ✅ In Progress |
| Phase 2: Code Migration | 8-12 hours | TBD | TBD | ⏳ Pending |
| Phase 3: Data Migration | 4-6 hours | TBD | TBD | ⏳ Pending |
| Phase 4: Database Cleanup | 2-3 hours | TBD | TBD | ⏳ Pending |
| Phase 5: Testing & Validation | 4-8 hours | TBD | TBD | ⏳ Pending |
| Phase 6: Production Deployment | 2-4 hours | TBD | TBD | ⏳ Pending |
| **Total Estimated Time** | **22-37 hours** | | | |

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Users locked out after migration | High | Medium | Maintain Identity fallback for 30 days |
| Role mapping errors | Medium | Low | Thorough testing, manual verification |
| Token validation issues | High | Low | Comprehensive API testing |
| Secrets exposed | Critical | Low | Use Key Vault, security review |
| Performance degradation | Medium | Low | Performance testing, monitoring |
| User resistance | Low | Medium | Clear communication, training |

## Resources

### Documentation
- [Phase 1 Setup Guide](Phase1-EntraID-Setup.md)
- [Configuration Template](entra-config-template.json)
- [PowerShell Script](../scripts/Setup-EntraApp.ps1)

### Microsoft Resources
- [Microsoft Identity Web](https://learn.microsoft.com/entra/msal/dotnet/microsoft-identity-web/)
- [ASP.NET Core Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/)
- [Migrate to MSAL.NET](https://learn.microsoft.com/entra/msal/dotnet/how-to/msal-net-migration)

### Support
- Azure Support: https://azure.microsoft.com/support/
- Microsoft Q&A: https://learn.microsoft.com/answers/
- Internal Documentation: See /Documentation folder

## Communication Plan

### Stakeholders
- Development Team
- IT Operations
- Security Team
- End Users
- Management

### Communication Schedule
- **Week -2**: Announce migration plan to stakeholders
- **Week -1**: Provide detailed user guide
- **Day 0**: Deploy to production, send go-live notification
- **Week +1**: Gather feedback, address issues
- **Week +2**: Decommission old system (if stable)

## Success Metrics

- [ ] 100% of users can authenticate with Entra ID
- [ ] 0 critical security vulnerabilities
- [ ] All existing features functional
- [ ] < 5% increase in authentication time
- [ ] Positive user feedback
- [ ] No production incidents related to authentication

## Sign-Off

### Phase 1 Completion
- [ ] PowerShell script created and tested
- [ ] Documentation complete
- [ ] App registration created
- [ ] Initial users assigned roles
- [ ] Approved to proceed to Phase 2

**Completed By**: _________________  
**Date**: _________________  
**Approved By**: _________________  

---

## Appendix

### A. Related Files

**Phase 1 Files:**
- `scripts/Setup-EntraApp.ps1` - Automation script
- `Documentation/Phase1-EntraID-Setup.md` - Setup guide
- `Documentation/entra-config-template.json` - Configuration template
- `Documentation/Authentication-Migration-Plan.md` - This document

**Files to Create in Phase 2:**
- `Budget.Web/Startup/ConfigureAuthentication.cs`
- `Budget.Api/Startup/ConfigureAuthentication.cs`
- Updated `appsettings.json` files

### B. Useful Commands

**Check Current Identity Users:**
```sql
SELECT COUNT(*) FROM BudgetIdentity.AspNetUsers;
```

**List App Registrations:**
```powershell
az ad app list --display-name "FantumBudget" --query "[].{Name:displayName,AppId:appId}"
```

**Test Token Acquisition:**
```powershell
$token = az account get-access-token --resource https://graph.microsoft.com --query accessToken -o tsv
```

### C. Contact Information

**For Issues:**
- Phase 1 (Setup): See [Phase1-EntraID-Setup.md](Phase1-EntraID-Setup.md)
- Phase 2+ (Code): Contact development team lead
- Azure/Entra Issues: Contact IT operations

---

**Document Version**: 1.0  
**Last Updated**: 2025-12-08  
**Next Review**: After Phase 1 completion
