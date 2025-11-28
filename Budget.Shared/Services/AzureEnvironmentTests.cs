using Budget.Shared.Services;

namespace Budget.Shared.Tests;

/// <summary>
/// Test helper to verify Azure environment detection
/// </summary>
public static class AzureEnvironmentTests
{
    /// <summary>
    /// Display current Azure environment detection status
    /// </summary>
    public static void DisplayEnvironmentInfo()
    {
        Console.WriteLine("=== Azure Environment Detection ===");
        Console.WriteLine($"IsRunningOnAzure: {AzureEnvironment.IsRunningOnAzure}");
        Console.WriteLine($"IsRunningOnContainerApps: {AzureEnvironment.IsRunningOnContainerApps}");
        Console.WriteLine($"IsRunningOnAppService: {AzureEnvironment.IsRunningOnAppService}");
        Console.WriteLine($"IsRunningOnAzureVirtualMachine: {AzureEnvironment.IsRunningOnAzureVirtualMachine}");
        Console.WriteLine($"HostingEnvironment: {AzureEnvironment.HostingEnvironment}");
        Console.WriteLine($"AppName: {AzureEnvironment.AppName ?? "N/A"}");
        Console.WriteLine($"InstanceId: {AzureEnvironment.InstanceId ?? "N/A"}");
        Console.WriteLine();
        
        Console.WriteLine("=== Environment Variables ===");
        DumpEnvironmentVariable("CONTAINER_APP_NAME");
        DumpEnvironmentVariable("CONTAINER_APP_REVISION");
        DumpEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");
        DumpEnvironmentVariable("WEBSITE_INSTANCE_ID");
        DumpEnvironmentVariable("WEBSITE_SITE_NAME");
        DumpEnvironmentVariable("AZURE_CLIENT_ID");
        DumpEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Console.WriteLine("================================");
    }
    
    private static void DumpEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Console.WriteLine($"{name}: {value ?? "(not set)"}");
    }
}
