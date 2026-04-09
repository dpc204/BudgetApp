using System;
using System.Collections.Generic;
using System.Text;

namespace Budget.Shared.Models
{
  public class EnvelopeDeltas : List<EnvelopeUpdate>
  {

  }

  public record EnvelopeUpdate(int EnvelopeId, decimal EnvelopeDelta);
}
