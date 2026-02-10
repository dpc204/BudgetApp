namespace Budget.Client.Services;

/// <summary>
/// Implementation of admin API client
/// </summary>
public sealed class AdminApiClient(HttpClient http, ILogger<AdminApiClient> logger) : Shared.Services.IAdminApiClient
{
  // Role management
  public async Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
  {
    var response = await http.GetFromJsonAsync<RolesResponse>("/api/admin/roles", cancellationToken);
    return response?.Roles ?? [];
  }

  public async Task<RoleDto?> GetRoleAsync(int id, CancellationToken cancellationToken = default)
    => await http.GetFromJsonAsync<RoleDto>($"/api/admin/roles/{id}", cancellationToken);

  public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
  {
    var response = await http.PostAsJsonAsync("/api/admin/roles", request, cancellationToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<RoleDto>(cancellationToken))!;
  }

  public async Task<RoleDto> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
  {
    var response = await http.PutAsJsonAsync($"/api/admin/roles/{id}", request, cancellationToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<RoleDto>(cancellationToken))!;
  }

  public async Task<bool> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
  {
    var response = await http.DeleteAsync($"/api/admin/roles/{id}", cancellationToken);
    return response.IsSuccessStatusCode;
  }

  // User management
  public async Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
  {
    var response = await http.GetFromJsonAsync<UsersResponse>("/api/admin/users", cancellationToken);
    return response?.Users ?? [];
  }

  public async Task<UserDetailDto?> GetUserAsync(int id, CancellationToken cancellationToken = default)
    => await http.GetFromJsonAsync<UserDetailDto>($"/api/admin/users/{id}", cancellationToken);

  public async Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
  {
    var response = await http.PutAsJsonAsync($"/api/admin/users/{id}", request, cancellationToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken))!;
  }

  // User-Role management
  public async Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
  {
    var response = await http.GetFromJsonAsync<UserRolesResponse>($"/api/admin/users/{userId}/roles", cancellationToken);
    return response?.Roles ?? [];
  }

  public async Task<AssignRoleResponse> AssignRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
  {
    var response = await http.PostAsync($"/api/admin/users/{userId}/roles/{roleId}", null, cancellationToken);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<AssignRoleResponse>(cancellationToken))!;
  }

  public async Task<bool> RemoveRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default)
  {
    var response = await http.DeleteAsync($"/api/admin/users/{userId}/roles/{roleId}", cancellationToken);
    return response.IsSuccessStatusCode;
  }

  // Response wrapper records
  private sealed record RolesResponse(List<RoleDto> Roles);
  private sealed record UsersResponse(List<UserDto> Users);
  private sealed record UserRolesResponse(int UserId, List<UserRoleDto> Roles);
}
