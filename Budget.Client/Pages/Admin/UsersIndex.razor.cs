using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;

namespace Budget.Client.Pages.Admin;

public partial class UsersIndex
{
  [Inject] private IBudgetMaintApiClient MaintApi { get; set; } = null!;
  [Inject] private IDialogService DialogService { get; set; } = null!;
  [Inject] private ISnackbar Snackbar { get; set; } = null!;

  private List<UserDto> Users { get; set; } = [];
  private bool IsLoading { get; set; }
  private string? ErrorMessage { get; set; }

  protected override async Task OnInitializedAsync()
  {
    await LoadUsers();
  }

  private async Task LoadUsers()
  {
    IsLoading = true;
    ErrorMessage = null;

    try
    {
      var users = await MaintApi.GetUsersAsync();
      Users = users.ToList();
    }
    catch (Exception ex)
    {
      ErrorMessage = $"Failed to load users: {ex.Message}";
    }
    finally
    {
      IsLoading = false;
    }
  }

  private async Task OpenEditDialog(UserDto user)
  {
    var parameters = new DialogParameters<UserEditDialog>
    {
      { x => x.UserId, user.Id },
      { x => x.Email, user.Email },
      { x => x.FirstName, user.FirstName },
      { x => x.LastName, user.LastName },
      { x => x.FamilyId, user.FamilyId }
    };

    var options = new DialogOptions
    {
      CloseButton = true,
      MaxWidth = MaxWidth.Small,
      FullWidth = true
    };

    var dialog = await DialogService.ShowAsync<UserEditDialog>("Edit User", parameters, options);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
      await LoadUsers();
      Snackbar.Add("User updated successfully", Severity.Success);
    }
  }

  private async Task OpenRoleAssignmentDialog(UserDto user)
  {
    var parameters = new DialogParameters<UserRoleDialog>
    {
      { x => x.UserId, user.Id },
      { x => x.UserName, $"{user.FirstName} {user.LastName}" },
      { x => x.UserEmail, user.Email }
    };

    var options = new DialogOptions
    {
      CloseButton = true,
      MaxWidth = MaxWidth.Medium,
      FullWidth = true
    };

    var dialog = await DialogService.ShowAsync<UserRoleDialog>("Manage User Roles", parameters, options);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
      await LoadUsers();
      Snackbar.Add("User roles updated successfully", Severity.Success);
    }
  }
}

