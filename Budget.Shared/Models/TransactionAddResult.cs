using System;
using System.Collections.Generic;
using System.Text;

namespace Budget.Shared.Models
{
  public class TransactionAddResult
  {
    public TransactionDto SingleAddedTransaction { get; set; } 
    public List<EnvelopeUpdate> EnvelopeUpdates { get; set; } = [];
 

  }  
  public record EnvelopeUpdate(int EnvelopeId, decimal EnvelopeDelta);
}
