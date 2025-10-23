using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Budget.DB;
using Budget.Api.Features.Envelopes;
using Budget.Api.Features.Authentication;

namespace Budget.Api;

public static class BudgetApiHostExtensions
{
    // Registers the API services so they can be hosted in another process (e.g., the Blazor Server app)
    public static IServiceCollection AddBudgetApi(this IServiceCollection services, IConfigurationManager configuration, IHostEnvironment env)
    {
        // Carter + MediatR from API assembly
        services.AddCarter();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAll).Assembly));

        // Connection strings (reuse API assembly for configuration keys)
        var budgetConnectionString = Budget.Shared.Misc.SetupConfigurationSources(configuration, configuration, typeof(Program).Assembly, Budget.Shared.Misc.ConnectionStringType.Budget);
        var identityConnectionString = Budget.Shared.Misc.SetupConfigurationSources(configuration, configuration, typeof(Program).Assembly, Budget.Shared.Misc.ConnectionStringType.Identity);

        if (string.IsNullOrWhiteSpace(budgetConnectionString))
            throw new InvalidOperationException("Missing Budget DB connection string.");
        if (string.IsNullOrWhiteSpace(identityConnectionString))
            throw new InvalidOperationException("Missing Identity DB connection string.");

        var isDev = env.IsDevelopment() || env.IsEnvironment("Testing") || env.IsEnvironment("Test");

        // Register BudgetContext only if not already registered by the host app
        if (!services.Any(d => d.ServiceType == typeof(DbContextOptions<BudgetContext>)))
        {
            services.AddDbContext<BudgetContext>(options =>
            {
                options.UseSqlServer(budgetConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "budget"));
                if (isDev)
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });
        }

        // If the host app already has an Identity stack (e.g., IdentityDBContext with roles), skip registering API Identity
        var hasRoleStore = services.Any(d => d.ServiceType == typeof(IRoleStore<IdentityRole>))
                        || services.Any(d => d.ImplementationType?.Name.Contains("RoleStore") == true);

        if (!hasRoleStore)
        {
            // API Identity context (separate from web identity)
            services.AddDbContext<ApiIdentityContext>(options =>
            {
                options.UseSqlServer(identityConnectionString);
                if (isDev)
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });

            services
                .AddIdentityCore<IdentityUser>(o => { o.User.RequireUniqueEmail = true; })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApiIdentityContext>()
                .AddSignInManager();
        }

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        var jwtOpt = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.SigningKey));

        // Add JWT bearer scheme without changing the host's default scheme (cookies)
        services.AddAuthentication().AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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

        services.AddAuthorization();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }

    // Maps the API endpoints (Carter) into the containing app endpoint pipeline
    public static IEndpointRouteBuilder MapBudgetApi(this IEndpointRouteBuilder endpoints, IHostEnvironment env)
    {
        // Ensure databases exist
        using (var scope = endpoints.ServiceProvider.CreateScope())
        {
            var apiIdentity = scope.ServiceProvider.GetService<ApiIdentityContext>();
            apiIdentity?.Database.EnsureCreated();
            var ctxOptions = scope.ServiceProvider.GetService<DbContextOptions<BudgetContext>>();
            if (ctxOptions is not null)
            {
                scope.ServiceProvider.GetRequiredService<BudgetContext>().Database.EnsureCreated();
            }
        }

        // Optionally expose a health endpoint and map Carter modules
        endpoints.MapGet("/healthz", () => Results.Ok("OK"));
        endpoints.MapCarter();

        return endpoints;
    }
}
