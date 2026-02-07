namespace Budget.Shared.Services;

public class UserOptions
{
  private string? _selectedCategoryType;
  private FillAmounts _fillAmountType;
  private int _previousImportAccount;

  public UserOptions()
  {
    _selectedCategoryType = "ALL";
  }

  [System.Text.Json.Serialization.JsonIgnore]
  public IUserAndOptions UserAndOptions { get; set; }

  public int UserId { get; set; }
  
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

  public int PreviousImportAccount
  {
    get
    {
      // Trigger lazy load before reading
      OnPropertyRead();
      return _previousImportAccount;
    }
    set
    {
      if (_previousImportAccount != value)
      {
        _previousImportAccount = value;
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