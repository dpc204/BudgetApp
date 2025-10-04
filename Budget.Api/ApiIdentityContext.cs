using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Budget.Api;

public sealed class ApiIdentityContext(DbContextOptions<ApiIdentityContext> options) : IdentityDbContext<IdentityUser>(options)
{
}
