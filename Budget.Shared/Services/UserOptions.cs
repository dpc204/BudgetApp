namespace Budget.Shared.Services;

public class UserOptions
{
  public string UserId { get; set; } = string.Empty;
  
  private FillAmounts _fillAmountType;
  public FillAmounts FillAmountType
  {
    get => _fillAmountType;
    set
    {
      if (_fillAmountType != value)
      {
        _fillAmountType = value;
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