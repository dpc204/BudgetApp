using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Budget.Shared;

namespace Budget.Api;

public sealed class ApiIdentityContext(DbContextOptions<ApiIdentityContext> options) : IdentityDbContext<BudgetUser>(options)
{
}
