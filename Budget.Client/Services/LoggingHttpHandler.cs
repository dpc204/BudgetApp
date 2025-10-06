using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Budget.Client.Services;

public sealed class LoggingHttpHandler(ILogger<LoggingHttpHandler> logger) : DelegatingHandler
{
  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    var stopwatch = Stopwatch.StartNew();
    var requestId = Guid.NewGuid().ToString("N")[..8];

    logger.LogInformation(
      "[{RequestId}] ? {Method} {Uri}",
      requestId,
      request.Method,
      request.RequestUri);

    if (request.Content is not null && logger.IsEnabled(LogLevel.Debug))
    {
      var content = await request.Content.ReadAsStringAsync(cancellationToken);
      logger.LogDebug("[{RequestId}] Request Body: {Body}", requestId, content);
    }

    HttpResponseMessage response;
    try
    {
      response = await base.SendAsync(request, cancellationToken);
      stopwatch.Stop();

      logger.LogInformation(
        "[{RequestId}] ? {StatusCode} {ReasonPhrase} in {ElapsedMs}ms",
        requestId,
        (int)response.StatusCode,
        response.ReasonPhrase,
        stopwatch.ElapsedMilliseconds);

      if (logger.IsEnabled(LogLevel.Debug) && response.Content is not null)
      {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug("[{RequestId}] Response Body: {Body}", requestId, responseContent);
      }

      return response;
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      logger.LogError(
        ex,
        "[{RequestId}] ? Request failed after {ElapsedMs}ms",
        requestId,
        stopwatch.ElapsedMilliseconds);
      throw;
    }
  }
}
