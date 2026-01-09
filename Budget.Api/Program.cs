using Scalar.AspNetCore;
using Budget.Api;
using Budget.Api.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
  loggingBuilder
    .SetMinimumLevel(LogLevel.Information)
    .AddConsole();
});

ILogger logger = loggerFactory.CreateLogger<Program>();

// Log Azure environment detection
logger.LogInformation("=== Azure Environment Detection ===");
logger.LogInformation("IsRunningOnAzure: {IsAzure}", AzureEnvironment.IsRunningOnAzure);
logger.LogInformation("Hosting Environment: {Environment}", AzureEnvironment.HostingEnvironment);
logger.LogInformation("App Name: {AppName}", AzureEnvironment.AppName ?? "N/A");
logger.LogInformation("Instance ID: {InstanceId}", AzureEnvironment.InstanceId ?? "N/A");

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Configure logging
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);





// Add HTTP logging
builder.Services.AddHttpLogging(logging =>
{
  logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
  logging.RequestBodyLogLimit = 4096;
  logging.ResponseBodyLogLimit = 4096;
});

// Add OpenAPI and Carter
builder.Services.AddOpenApi();
builder.Services.AddCarter();

var assembly = typeof(Budget.Api.Program).Assembly;

// Configure configuration sources (appsettings, secrets, environment, Key Vault)
Misc.SetupConfigurationSources(builder, assembly, logger);

// Log all configuration settings with their provider sources
Misc.LogAllConfigurationSettings(builder, logger);

// Add FantumMediator
builder.Services.AddFantumMediator();

// Register HttpContextAccessor and CurrentFamilyService for multi-tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentFamilyService, CurrentFamilyService>();

// Check if running in test mode
var isTest = AppDomain.CurrentDomain.GetAssemblies()
  .Any(a => a.FullName != null && (a.FullName.StartsWith("xunit")
                                   || a.FullName.StartsWith("nunit")
                                   || a.FullName.StartsWith("Microsoft.VisualStudio.TestPlatform")));



// Get connection strings (not required for tests)
var budgetConnectionString = isTest ? "TestConnection" : Misc.GetConnectionString(builder, Misc.ConnectionStringType.Budget, logger);
var identityConnectionString = isTest ? "TestConnection" : Misc.GetConnectionString(builder, Misc.ConnectionStringType.Identity, logger);

if (string.IsNullOrWhiteSpace(budgetConnectionString))
  throw new InvalidOperationException("Missing Budget DB connection string.");
if (string.IsNullOrWhiteSpace(identityConnectionString))
  throw new InvalidOperationException("Missing Identity DB connection string.");

var isDev = builder.Environment.IsDevelopment();

// Configure BudgetContext
builder.Services.AddDbContext<BudgetContext>(options =>
{
  if (isTest)
  {
    options.UseInMemoryDatabase("BudgetTestDb");
  }
  else
  {
    options.UseSqlServer(budgetConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "budget")
        .EnableRetryOnFailure(
          maxRetryCount: 5, // Number of retries
          maxRetryDelay: TimeSpan.FromSeconds(10), // Delay between retries
          errorNumbersToAdd: null) // Additional SQL error codes to retry on)
    );
  }

  if (isDev || isTest)
  {
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
  }
});

// Configure ApiIdentityContext
builder.Services.AddDbContext<ApiIdentityContext>(options =>
{
  if (isTest)
  {
    options.UseInMemoryDatabase("IdentityTestDb");
  }
  else
  {
    options.UseSqlServer(identityConnectionString, options => options.EnableRetryOnFailure(
      maxRetryCount: 5, // Number of retries
      maxRetryDelay: TimeSpan.FromSeconds(10), // Delay between retries
      errorNumbersToAdd: null));
  }

  if (isDev || isTest)
  {
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
  }
});

// Configure Identity
builder.Services
  .AddIdentityCore<BudgetUser>(o => { o.User.RequireUniqueEmail = true; })
  .AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<ApiIdentityContext>()
  .AddSignInManager();

// Configure JWT authentication
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.SigningKey));

// Configure dual authentication: Entra ID JWT + Custom JWT
// Use explicit scheme names to avoid conflicts
const string EntraScheme = "EntraJwt";
const string LocalScheme = "LocalJwt";
const string DynamicScheme = "SmartJwt"; // Policy scheme that forwards to the right handler

var authBuilder = builder.Services.AddAuthentication(options =>
{
  // Use the dynamic policy scheme as default
  options.DefaultScheme = DynamicScheme;
  options.DefaultChallengeScheme = DynamicScheme;
  logger.LogInformation("Using smart JWT authentication with dynamic scheme selection");
});

// Add custom JWT Bearer for backward compatibility (local auth)
authBuilder.AddJwtBearer(LocalScheme, options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtOpt.Issuer,
    ValidAudience = jwtOpt.Audience,
    IssuerSigningKey = key,
    ClockSkew = TimeSpan.FromMinutes(1)
  };
});

// Add Microsoft Entra ID JWT Bearer authentication
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var isEntraConfigured = !string.IsNullOrWhiteSpace(azureAdSection["ClientId"]);

if (isEntraConfigured)
{
  authBuilder.AddMicrosoftIdentityWebApi(options =>
  {
    azureAdSection.Bind(options);
    
    // Map Azure AD "roles" claims to standard ClaimTypes.Role
    options.TokenValidationParameters.RoleClaimType = "roles";
  }, 
  options =>
  {
    azureAdSection.Bind(options);
  },
  EntraScheme);
  
  logger.LogInformation("Microsoft Entra ID JWT Bearer authentication configured with scheme: {Scheme}", EntraScheme);
}
else
{
  logger.LogWarning("AzureAd:ClientId not configured - Entra ID authentication disabled");
}


// Add policy scheme that intelligently forwards to the correct JWT handler
// This prevents both handlers from trying to validate every token
authBuilder.AddPolicyScheme(DynamicScheme, "Smart JWT Selector", options =>
{
  options.ForwardDefaultSelector = context =>
  {
    // Extract the Authorization header
    var authHeader = context.Request.Headers.Authorization.ToString();
    
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
      // Return default scheme instead of null - PolicySchemeHandler needs a valid scheme
      return isEntraConfigured ? EntraScheme : LocalScheme;
    }

    var token = authHeader.Substring("Bearer ".Length).Trim();
    
    // Quick inspection: decode the token header to determine issuer
    try
    {
      var handler = new JwtSecurityTokenHandler();
      if (handler.CanReadToken(token))
      {
        var jwtToken = handler.ReadJwtToken(token);
        var issuer = jwtToken.Issuer;
        
        // If issuer is from Microsoft (Entra ID), use EntraScheme
        if (isEntraConfigured && (issuer.Contains("login.microsoftonline.com") || issuer.Contains("sts.windows.net")))
        {
          return EntraScheme;
        }
      }
    }
    catch
    {
      // If we can't read the token, try LocalScheme first
    }
    
    // Default to LocalScheme for custom tokens
    return LocalScheme;
  };
});

logger.LogInformation("Smart JWT selector configured - will route to {EntraScheme} or {LocalScheme} based on token issuer", 
  EntraScheme, LocalScheme);

// Configure authorization policies - use the dynamic scheme
builder.Services.AddAuthorization(options =>
{
  logger.LogInformation("Configuring authorization with dynamic scheme: {Scheme}", DynamicScheme);

  // Default policy requires authentication from the dynamic scheme
  options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .AddAuthenticationSchemes(DynamicScheme)
    .Build();

  // Admin-only policy
  options.AddPolicy("AdminOnly", policy => policy
    .RequireRole("Admin")
    .AddAuthenticationSchemes(DynamicScheme));

  // Admin policy (alias for AdminOnly - some endpoints use "Admin" instead)
  options.AddPolicy("Admin", policy => policy
    .RequireRole("Admin")
    .AddAuthenticationSchemes(DynamicScheme));

  // PowerUser or above policy
  options.AddPolicy("PowerUserOrAbove", policy => policy
    .RequireAssertion(context =>
      context.User.IsInRole("Admin") || context.User.IsInRole("PowerUser"))
    .AddAuthenticationSchemes(DynamicScheme));

  // Authenticated user policy (any role)
  options.AddPolicy("AuthenticatedUser", policy => policy
    .RequireAuthenticatedUser()
    .AddAuthenticationSchemes(DynamicScheme));
});

// Register JWT token service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Register BackupAzureSql service with HttpClient
builder.Services.AddHttpClient<BackupAzureSql>();

// Register BackupProgressService for tracking background backup jobs
builder.Services.AddSingleton<IBackupProgressService, BackupProgressService>();

// Configure Azure Storage (Blob and Table) for backup functionality
var azureStorageConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
var storageBlobEndpoint = builder.Configuration["AZURE_STORAGE_BLOB_ENDPOINT"];
var storageTableEndpoint = builder.Configuration["AZURE_STORAGE_TABLE_ENDPOINT"];

// Determine if running on Azure (managed identity available)
var isRunningOnAzure = AzureEnvironment.IsRunningOnAzure;

logger.LogInformation("=== Azure Storage Configuration ===");
logger.LogInformation("IsRunningOnAzure: {IsRunningOnAzure}", isRunningOnAzure);
logger.LogInformation("StorageBlobEndpoint: {Endpoint}", storageBlobEndpoint ?? "(not set)");
logger.LogInformation("StorageTableEndpoint: {Endpoint}", storageTableEndpoint ?? "(not set)");
logger.LogInformation("Has ConnectionString: {HasConnStr}", !string.IsNullOrWhiteSpace(azureStorageConnectionString));

if (isRunningOnAzure && !string.IsNullOrWhiteSpace(storageBlobEndpoint))
{
  // Use Managed Identity on Azure
  var blobUri = new Uri(storageBlobEndpoint);
  var tableUri = new Uri(storageTableEndpoint!);
  
  logger.LogInformation("Creating BlobServiceClient with Managed Identity for: {Uri}", blobUri);
  logger.LogInformation("Creating TableServiceClient with Managed Identity for: {Uri}", tableUri);
  
  var credential = new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
  {
    ExcludeEnvironmentCredential = false,
    ExcludeManagedIdentityCredential = false,
    ExcludeSharedTokenCacheCredential = true,
    ExcludeVisualStudioCredential = true,
    ExcludeVisualStudioCodeCredential = true,
    ExcludeAzureCliCredential = true,
    ExcludeAzurePowerShellCredential = true,
    ExcludeInteractiveBrowserCredential = true,
    ManagedIdentityClientId = "c5817686-acae-494b-a8e9-f5620f83b0d4"
  });
  
  builder.Services.AddSingleton(sp => new Azure.Storage.Blobs.BlobServiceClient(blobUri, credential));
  builder.Services.AddSingleton(sp => new Azure.Data.Tables.TableServiceClient(tableUri, credential));
  
  logger.LogInformation("? Azure Storage configured with Managed Identity (Blob: {BlobEndpoint}, Table: {TableEndpoint})", 
    storageBlobEndpoint, storageTableEndpoint);
}
else if (!string.IsNullOrWhiteSpace(azureStorageConnectionString))
{
  // Use Connection String locally
  logger.LogInformation("Creating storage clients with connection string (local development)");
  builder.Services.AddSingleton(sp => new Azure.Storage.Blobs.BlobServiceClient(azureStorageConnectionString));
  builder.Services.AddSingleton(sp => new Azure.Data.Tables.TableServiceClient(azureStorageConnectionString));
  logger.LogInformation("? Azure Storage configured with connection string (local development)");
}
else
{
  // Always register storage clients even if not configured
  // Use UseDevelopmentStorage=true for local Azure Storage Emulator / Azurite
  logger.LogWarning("?? Azure Storage not configured - using development storage (UseDevelopmentStorage=true)");
  logger.LogWarning("   To enable backup functionality, configure Azure Storage connection string or endpoints");
  
  var devStorageConnectionString = "UseDevelopmentStorage=true";
  builder.Services.AddSingleton(sp => new Azure.Storage.Blobs.BlobServiceClient(devStorageConnectionString));
  builder.Services.AddSingleton(sp => new Azure.Data.Tables.TableServiceClient(devStorageConnectionString));
  
  logger.LogInformation("? Registered storage clients with development storage connection string");
}

var app = builder.Build();

// Ensure databases exist
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  services.GetRequiredService<ApiIdentityContext>().Database.EnsureCreated();
  services.GetRequiredService<BudgetContext>().Database.EnsureCreated();
}

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
  app.UseHttpLogging();
  app.MapOpenApi();
  app.MapScalarApiReference(options =>
    options.WithTheme(ScalarTheme.DeepSpace)
      .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map Carter endpoints
app.MapCarter();

// Map default Aspire endpoints (health checks)
app.MapDefaultEndpoints();

// Sample weather forecast endpoint
var summaries = new[]
  { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

app.MapGet("/weatherforecast", () =>
{
  var forecast = Enumerable.Range(1, 5)
    .Select(index => new WeatherForecast(
      DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
      Random.Shared.Next(-20, 55),
      summaries[Random.Shared.Next(summaries.Length)]))
    .ToArray();
  return forecast;
}).WithName("GetWeatherForecast");

// DEBUG: Temporary endpoint to check user roles and claims
if (app.Environment.IsDevelopment())
{
  // Anonymous version - see if any auth data exists
  app.MapGet("/api/debug/my-auth-status", (HttpContext httpContext) =>
  {
    var user = httpContext.User;
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var cookies = httpContext.Request.Cookies.Keys.ToList();
    
    var roles = user.Claims
        .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
        .Select(c => c.Value)
        .ToList();
    
    var allClaims = user.Claims.Select(c => new { 
        Type = c.Type, 
        Value = c.Value,
        TypeFriendly = c.Type.Split('/').Last()
    }).ToList();
    
    return Results.Ok(new { 
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
        AuthenticationType = user.Identity?.AuthenticationType,
        Name = user.Identity?.Name,
        HasAuthorizationHeader = !string.IsNullOrEmpty(authHeader),
        AuthorizationHeaderPreview = authHeader?.Length > 20 ? authHeader.Substring(0, 20) + "..." : authHeader,
        CookieNames = cookies,
        Roles = roles,
        RoleCount = roles.Count,
        HasAdminRole = user.IsInRole("Admin"),
        HasPowerUserRole = user.IsInRole("PowerUser"),
        HasUserRole = user.IsInRole("User"),
        AllClaims = allClaims,
        ClaimCount = allClaims.Count,
        RequestScheme = httpContext.Request.Scheme,
        RequestHost = httpContext.Request.Host.ToString()
    });
  })
  .AllowAnonymous()
  .WithName("GetAuthStatus")
  .WithTags("Debug");

  // Authenticated version - requires valid token
  app.MapGet("/api/debug/my-roles", (ClaimsPrincipal user) =>
  {
    var roles = user.Claims
        .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
        .Select(c => c.Value)
        .ToList();
    
    var allClaims = user.Claims.Select(c => new { 
        Type = c.Type, 
        Value = c.Value,
        TypeFriendly = c.Type.Split('/').Last()
    }).ToList();
    
    return Results.Ok(new { 
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
        AuthenticationType = user.Identity?.AuthenticationType,
        Name = user.Identity?.Name,
        Roles = roles,
        RoleCount = roles.Count,
        HasAdminRole = user.IsInRole("Admin"),
        HasPowerUserRole = user.IsInRole("PowerUser"),
        HasUserRole = user.IsInRole("User"),
        AllClaims = allClaims,
        ClaimCount = allClaims.Count
    });
  })
  
  .WithName("GetMyRoles")
  .WithTags("Debug");
}

app.Run();

// Program class for WebApplicationFactory in tests
namespace Budget.Api
{
  public partial class Program
  {
  }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}