using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Financeiro.Api.Security;

public sealed class BrowserMutationFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method)) return;
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException)
        {
            var result = new BadRequestObjectResult(new ProblemDetails { Status = 400, Title = "Anti-forgery validation failed." });
            result.ContentTypes.Add("application/problem+json");
            context.Result = result;
        }
    }
}
