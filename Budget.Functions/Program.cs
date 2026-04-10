using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Budget.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()
  .ConfigureServices((context, services) =>
  {
    var config = context.Configuration;

    // Register Azure Blob Storage
    var blobEndpoint = config["AZURE_STORAGE_BLOB_ENDPOINT"];
    var tableEndpoint = config["AZURE_STORAGE_TABLE_ENDPOINT"];
    var storageConnectionString = config["AzureStorage__ConnectionString"];

    if(!string.IsNullOrWhiteSpace(blobEndpoint) && !string.IsNullOrWhiteSpace(tableEndpoint))
    {
      // Managed Identity on Azure
      var clientId = config["AZURE_CLIENT_ID"];
      var credential = string.IsNullOrWhiteSpace(clientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });

      services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(new Uri(blobEndpoint), credential));
      services.AddSingleton<TableServiceClient>(_ => new TableServiceClient(new Uri(tableEndpoint), credential));
    }
    else if(!string.IsNullOrWhiteSpace(storageConnectionString))
    {
      services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));
      services.AddSingleton(_ => new TableServiceClient(storageConnectionString));
    }
    else
    {
      // Development: Azurite local storage
      //services.AddSingleton(_ => new BlobServiceClient("UseDevelopmentStorage=true"));
      //services.AddSingleton(_ => new TableServiceClient("UseDevelopmentStorage=true"));   
      services.AddSingleton(_ => new BlobServiceClient("storageConnectionString"));
      services.AddSingleton(_ => new TableServiceClient("storageConnectionString"));
    }

    services.AddSingleton<BacpacBackupService>();
  })
  .Build();

await host.RunAsync();
