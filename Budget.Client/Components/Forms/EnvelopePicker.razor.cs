namespace Budget.Client.Components.Forms;

/// <summary>
/// A reusable envelope picker component that provides autocomplete functionality for selecting envelopes.
/// </summary>
public partial class EnvelopePicker(IEnvelopesApiClient envelopesApi, ICategoriesApiClient categoriesApi) : ComponentBase
{
  /// <summary>
  /// Gets or sets the currently selected envelope.
  /// </summary>
  [Parameter]
  public EnvelopeIdName? Value { get; set; }

  /// <summary>
  /// Gets or sets the event callback that is invoked when the selected envelope changes.
  /// </summary>
  [Parameter]
  public EventCallback<EnvelopeIdName?> ValueChanged { get; set; }

  /// <summary>
  /// Gets or sets the placeholder text displayed when no envelope is selected.
  /// </summary>
  [Parameter]
  public string Placeholder { get; set; } = "Search Envelope!!!";

  /// <summary>
  /// Gets or sets whether the picker is disabled.
  /// </summary>
  [Parameter]
  public bool Disabled { get; set; }

  /// <summary>
  /// Gets or sets the display format mode. When true, shows "Category - Envelope". When false, shows only "Envelope".
  /// </summary>
  [Parameter]
  public bool ShowCategoryInDisplay { get; set; } = true;

  /// <summary>
  /// Gets or sets the variant of the autocomplete component.
  /// </summary>
  [Parameter]
  public Variant Variant { get; set; } = Variant.Outlined;

  /// <summary>
  /// Gets or sets whether the component uses dense spacing.
  /// </summary>
  [Parameter]
  public bool Dense { get; set; } = true;

  /// <summary>
  /// Gets or sets the margin of the component.
  /// </summary>
  [Parameter]
  public Margin Margin { get; set; } = Margin.None;

  /// <summary>
  /// Gets or sets additional CSS classes to apply to the component.
  /// </summary>
  [Parameter]
  public string? Class { get; set; }

  /// <summary>
  /// Gets or sets additional inline styles to apply to the component.
  /// </summary>
  [Parameter]
  public string? Style { get; set; }

  /// <summary>
  /// Gets or sets the envelope types to include in the picker. Defaults to Standard and Income envelopes.
  /// </summary>
  [Parameter]
  public EnvelopeTypes[] IncludeEnvelopeTypes { get; set; } = [EnvelopeTypes.Standard, EnvelopeTypes.Income];

  private List<EnvelopeIdName> _availableEnvelopes = [];

  protected override async Task OnInitializedAsync()
  {
    await LoadEnvelopesAsync();
  }

  private async Task LoadEnvelopesAsync()
  {
    var envelopes = await envelopesApi.GetEnvelopesAsync();
    var categories = await categoriesApi.GetCategoriesAsync();

    var result = from e in envelopes
                 join c in categories on e.CategoryId equals c.CategoryId
                 where IncludeEnvelopeTypes.Contains(e.EnvelopeType)
                 select new EnvelopeIdName(e.Id, c.Name, e.Name, c.SortOrder, e.SortOrder);

    _availableEnvelopes = [.. result];
  }

  private async Task<IEnumerable<EnvelopeIdName>> SearchEnvelopesAsync(string? searchText, CancellationToken cancellationToken)
  {
    if(string.IsNullOrWhiteSpace(searchText))
    {
      return [.. _availableEnvelopes];
    }

    return
    [
      .. _availableEnvelopes.Where(e =>
        e.CategoryName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase) ||
        e.EnvelopeName.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)
      )
    ];
  }

  private string? GetDisplayName(EnvelopeIdName? envelope)
  {
    if(envelope is null)
      return null;

    return ShowCategoryInDisplay
      ? $"{envelope.CategoryName} - {envelope.EnvelopeName}"
      : envelope.EnvelopeName;
  }

  private async Task OnValueChangedAsync(EnvelopeIdName? selectedEnvelope)
  {
    Value = selectedEnvelope;
    await ValueChanged.InvokeAsync(selectedEnvelope);
  }
}
