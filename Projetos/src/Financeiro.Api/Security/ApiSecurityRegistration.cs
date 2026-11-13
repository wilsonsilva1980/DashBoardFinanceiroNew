using Financeiro.Application.Access;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Identity;
using Financeiro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace Financeiro.Api.Security;

public static class ApiSecurityRegistration
{
    public static IServiceCollection AddFinancialSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestActor, HttpRequestActor>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<PermissionEvaluator>();
        services.AddScoped<AccessManagementService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<FinanceiroDbContext>().AddSignInManager().AddDefaultTokenProviders();
        services.Configure<IdentityOptions>(configuration.GetSection("Identity"));
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddCookie(IdentityConstants.ApplicationScheme, options =>
        {
            options.Cookie.Name = "__Host-financeiro-auth";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = CookieSessionValidation.ValidateAsync,
                OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; },
                OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; }
            };
        });
        services.AddAuthorization(options => options.AddPolicy("FinancialScope", policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            context.Resource is HttpContext http && http.RequestServices.GetRequiredService<FinancialScope>().IsAuthorized)));
        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionCatalog.All)
                options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));
        });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-financeiro-csrf";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddControllers(options => options.Filters.Add<BrowserMutationFilter>());
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Financeiro API";
                document.Info.Version = "v1";
                document.Info.Description = "First-party cookie authentication. Obtain a request token with GET /api/v1/auth/csrf, then send X-CSRF-TOKEN on browser mutations. Refresh the CSRF token after login/logout. Cookies require HTTPS; cross-origin credentials are not enabled.";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["cookieAuth"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Cookie, Name = "__Host-financeiro-auth" },
                    ["csrf"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "X-CSRF-TOKEN", Description = "Request token returned by /api/v1/auth/csrf; never use the HttpOnly cookie as a header value." }
                };
                document.Components.Examples = new Dictionary<string, IOpenApiExample>
                {
                    ["pageResult"] = new OpenApiExample
                    {
                        Summary = "Resposta paginada",
                        Description = "Coleções retornam itens e metadados de paginação; pageSize máximo é 100.",
                        Value = JsonNode.Parse("{\"items\":[],\"total\":0,\"page\":1,\"pageSize\":25}")
                    },
                    ["problemDetails"] = new OpenApiExample
                    {
                        Summary = "Erro de validação",
                        Description = "Erros são retornados como application/problem+json e incluem o identificador de correlação.",
                        Value = JsonNode.Parse("{\"type\":\"about:blank\",\"title\":\"Dados inválidos.\",\"status\":400,\"traceId\":\"00-...\",\"correlationId\":\"00-...\"}")
                    }
                };
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var requirement = new OpenApiSecurityRequirement();
                if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
                    requirement[new OpenApiSecuritySchemeReference("cookieAuth", context.Document)] = [];
                if (context.Description.HttpMethod is "POST" or "PUT" or "PATCH" or "DELETE")
                    requirement[new OpenApiSecuritySchemeReference("csrf", context.Document)] = [];
                if (requirement.Count > 0) operation.Security = [requirement];
                return Task.CompletedTask;
            });
        });
        return services;
    }
}
