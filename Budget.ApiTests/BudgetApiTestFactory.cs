using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Budget.DB;
using Budget.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Budget.ApiTests;

/// <summary>
/// Custom WebApplicationFactory for testing with in-memory database
/// </summary>
public class BudgetApiTestFactory : WebApplicationFactory<Budget.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL existing database-related service descriptors
            var descriptorsToRemove = services
                .Where(d => 
                    d.ServiceType.FullName != null && (
                        d.ServiceType.FullName.Contains("DbContext") ||
                        d.ServiceType.FullName.Contains("EntityFramework") ||
                        d.ServiceType == typeof(DbContextOptions<BudgetContext>) ||
                        d.ServiceType == typeof(DbContextOptions<ApiIdentityContext>) ||
                        d.ServiceType == typeof(BudgetContext) ||
                        d.ServiceType == typeof(ApiIdentityContext)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for BudgetContext
            services.AddDbContext<BudgetContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase("BudgetTestDb");
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            });

            // Add in-memory database for ApiIdentityContext
            services.AddDbContext<ApiIdentityContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase("IdentityTestDb");
            });
        });
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Clean up test databases
                using var scope = Services.CreateScope();
                var budgetDb = scope.ServiceProvider.GetRequiredService<BudgetContext>();
                budgetDb.Database.EnsureDeleted();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        base.Dispose(disposing);
    }
}
