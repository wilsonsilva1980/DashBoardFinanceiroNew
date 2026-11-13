namespace Financeiro.Api.Security;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = context.TraceIdentifier,
            ["RequestPath"] = context.Request.Path.Value ?? string.Empty
        }))
        {
            await next(context);
        }
    }
}
