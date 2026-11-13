using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.Security;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            CatalogNotFoundException => (404, "Cadastro não encontrado."),
            ReferencedCatalogException => (409, "Cadastro em uso."),
            CatalogNameConflictException => (409, "Nome de bandeira já utilizado."),
            CatalogValidationException => (400, "Dados de cadastro inválidos."),
            AccessDeniedException => (403, "Access denied."),
            RequestConflictException => (409, "Request conflicts with an earlier operation."),
            DbUpdateConcurrencyException => (409, "The record changed. Reload before retrying."),
            DbUpdateException => (409, "A relationship or financial constraint prevents this operation."),
            ArgumentException => (400, "Invalid request data."),
            _ => (500, "Unable to complete the request.")
        };
        // Never log payloads, SQL parameters, cookies, passwords, account identifiers, or exception messages.
        logger.LogWarning("Request failed with {ExceptionType} {StatusCode} {CorrelationId} {Path}",
            exception.GetType().Name, status, context.TraceIdentifier, context.Request.Path.Value);
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception is CatalogValidationException or ReferencedCatalogException or CatalogNameConflictException or RequestConflictException ? exception.Message : null,
            Extensions = { ["traceId"] = context.TraceIdentifier, ["correlationId"] = context.TraceIdentifier }
        };
        var written = await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
        if (!written) await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
