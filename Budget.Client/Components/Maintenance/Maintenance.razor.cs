namespace Budget.Client.Components.Maintenance;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using Budget.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

public partial class Maintenance
{
 [Inject] private IBudgetApiClient ApiClient { get; set; } = default!;
 [Inject] private ISnackbar Snackbar { get; set; } = default!;
 [Inject] private NavigationManager Nav { get; set; } = default!;
 [Inject] private IJSRuntime JS { get; set; } = default!;

 protected bool Busy { get; private set; }
 protected string ButtonText { get; private set; } = "Backup Azure SQL Database";
 protected string? Status { get; private set; }

 protected async Task TriggerBackupAsync()
 {
 Busy = true;
 Status = null;
 ButtonText = "Preparing download...";
 try
 {
 // Ask server for the filename it will use
 using var http = new HttpClient { BaseAddress = new Uri(Nav.BaseUri) };
 var plan = await http.GetFromJsonAsync<BackupPlan>("/api/maintenance/backup-plan");
 var fileName = plan?.FileName ?? "backup.bacpac";

 // Start download with agreed filename
 var url = Nav.BaseUri.TrimEnd('/') + $"/api/maintenance/backup-download?name={Uri.EscapeDataString(fileName)}";
 await JS.InvokeVoidAsync("open", url, "_blank");
 Snackbar.Add("Backup export started. Your browser should download the .bacpac.", Severity.Success);
 Status = $"Backup: {fileName} downloaded";
 }
 catch (Exception ex)
 {
 Status = ex.Message;
 Snackbar.Add(Status, Severity.Error);
 }
 finally
 {
 Busy = false;
 ButtonText = "Backup Azure SQL Database";
 }
 }

 private sealed record BackupPlan(string FileName);
}