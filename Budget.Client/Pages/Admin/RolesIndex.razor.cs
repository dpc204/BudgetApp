using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;

namespace Budget.Client.Pages.Admin;

public partial class RolesIndex
{
  [Inject] private IBudgetMaintApiClient MaintApi { get; set; } = null!;
  [Inject] private IDialogService DialogService { get; set; } = null!;
  [Inject] private ISnackbar Snackbar { get; set; } = null!;

  private List<RoleDto> RolesList { get; set; } = [];
  private bool IsLoading { get; set; }
  private string? ErrorMessage { get; set; }

  protected override async Task OnInitializedAsync()
  {
    await LoadRoles();
  }

  private async Task LoadRoles()
  {
    IsLoading = true;
    ErrorMessage = null;

    try
    {
      var roles = await MaintApi.GetRolesAsync();
      RolesList = roles.ToList();
    }
    catch (Exception ex)
    {
      ErrorMessage = $"Failed to load roles: {ex.Message}";
    }
    finally
    {
      IsLoading = false;
    }
  }

  private async Task OpenCreateDialog()
  {
    var parameters = new DialogParameters<RoleDialog>
    {
      { x => x.IsEditMode, false }
    };

    var options = new DialogOptions
    {
      CloseButton = true,
      MaxWidth = MaxWidth.Small,
      FullWidth = true
    };

    var dialog = await DialogService.ShowAsync<RoleDialog>("Create Role", parameters, options);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
      await LoadRoles();
      Snackbar.Add("Role created successfully", Severity.Success);
    }
  }

  private async Task OpenEditDialog(RoleDto role)
  {
    var parameters = new DialogParameters<RoleDialog>
    {
      { x => x.IsEditMode, true },
      { x => x.RoleId, role.Id },
      { x => x.RoleName, role.Name },
      { x => x.RoleDescription, role.Description }
    };

    var options = new DialogOptions
    {
      CloseButton = true,
      MaxWidth = MaxWidth.Small,
      FullWidth = true
    };

    var dialog = await DialogService.ShowAsync<RoleDialog>("Edit Role", parameters, options);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
      await LoadRoles();
      Snackbar.Add("Role updated successfully", Severity.Success);
    }
  }

  private async Task DeleteRole(RoleDto role)
  {
    var confirmed = await DialogService.ShowMessageBox(
      "Confirm Delete",
      $"Are you sure you want to delete the role '{role.Name}'?",
      yesText: "Delete",
      cancelText: "Cancel");

    if (confirmed != true)
      return;

    try
    {
      var success = await MaintApi.DeleteRoleAsync(role.Id);
      
      if (success)
      {
        await LoadRoles();
        Snackbar.Add($"Role '{role.Name}' deleted successfully", Severity.Success);
      }
      else
      {
        Snackbar.Add("Failed to delete role", Severity.Error);
      }
    }
    catch (Exception ex)
    {
      Snackbar.Add($"Error deleting role: {ex.Message}", Severity.Error);
    }
  }
}

