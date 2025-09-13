using System;
using System.Diagnostics;
using System.Reflection;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace Budget.Shared;

public static class Misc
{
    public static string? SetupConfigurationSources(IConfigurationBuilder configBuilder, IConfiguration configuration, Assembly assembly)
    {
    configBuilder.AddJsonFile("appsettings.json");
    configBuilder.AddUserSecrets(assembly);
        configBuilder.AddEnvironmentVariables();

        configBuilder.AddAzureKeyVault(
            new Uri("https://fantumkeyvault.vault.azure.net/"),
            new DefaultAzureCredential());

        string? s;
        if (configuration["LocalBudgetConnection"] != null)
        {
            s = configuration["LocalBudgetConnection"];
        }
        else if (configuration["budgetconnection"] != null)
        {
            // For local dev, fall back to regular connection string if LocalBudgetConnection is not set
            s = configuration["budgetconnection"];
        }
        else
        {
            // For deployed environments, fall back to regular connection string if LocalBudgetConnection is not set
            s = configuration.GetConnectionString("BudgetConnection");
        }

        if (string.IsNullOrWhiteSpace(s))
            s = configuration["budgetconnection"] ??
                throw new InvalidOperationException("Connection string 'BudgetConnection' not found.");

        Console.WriteLine("Console ConnectionString:" + s);

        Debug.WriteLine("DEBUG " + s);
        Console.WriteLine("Console " + s);
        return s;
    }
}