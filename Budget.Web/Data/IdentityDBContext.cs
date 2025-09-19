using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Budget.Web.Data;

namespace Budget.Web.Data
{
    public class IdentityDBContext(DbContextOptions<IdentityDBContext> options) : IdentityDbContext<BudgetUser>(options)
    {
      
    }
}
