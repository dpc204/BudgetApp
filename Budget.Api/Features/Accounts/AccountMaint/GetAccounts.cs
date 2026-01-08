namespace Budget.Api.Features.Accounts.AccountMaint;

public static class GetAll
{
  public sealed record Query : IRequest<IEnumerable<Response>>;
  public sealed record Response(int Id, string Name, decimal Balance, BankAccount.AccountTypes AccountType);

  public class Handler(BudgetContext db, ILogger<Handler> logger) : IRequestHandler<Query, IEnumerable<Response>>
  {
    public async Task<IEnumerable<Response>> Handle(Query request, CancellationToken cancellationToken)
    {
      logger.LogInformation("******* In Account query");
      var result = await db.BankAccounts.AsNoTracking()
        .Select(a => new Response(a.Id, a.Name, a.Balance, a.AccountType))
        .ToListAsync(cancellationToken);
      return result;
    }
  }

  public class Endpoint : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGet("/accounts/maint/getall", async ([FromServices] ISender sender) =>
      {

        var result = await sender.Send(new Query());
        return Results.Ok(result);
      });
    }
  }
}
