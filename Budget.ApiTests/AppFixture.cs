using System.Diagnostics;

namespace Budget.ApiTests;

public class AppFixture : IAsyncLifetime
{
  private Process _app;
  public string BaseUrl { get; private set; } = "http://localhost:5005";

  public async ValueTask InitializeAsync()
  {
    _app = Process.Start(new ProcessStartInfo {
      FileName = "dotnet",
      Arguments = "run --project ../MyApp/MyApp.csproj --urls=http://localhost:5005",
      RedirectStandardOutput = true,
      UseShellExecute = false
    });

    // Optional: wait for readiness
    await Task.Delay(3000);
  }

  public ValueTask DisposeAsync()
  {
    _app?.Kill();
    return new ValueTask(Task.CompletedTask);
  }
}