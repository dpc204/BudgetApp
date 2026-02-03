namespace Budget.Shared.Models;

public class UserInfoDto
{
  public int Id { get; set; }
  public string? Email { get; set; }
  public string? Name { get; set; }
  public int FamilyId { get; set; }
  public IList<string> Roles { get; set; } = [];
}