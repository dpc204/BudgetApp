namespace Budget.Shared.Models;

public class UserInfoDto
{
  public string? Id { get; set; }
  public string? Email { get; set; }
  public string? Name { get; set; }
  public IList<string> Roles { get; set; } = new List<string>();
}