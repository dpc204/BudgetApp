using Budget.Web.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Budget.Web.Data
{
    public class IdentityDBContext(DbContextOptions<IdentityDBContext> options) : IdentityDbContext<BudgetUser>(options)
    {
      
    }
}
