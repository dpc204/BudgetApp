using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Budget.DB
{
  public class EnvelopeDto
  {
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public CategoryDto Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Balance { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
   
  }
}