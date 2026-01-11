namespace Budget.Shared.Services;

public class UserOptions
{
  private string? _selectedCategoryType;
  private FillAmounts _fillAmountType;

  public UserOptions()
  {
    _selectedCategoryType = "ALL";
  }
  
  public string UserId { get; set; } = string.Empty;
  
  /// <summary>
  /// Event raised when any property is being read (before returning value).
  /// This allows lazy loading to be triggered automatically.
  /// </summary>
  public event Func<Task>? PropertyRead;
  
  public FillAmounts FillAmountType
  {
    get
    {
      // Trigger lazy load before reading
      OnPropertyRead();
      return _fillAmountType;
    }
    set
    {
      if (_fillAmountType != value)
      {
        _fillAmountType = value;
        OnPropertyChanged();
      }
    }
  }

  public string? SelectedCategoryType
  {
    get
    {
      // Trigger lazy load before reading
      OnPropertyRead();
      return _selectedCategoryType;
    }
    set
    {
      if (_selectedCategoryType != value)
      {
        _selectedCategoryType = value;
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
  
  private void OnPropertyRead()
  {
    // Fire and forget - don't block the getter
    // The async handler will load data in background
    _ = PropertyRead?.Invoke();
  }
}