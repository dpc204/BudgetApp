using Budget.Web.Data;
using Microsoft.AspNetCore.Identity;

namespace Budget.Web.Components.Account
{
    internal sealed class IdentityUserAccessor(UserManager<BudgetUser> userManager, IdentityRedirectManager redirectManager)
    {
        public async Task<BudgetUser> GetRequiredUserAsync(HttpContext context)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            }

            return user;
        }
    }
}
