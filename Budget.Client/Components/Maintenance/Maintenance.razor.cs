namespace Budget.Client.Components.Maintenance;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

public partial class Maintenance : IDisposable
{
  protected string? Status { get; private set; }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}