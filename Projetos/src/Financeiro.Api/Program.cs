using Financeiro.Application.Accounts;
using Financeiro.Application.Dashboard;
using Financeiro.Infrastructure.Accounts;
using Financeiro.Infrastructure.Dashboard;
using Financeiro.Application.Catalogs;
using Financeiro.Infrastructure.Catalogs;
using Financeiro.Application.Obligations;
using Financeiro.Infrastructure.Obligations;
using Financeiro.Application.Recurrences;
using Financeiro.Infrastructure.Recurrences;
using Financeiro.Application.Travel;
using Financeiro.Infrastructure.Travel;
using Financeiro.Application.Payments;
using Financeiro.Infrastructure.Payments;
using Financeiro.Api.Security;
using Financeiro.Infrastructure.Persistence;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFinancialDatabase(builder.Configuration);
builder.Services.AddFinancialSecurity(builder.Configuration);
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IObligationService, ObligationService>();
builder.Services.AddScoped<IRecurrenceService, RecurrenceService>();
builder.Services.AddScoped<ITravelService, TravelService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IHistoricalSettlementService, HistoricalSettlementService>();
builder.Services.AddScoped<IAgreementService, AgreementService>();
builder.Services.AddScoped<IAccountPositionService, AccountPositionService>();
builder.Services.AddScoped<IDashboardQueries, DashboardQueries>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();
app.UseMiddleware<CorrelationMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Financeiro v1"));
}
app.UseAuthentication();
app.UseMiddleware<FinancialContextMiddleware>();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program;
