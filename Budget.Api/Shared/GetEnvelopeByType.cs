namespace Budget.Api.Shared
{
  public class GetEnvelopeByType
  {
    public static async Task<EnvelopeDto?> Get(BudgetContext db,EnvelopeTypes envType, CancellationToken cancellationToken)
    {
      var envelope = await db.Envelopes
        .AsNoTracking()
        .Include(env => env.Category)
        .Where(a => a.EnvelopeType == envType)
        .ProjectToType<EnvelopeDto>(TypeAdapterConfig<Envelope, EnvelopeDto>
          .NewConfig()
          .MaxDepth(2)
          .Config)
        .FirstOrDefaultAsync(cancellationToken);

      return envelope;
    }
  }
}
