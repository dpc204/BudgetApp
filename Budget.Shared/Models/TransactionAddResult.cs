namespace Budget.Shared.Models
{
  public class EnvelopeDeltas : List<EnvelopeUpdate>
  {

  }

  public record EnvelopeUpdate(int EnvelopeId, decimal EnvelopeDelta);
}
