using System;
using System.Collections.Generic;
using System.Text;

namespace Budget.DB
{
  public class CategoryDto
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<EnvelopeDto> Envelopes { get; set; } = [];

  
  }
}