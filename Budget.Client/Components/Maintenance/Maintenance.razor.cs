namespace Budget.Client.Components.Maintenance;

public partial class Maintenance : IDisposable
{
  protected string? Status { get; private set; }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}