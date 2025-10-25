using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Budget.Web.Services;

// Forwards the current request's cookies (including Identity auth cookie) to the outgoing HttpClient request
public sealed class ForwardAuthCookiesHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
 protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
 {
 var ctx = httpContextAccessor.HttpContext;
 if (ctx is not null && ctx.Request.Headers.TryGetValue("Cookie", out var cookie))
 {
 // Forward all cookies to preserve auth context
 if (!request.Headers.Contains("Cookie"))
 {
 request.Headers.TryAddWithoutValidation("Cookie", (string[])cookie);
 }
 }
 return base.SendAsync(request, cancellationToken);
 }
}
