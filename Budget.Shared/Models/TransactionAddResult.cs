using System;
using System.Collections.Generic;
using System.Text;

namespace Budget.Shared.Models
{
  public class TransactionAddResult
  {
    public List<EnvelopeUpdate> EnvelopeUpdates { get; set; } = [];
  }  

  public class EnvelopeUpdates : List<EnvelopeUpdate>
  {
    
  }

  public record EnvelopeUpdate(int EnvelopeId, decimal EnvelopeDelta);
}
