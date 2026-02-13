namespace Budget.Client.Tests.Playwright;

/// <summary>
/// Base class for Playwright tests with setup and teardown infrastructure
/// </summary>
public abstract class PlaywrightTestBase : IAsyncLifetime
{
  protected TestWebApplicationFactory Factory { get; private set; } = null!;
  protected IPlaywright PlaywrightInstance { get; private set; } = null!;
  protected IBrowser Browser { get; private set; } = null!;
  protected IBrowserContext Context { get; private set; } = null!;
  protected IPage Page { get; private set; } = null!;
  protected string BaseUrl { get; private set; } = null!;

  /// <summary>
  /// Initializes the test environment before each test
  /// </summary>
  public virtual async ValueTask InitializeAsync()
  {
    // TODO: For now, start Budget.Web or Budget.AppHost separately
    // and set the URL here. WebApplicationFactory integration can be completed later.
    BaseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_URL") ?? "https://localhost:7141";
    
    // Uncomment when WebApplicationFactory is fully configured:
    // Factory = new TestWebApplicationFactory();
    // var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    // BaseUrl = client.BaseAddress!.ToString().TrimEnd('/');

    // Install Playwright browsers if needed (run once)
    // Note: You may need to run "pwsh bin/Debug/net10.0/playwright.ps1 install" manually first
    PlaywrightInstance = await Microsoft.Playwright.Playwright.CreateAsync();

    // Launch browser in headless mode
    Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = false,
      SlowMo = 200 // Set to > 0 for debugging (milliseconds delay between actions)
    });

    // Create a new browser context
    Context = await Browser.NewContextAsync(new BrowserNewContextOptions
    {
      IgnoreHTTPSErrors = true,
      BaseURL = BaseUrl
    });

    // Create a new page
    Page = await Context.NewPageAsync();
  }

  /// <summary>
  /// Cleans up resources after each test
  /// </summary>
  public virtual async ValueTask DisposeAsync()
  {
    if (Page != null)
      await Page.CloseAsync();

    if (Context != null)
      await Context.CloseAsync();

    if (Browser != null)
      await Browser.CloseAsync();

    PlaywrightInstance?.Dispose();

    if (Factory != null)
      await Factory.DisposeAsync();
    
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Navigates to a specific path relative to the base URL
  /// </summary>
  /// <param name="path">The path to navigate to (e.g., "/fund")</param>
  protected async Task NavigateToAsync(string path)
  {
    await Page.GotoAsync($"{BaseUrl}{path}");
    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
  }

  /// <summary>
  /// Takes a screenshot for debugging purposes
  /// </summary>
  /// <param name="name">Name of the screenshot file</param>
  protected async Task TakeScreenshotAsync(string name)
  {
    await Page.ScreenshotAsync(new PageScreenshotOptions
    {
      Path = $"screenshots/{name}.png",
      FullPage = true
    });
  }
}
