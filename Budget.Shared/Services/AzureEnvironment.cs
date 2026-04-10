// In ServiceDefaults or a shared configuration class
namespace Budget.Shared.Services;

public static class AzureEnvironment
{
  /// <summary>
  /// Detects if the application is running on Azure (App Service or Container Apps)
  /// </summary>
  public static bool IsRunningOnAzure =>
    IsRunningOnAppService ||
    IsRunningOnContainerApps ||
    IsRunningOnAzureVirtualMachine;

  /// <summary>
  /// Detects if running on Azure App Service
  /// </summary>
  public static bool IsRunningOnAppService =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));

  /// <summary>
  /// Detects if running on Azure Container Apps
  /// </summary>
  public static bool IsRunningOnContainerApps =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONTAINER_APP_NAME")) ||
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION"));

  /// <summary>
  /// Detects if running on Azure Virtual Machine (via Azure Instance Metadata Service)
  /// </summary>
  public static bool IsRunningOnAzureVirtualMachine =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP"));

  public static string? InstanceId =>
    Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ??
    Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");

  public static string? AppName =>
    Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") ??
    Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

  /// <summary>
  /// Gets the Azure hosting environment type
  /// </summary>
  public static string HostingEnvironment
  {
    get
    {
      if(IsRunningOnContainerApps) return "Azure Container Apps";
      if(IsRunningOnAppService) return "Azure App Service";
      if(IsRunningOnAzureVirtualMachine) return "Azure Virtual Machine";
      return "Local/Unknown";
    }
  }
}