//namespace Budget.Client.Tests.Playwright;

///// <summary>
///// Sample Playwright tests demonstrating navigation, interaction, and assertions
///// </summary>
//public class SamplePlaywrightTests : PlaywrightTestBase
//{
//  [Fact(Skip = "Playwright tests not working")]

//  public async Task HomePage_Should_Load_Successfully()
//  {
//    // Arrange & Act
//    await NavigateToAsync("/");

//    // Assert
//    var title = await Page.TitleAsync();
//    title.Should().Be("Home");

//    // Verify the page loaded
//    await Page.WaitForSelectorAsync("body");
//    var bodyText = await Page.TextContentAsync("body");
//    bodyText.Should().NotBeNull();
//  }

//  [Fact(Skip = "Playwright tests not working")]

//  public async Task Navigation_Should_Work_With_Authentication()
//  {
//    // Arrange
//    await NavigateToAsync("/");

//    // Act - Navigate to Fund page
//    await NavigateToAsync("/fund");

//    // Assert - Verify we're on the Fund page
//    var url = Page.Url;
//    url.Should().Contain("/fund");

//    // Wait for the page to render
//    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

//    // Verify some content exists (adjust selector based on your actual Fund page)
//    var content = await Page.TextContentAsync("body");
//    content.Should().NotBeNull();
//  }

//  [Fact(Skip = "Playwright tests not working")]

//  public async Task MudBlazor_Components_Should_Render()
//  {
//    // Arrange & Act
//    await NavigateToAsync("/");

//    // Wait for MudBlazor to initialize
//    await Page.WaitForSelectorAsync(".mud-layout", new PageWaitForSelectorOptions
//    {
//      Timeout = 5000
//    });

//    // Assert - Check if MudBlazor layout is present
//    var mudLayout = await Page.QuerySelectorAsync(".mud-layout");
//    mudLayout.Should().NotBeNull("MudBlazor layout should be rendered");
//  }

  
//  [Fact(Skip = "Playwright tests not working")]
//  public async Task Authenticated_User_Information_Should_Be_Available()
//  {
//    // This test verifies that the mock authentication is working
//    // Arrange & Act
//    await NavigateToAsync("/");

//    // Wait for the page to fully load
//    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

//    Task.Delay(10000);

    
    
//    var signInButton=  Page.Locator("button:has-text(\"Sign in with\")");

//    signInButton.Should().Be(null);


//    var url = Page.Url;
//    url.Should().NotContain("/Account/Login", "User should be authenticated");
//    url.Should().NotContain("/Account/AccessDenied", "User should have access");
//  }

//  [Fact(Skip = "Example test - enable when you have a button to interact with")]
//  public async Task Sample_Button_Click_Interaction()
//  {
//    // This is an example of how to interact with buttons
//    // Arrange
//    await NavigateToAsync("/fund");

//    // Act - Click a button (adjust selector to match your actual UI)
//    await Page.ClickAsync("button:has-text('Fund Envelopes')");

//    // Wait for any resulting action
//    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

//    // Assert - Verify the expected outcome
//    var successMessage = await Page.QuerySelectorAsync(".mud-snackbar");
//    successMessage.Should().NotBeNull();
//  }

//  [Fact(Skip = "Example test - enable when you have form inputs to test")]
//  public async Task Sample_Form_Input_Interaction()
//  {
//    // This is an example of how to interact with form inputs
//    // Arrange
//    await NavigateToAsync("/fund");

//    // Act - Fill in a text field
//    await Page.FillAsync("input[placeholder='Amount']", "100");

//    // Act - Select from a dropdown
//    await Page.SelectOptionAsync("select[name='envelope']", "1");

//    // Act - Click submit button
//    await Page.ClickAsync("button[type='submit']");

//    // Assert - Verify the form submission result
//    await Page.WaitForSelectorAsync(".success-message");
//  }
//}
