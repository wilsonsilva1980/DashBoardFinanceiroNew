using System.Security.Claims;
using Financeiro.Application.Access;

namespace Financeiro.Api.Security;

public sealed class HttpRequestActor(IHttpContextAccessor accessor) : IRequestActor
{
    public Guid? UserId => accessor.HttpContext?.User.Identity?.IsAuthenticated == true && Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string CorrelationId => accessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
