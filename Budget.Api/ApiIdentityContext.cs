using Budget.Shared;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Budget.Api;

public sealed class ApiIdentityContext(DbContextOptions<ApiIdentityContext> options) : IdentityDbContext<BudgetUser>(options)
{
}
