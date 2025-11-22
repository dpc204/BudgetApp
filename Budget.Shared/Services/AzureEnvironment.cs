// In ServiceDefaults or a shared configuration class
namespace Budget.Shared.Services;

public static class AzureEnvironment
{
  public static bool IsRunningOnAzure => 
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
    
  public static string? InstanceId => 
    Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
    
  public static string? AppName => 
    Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ?? 
    Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
}