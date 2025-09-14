using System.Net.Http.Json;

namespace Budget.Client.Services;

public interface IBudgetApiClient
{
  Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<WeatherForecastDto>> GetWeatherForecastAsync(CancellationToken cancellationToken = default);
}

public sealed class BudgetApiClient(HttpClient http) : IBudgetApiClient
{
  public async Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    => await GetAsync<EnvelopeDto>("envelopes/getall", cancellationToken);

  public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    => await GetAsync<CategoryDto>("categories/getbyenvelopeid", cancellationToken);

  public async Task<IReadOnlyList<TransactionDto>> GetTransactionsByEnvelopeAsync(int envelopeId, CancellationToken cancellationToken = default)
    => await GetAsync<TransactionDto>($"transactions/{envelopeId}", cancellationToken);

  public async Task<IReadOnlyList<WeatherForecastDto>> GetWeatherForecastAsync(CancellationToken cancellationToken = default)
    => await GetAsync<WeatherForecastDto>("weatherforecast", cancellationToken);

  private async Task<IReadOnlyList<T>> GetAsync<T>(string relativeUrl, CancellationToken ct)
  {
    var result = await http.GetFromJsonAsync<List<T>>(relativeUrl, cancellationToken: ct);
    return result ?? [];
  }
}

public sealed record EnvelopeDto(int Id, string Name, decimal Balance, decimal Budget, int CategoryId, int SortOrder);
public sealed record CategoryDto(int Id, string Name, string Description, int SortOrder);
public sealed record TransactionDto(int TransactionId, int LineId, string Description, decimal Amount, DateTime Date);
public sealed record WeatherForecastDto(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
