using System.Security.Claims;
using System.Text.Json;
using Budget.Web.Components.Account.Pages;
using Budget.Web.Components.Account.Pages.Manage;
using Budget.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            // Login via HTTP POST so cookies can be written safely
            accountGroup.MapPost("/Login", async (
                HttpContext context,
                [FromServices] SignInManager<BudgetUser> signInManager,
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] string? rememberMe,
                [FromForm] string? returnUrl) =>
            {
                var persistent = string.Equals(rememberMe, "true", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(rememberMe, "on", StringComparison.OrdinalIgnoreCase);
                var result = await signInManager.PasswordSignInAsync(email, password, persistent, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var dest = GetSafeRedirect(returnUrl);
                    return TypedResults.LocalRedirect(dest);
                }
                if (result.RequiresTwoFactor)
                {
                    var dest = UriHelper.BuildRelative(
                        context.Request.PathBase,
                        "/Account/LoginWith2fa",
                        QueryString.Create(new[]
                        {
                            new KeyValuePair<string, StringValues>("ReturnUrl", returnUrl ?? "/"),
                            new KeyValuePair<string, StringValues>("RememberMe", persistent.ToString())
                        }));
                    return TypedResults.LocalRedirect(dest);
                }
                if (result.IsLockedOut)
                {
                    return TypedResults.LocalRedirect("~/Account/Lockout");
                }

                // Back to login with status
                var failed = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Login",
                    QueryString.Create("status", "Error: Invalid login attempt."));
                return TypedResults.LocalRedirect(failed);
            });

            accountGroup.MapPost("/PerformExternalLogin", (
                HttpContext context,
                [FromServices] SignInManager<BudgetUser> signInManager,
                [FromForm] string provider,
                [FromForm] string returnUrl) =>
            {
                IEnumerable<KeyValuePair<string, StringValues>> query = [
                    new("ReturnUrl", returnUrl),
                    new("Action", ExternalLogin.LoginCallbackAction)];

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/ExternalLogin",
                    QueryString.Create(query));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return TypedResults.Challenge(properties, [provider]);
            });

            accountGroup.MapPost("/Logout", async (
                ClaimsPrincipal user,
                SignInManager<BudgetUser> signInManager,
                [FromForm] string returnUrl) =>
            {
                await signInManager.SignOutAsync();
                // Always go home after logout
                return TypedResults.LocalRedirect("~/");
            });

            // Support GET-based logout to work with navigation links. Always go home.
            accountGroup.MapGet("/Logout", async (
                SignInManager<BudgetUser> signInManager,
                [FromQuery] string? returnUrl) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.LocalRedirect("~/");
            });

            var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

            manageGroup.MapPost("/LinkExternalLogin", async (
                HttpContext context,
                [FromServices] SignInManager<BudgetUser> signInManager,
                [FromForm] string provider) =>
            {
                // Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Manage/ExternalLogins",
                    QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
                return TypedResults.Challenge(properties, [provider]);
            });

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] UserManager<BudgetUser> userManager,
                [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
                }

                var userId = await userManager.GetUserIdAsync(user);
                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download
                var personalData = new Dictionary<string, string>();
                var personalDataProps = typeof(BudgetUser).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
                }

                personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
                var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
                return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
            });

            return accountGroup;

            static string GetSafeRedirect(string? returnUrl)
            {
                if (string.IsNullOrWhiteSpace(returnUrl))
                    return "~/";
                if (returnUrl.Contains("/Account/", StringComparison.OrdinalIgnoreCase))
                    return "~/";
                if (Uri.IsWellFormedUriString(returnUrl, UriKind.Absolute))
                    return "~/";
                return "~" + (returnUrl.StartsWith('/') ? returnUrl : "/" + returnUrl);
            }
        }
    }
}
