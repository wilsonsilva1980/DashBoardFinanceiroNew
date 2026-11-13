using System.Security.Claims;
using Financeiro.Infrastructure.Access;

namespace Financeiro.Api.Security;

public sealed class FinancialContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ScopeResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) &&
            Guid.TryParse(context.User.FindFirstValue("sid"), out var sessionId))
            await resolver.ResolveAsync(sessionId, userId, context.RequestAborted);
        await next(context);
    }
}
