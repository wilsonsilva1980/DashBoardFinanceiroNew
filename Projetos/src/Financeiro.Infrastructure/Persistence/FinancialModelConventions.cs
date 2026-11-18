using System.Text.RegularExpressions;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Persistence;

internal static partial class FinancialModelConventions
{
    public static void Apply(ModelBuilder model, bool postgreSql)
    {
        model.Entity<FinancialSpace>().HasAlternateKey(x => new { x.TenantId, x.Id });
        model.Entity<FinancialSpace>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TenantMember>().HasAlternateKey(x => new { x.TenantId, x.UserId });
        model.Entity<TenantMember>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TenantMember>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SpaceMember>().HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.UserId });
        model.Entity<SpaceMember>().HasOne<TenantMember>().WithMany().HasForeignKey(x => new { x.TenantId, x.UserId }).HasPrincipalKey(x => new { x.TenantId, x.UserId }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<MemberRole>().HasOne<SpaceMember>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.UserId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.UserId }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RolePermission>().HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<CreditCard>().HasOne<CardBrand>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.BrandId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OnboardingRecord>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OnboardingRecord>().HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<HistoricalSettlement>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AuditRecord>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Annotation>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<IdempotencyRecord>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ApplicationUser>().HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("\"normalized_email\" IS NOT NULL");
        model.Entity<UserSession>().ToTable("sessoes_usuario");
        model.Entity<UserSession>().HasIndex(x => new { x.UserId, x.ExpiresAt });
        model.Entity<AuditRecord>().HasIndex(x => new { x.TenantId, x.SpaceId, x.Instant });
        model.Entity<AuditRecord>().HasIndex(x => new { x.TenantId, x.SpaceId, x.Action, x.Instant });
        model.Entity<AuditRecord>().HasIndex(x => new { x.TenantId, x.SpaceId, x.UserId, x.Instant });
        model.Entity<AuditRecord>().HasIndex(x => new { x.TenantId, x.SpaceId, x.Entity, x.EntityId, x.Instant });
        model.Entity<AuditRecord>().HasIndex(x => new { x.TenantId, x.SpaceId, x.CorrelationId });
        model.Entity<UserSession>().HasKey(x => x.Id);
        model.Entity<UserSession>().Property(x => x.Id).ValueGeneratedNever();
        model.Entity<UserSession>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<UserSession>().HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.ActiveTenantId, x.ActiveSpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<PayableTitle>().HasIndex(x => new { x.TenantId, x.SpaceId, x.Period });
        model.Entity<PayableTitle>().HasIndex(x => new { x.TenantId, x.SpaceId, x.DueDate }).HasFilter("\"state\" <> 'Cancelled'");
        model.Entity<PayableTitle>().HasIndex(x => new { x.TenantId, x.SpaceId, x.ResponsibleId, x.Period });
        model.Entity<Payment>().HasIndex(x => new { x.TenantId, x.SpaceId, x.PaymentDate });
        model.Entity<PaymentAllocation>().HasIndex(x => new { x.TenantId, x.SpaceId, x.TitleId });
        model.Entity<AccountPosition>().HasIndex(x => new { x.TenantId, x.SpaceId, x.AccountId, x.PositionDate });
        model.Entity<InvoicePurchase>().HasIndex(x => new { x.TenantId, x.SpaceId, x.PurchaseObligationId, x.OccurrenceKey }).IsUnique().HasFilter("\"purchase_obligation_id\" IS NOT NULL");

        foreach (var entity in model.Model.GetEntityTypes().ToArray())
        {
            var builder = model.Entity(entity.ClrType);
            foreach (var property in entity.GetProperties().ToArray())
            {
                var column = property.Name == "SpaceId" ? "espaco_id" : SnakeCase(property.Name);
                property.SetColumnName(column);
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type == typeof(string) && property.GetMaxLength() is null)
                    property.SetMaxLength(property.Name is "RawContentJson" or "DerivedValuesJson" ? int.MaxValue :
                        property.Name is "ChangesJson" or "Text" or "Content" ? 8000 :
                        property.Name.Contains("Description") || property.Name.Contains("Reason") || property.Name == "Justification" ? 1000 : 200);
                if (type == typeof(decimal))
                {
                    property.SetPrecision(property.Name == "Rate" ? 20 : 19);
                    property.SetScale(property.Name == "Rate" ? 8 : 2);
                }
                if (type == typeof(DateOnly)) property.SetColumnType("date");
                if (type.IsEnum)
                {
                    builder.Property(property.Name).HasConversion<string>().HasMaxLength(30);
                    Check(entity.ClrType.Name + "_" + column + "_enum", $"\"{column}\" IN ({string.Join(",", Enum.GetNames(type).Select(x => $"'{x}'"))})");
                }
                if (property.Name is "Currency" or "SourceCurrency" or "DestinationCurrency")
                {
                    property.SetMaxLength(3);
                    Check(entity.ClrType.Name + "_" + column + "_currency", $"\"{column}\" IN ('BRL','USD','EUR','CLP','ARS','GBP','JPY','CAD','AUD','CHF','CNY','UYU','PYG','BOB','PEN','COP','MXN')");
                }
                if (property.Name is "DueDay" or "ClosingDay" or "NominalDay") Check(entity.ClrType.Name + "_" + column, $"\"{column}\" BETWEEN 1 AND 31");
                if (property.Name is "InstallmentCount" or "InstallmentNumber" or "RecurrenceHorizon" or "Version") Check(entity.ClrType.Name + "_" + column, $"\"{column}\" > 0");
                if (property.Name == "Period") Check(entity.ClrType.Name + "_period", postgreSql ? "EXTRACT(DAY FROM \"period\") = 1" : "DATEPART(day, \"period\") = 1");
                if (type == typeof(decimal) && property.Name is not ("Balance" or "SavingsBalance"))
                {
                    var positive = entity.ClrType == typeof(Payment) || entity.ClrType == typeof(PaymentAllocation) || property.Name == "Rate";
                    Check(entity.ClrType.Name + "_" + column + "_amount", $"\"{column}\" {(positive ? ">" : ">=")} 0");
                }
                if (entity.ClrType.IsAssignableTo(typeof(Entity)) && property.Name == "CreatedBy")
                    builder.HasOne(typeof(ApplicationUser)).WithMany().HasForeignKey("CreatedBy").OnDelete(DeleteBehavior.Restrict);
            }
            void Check(string name, string sql) => builder.ToTable(entity.GetTableName()!, table => table.HasCheckConstraint("CK_" + (entity.ClrType == typeof(CardBrand) ? "Scoped" + name : name), sql));
        }
        model.Entity<RecurrencePrice>().ToTable("recorrencia_vigencias", b => b.HasCheckConstraint("CK_recurrence_interval", "\"interval_months\" IN (1,3,12)"));
        model.Entity<RecurrencePrice>().ToTable("recorrencia_vigencias", b => b.HasCheckConstraint("CK_recurrence_dates", "\"end_date\" IS NULL OR \"end_date\" >= \"start_date\""));
        model.Entity<Tenant>().ToTable("tenants", b => b.HasCheckConstraint("CK_tenant_status", "\"status\" IN ('Active','Suspended','Closed')"));
        foreach (var type in new[] { typeof(Annotation), typeof(InformedReference) })
            model.Entity(type).ToTable(model.Entity(type).Metadata.GetTableName()!, b => b.HasCheckConstraint("CK_" + type.Name + "_one_target", "(CASE WHEN \"title_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"obligation_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"travel_id\" IS NULL THEN 0 ELSE 1 END) = 1"));
    }

    private static string SnakeCase(string value) => Boundary().Replace(value, "$1_$2").ToLowerInvariant();
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex Boundary();
}
