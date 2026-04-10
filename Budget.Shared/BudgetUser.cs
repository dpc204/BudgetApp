namespace Budget.Shared;

// Add profile data for application users by adding properties to the ApplicationUser class
public class BudgetUser : IdentityUser
{
  public string UserInitials { get; set; } = String.Empty;
  public int FamilyId { get; set; } = 1; // Default to family 1
}
