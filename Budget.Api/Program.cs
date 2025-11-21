using Scalar.AspNetCore;
using Microsoft.Extensions.Logging;
using Budget.Api;
using Budget.Api.Services;

var builder = WebApplication.CreateBuilder(args);

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
Misc.SetupConfigurationSources(builder, assembly);


// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAll).Assembly));

// Get connection strings
var budgetConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Budget);
var identityConnectionString = Misc.GetConnectionString(builder.Configuration, Misc.ConnectionStringType.Identity);

if (string.IsNullOrWhiteSpace(budgetConnectionString)) 
    throw new InvalidOperationException("Missing Budget DB connection string.");
if (string.IsNullOrWhiteSpace(identityConnectionString)) 
    throw new InvalidOperationException("Missing Identity DB connection string.");

var isDev = builder.Environment.IsDevelopment();
var isTest = builder.Environment.IsEnvironment("Testing") || builder.Environment.IsEnvironment("Test");

// Configure BudgetContext
builder.Services.AddDbContext<BudgetContext>(options =>
{
    options.UseSqlServer(budgetConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "budget"));
    if (isDev || isTest)
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// Configure ApiIdentityContext
builder.Services.AddDbContext<ApiIdentityContext>(options =>
{
    options.UseSqlServer(identityConnectionString);
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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

builder.Services.AddAuthorization();

// Register JWT token service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        var allowedOrigins = builder.Configuration["ALLOWED_ORIGINS"];
        if (!string.IsNullOrWhiteSpace(allowedOrigins))
        {
            var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            options.AddPolicy("AllowBudgetWeb", policy =>
            {
                policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
            });
        }
        else
        {
            options.AddPolicy("AllowBudgetWeb", policy => 
            { 
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); 
            });
        }
    }
    else
    {
        var allowedOrigins = builder.Configuration["ALLOWED_ORIGINS"] 
            ?? throw new InvalidOperationException("ALLOWED_ORIGINS environment variable must be set in production.");
        var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        options.AddPolicy("AllowBudgetWeb", policy =>
        {
            policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        });
    }
});
// Register BackupAzureSql service with HttpClient
builder.Services.AddHttpClient<BackupAzureSql>();
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
app.UseCors("AllowBudgetWeb");
app.UseAuthentication();
app.UseAuthorization();

// Map Carter endpoints
app.MapCarter();

// Map default Aspire endpoints (health checks)
app.MapDefaultEndpoints();

// Sample weather forecast endpoint
var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

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

app.Run();

// Program class for WebApplicationFactory in tests
namespace Budget.Api
{
    public partial class Program { }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
