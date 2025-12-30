namespace Budget.Shared.Services;

public class UserOptions
{
  public string UserId { get; set; } = string.Empty;
  
  public FillAmounts FillAmountType
  {
    get => field;
    set
    {
      if (field != value)
      {
        field = value;
        OnPropertyChanged();
      }
    }
  }

  public string? SelectedCategoryType
  {
    get => field;
    set
    {
      if (field != value)
      {
        field = value;
        OnPropertyChanged();
      }
    }
  }

  /// <summary>
  /// Event raised when any property changes
  /// </summary>
  public event Action? PropertyChanged;

  private void OnPropertyChanged()
  {
    PropertyChanged?.Invoke();
  }
}