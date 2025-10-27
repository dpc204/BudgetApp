using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Carter;
using MediatR;
using Budget.DB;
using Budget.Api.Features.Envelopes;
using Budget.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Budget.Api.Features.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Budget.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);

// Add HTTP logging services
builder.Services.AddHttpLogging(logging =>
{
 logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
 logging.RequestBodyLogLimit =4096;
 logging.ResponseBodyLogLimit =4096;
});

builder.Services.AddOpenApi();
builder.Services.AddCarter();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAll).Assembly));

// Read connection strings (env overrides supported via SetupConfigurationSources)
var budgetConnectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration, typeof(Program).Assembly, Misc.ConnectionStringType.Budget);
var identityConnectionString = Misc.SetupConfigurationSources(builder.Configuration, builder.Configuration, typeof(Program).Assembly, Misc.ConnectionStringType.Identity);

if (string.IsNullOrWhiteSpace(budgetConnectionString))
{
 throw new InvalidOperationException("Missing Budget DB connection string.");
}
if (string.IsNullOrWhiteSpace(identityConnectionString))
{
 throw new InvalidOperationException("Missing Identity DB connection string.");
}

var isDev = builder.Environment.IsDevelopment();
var isTest = builder.Environment.IsEnvironment("Testing") || builder.Environment.IsEnvironment("Test");

// Domain data context (uses schema 'budget')
builder.Services.AddDbContext<BudgetContext>(options =>
{
 options.UseSqlServer(budgetConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "budget"));
 if (isDev || isTest)
 {
 options.EnableDetailedErrors();
 options.EnableSensitiveDataLogging();
 }
});

// Identity context (separate or same DB)
builder.Services.AddDbContext<ApiIdentityContext>(options =>
{
 options.UseSqlServer(identityConnectionString);
 if (isDev || isTest)
 {
 options.EnableDetailedErrors();
 options.EnableSensitiveDataLogging();
 }
});

// Register Identity services for BudgetUser
builder.Services
 .AddIdentityCore<BudgetUser>(o => { o.User.RequireUniqueEmail = true; })
 .AddRoles<IdentityRole>()
 .AddEntityFrameworkStores<ApiIdentityContext>()
 .AddSignInManager();

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

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Add CORS policy using configuration
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
 policy.WithOrigins(origins)
 .AllowAnyMethod()
 .AllowAnyHeader()
 .AllowCredentials();
 });
 }
 else
 {
 options.AddPolicy("AllowBudgetWeb", policy =>
 {
 policy.AllowAnyOrigin()
 .AllowAnyMethod()
 .AllowAnyHeader();
 });
 }
 }
 else
 {
 var allowedOrigins = builder.Configuration["ALLOWED_ORIGINS"];
 if (string.IsNullOrWhiteSpace(allowedOrigins))
 {
 throw new InvalidOperationException("ALLOWED_ORIGINS environment variable must be set in production.");
 }
 var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
 options.AddPolicy("AllowBudgetWeb", policy =>
 {
 policy.WithOrigins(origins)
 .AllowAnyMethod()
 .AllowAnyHeader()
 .AllowCredentials();
 });
 }
});

var app = builder.Build();

// Always ensure databases exist at startup (idempotent)
using (var scope = app.Services.CreateScope())
{
 var services = scope.ServiceProvider;
 services.GetRequiredService<ApiIdentityContext>().Database.EnsureCreated();
 services.GetRequiredService<BudgetContext>().Database.EnsureCreated();
}

// Add HTTP request logging (logs all incoming requests)
if (app.Environment.IsDevelopment())
{
 app.UseHttpLogging();
 app.MapOpenApi();
 app.MapScalarApiReference(options =>
 options.WithTheme(ScalarTheme.DeepSpace)
 .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
}

app.UseHttpsRedirection();

// Enable CORS before authentication/authorization
app.UseCors("AllowBudgetWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.MapGet("/healthz", () => Results.Ok("OK"));

var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

app.MapGet("/weatherforecast", () =>
{
 var forecast = Enumerable.Range(1,5).Select(index => new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(index)), Random.Shared.Next(-20,55), summaries[Random.Shared.Next(summaries.Length)])).ToArray();
 return forecast;
}).WithName("GetWeatherForecast");

app.Run();

public partial class Program { }

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
 public int TemperatureF =>32 + (int)(TemperatureC /0.5556);
}
