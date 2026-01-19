namespace Budget.Shared.Models
{
  /// <summary>
  /// Represents a budget envelope that holds budget allocations, balances, and spending tracking for a specific category.
  /// </summary>
  public class EnvelopeDto
  {
    /// <summary>
    /// Gets or sets the unique identifier for the envelope.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the category this envelope belongs to.
    /// </summary>
    public string CategoryId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the associated category details.
    /// </summary>
    public CategoryDto Category { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the display name of the envelope.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the budgeted amount for this envelope. Null indicates no budget is set.
    /// </summary>
    public decimal? Budget { get; set; }
    
    /// <summary>
    /// Gets or sets the current balance of the envelope after transactions.
    /// </summary>
    public decimal Balance { get; set; }
    
    /// <summary>
    /// Gets or sets the type of envelope (e.g., Standard, Funding).
    /// </summary>
    public EnvelopeTypes 
EnvelopeType { get; set; }
    
    /// <summary>
    /// Gets or sets the amount to fund this envelope with during the funding process.
    /// </summary>
    public decimal FundAmount { get; set; }
    
    /// <summary>
    /// Gets or sets an optional description providing additional details about the envelope's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display order for sorting envelopes in the UI.
    /// </summary>
    public int SortOrder { get; set; }
   
  }

  public class EnvelopeUpdateDto
  {
    public int Id { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public EnvelopeTypes EnvelopeType { get; set; }
    public decimal FundAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

  }


}