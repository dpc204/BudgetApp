using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Budget.Web.Components;
using Budget.Web.Components.Account;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();



builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Configuration.AddJsonFile("appsettings.json")
  .AddUserSecrets<Program>()
  .AddEnvironmentVariables();

//var connectionString = builder.Configuration["sqlbudgetconnection"]?? throw new InvalidOperationException("Connection string 'BudgetConnection' not found.");
var connectionString = "Data Source=fantumsqlserver.database.windows.net;Initial Catalog=BudgetDB;User ID=dpc;Password=fantum204!;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";


Debug.WriteLine("DEBUG"  +connectionString);
Console.WriteLine("Console "+connectionString);

builder.Configuration.AddAzureKeyVault(
  new Uri($"https://fantumkeyvault.vault.azure.net/"),
  new DefaultAzureCredential());

connectionString = builder.Configuration["budgetconnection"]?? throw new InvalidOperationException("Connection string 'BudgetConnection' not found.");


builder.Services.AddDbContext<ApplicationDbContext>(options =>
  options.UseSqlServer(connectionString)
    .EnableSensitiveDataLogging() // Optional: logs parameter values
    .LogTo(
      Console.WriteLine,
      LogLevel.Debug // Or LogLevel.Debug for more detail
    ));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true);

app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseDeveloperExceptionPage();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Budget.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();



app.Run();

