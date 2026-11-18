using Financeiro.Domain.Entities;
using Financeiro.Application.Access;
using Financeiro.Infrastructure.Access;
using System.Reflection;
using Financeiro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Persistence;

public sealed class FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options, FinancialScope? financialScope = null)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private FinancialScope _scope = financialScope ?? new FinancialScope();

    public static FinanceiroDbContext CreateForMaintenance(DbContextOptions<FinanceiroDbContext> options) => new(options, FinancialScope.ForMaintenance());

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<FinancialSpace> FinancialSpaces => Set<FinancialSpace>();
    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
    public DbSet<SpaceMember> SpaceMembers => Set<SpaceMember>();
    public DbSet<SpacePreferences> SpacePreferencess => Set<SpacePreferences>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Category> Categorys => Set<Category>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<CardBrand> CardBrands => Set<CardBrand>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<AccountPosition> AccountPositions => Set<AccountPosition>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Obligation> Obligations => Set<Obligation>();
    public DbSet<PayableTitle> PayableTitles => Set<PayableTitle>();
    public DbSet<PlanningEntry> PlanningEntrys => Set<PlanningEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<HistoricalSettlement> HistoricalSettlements => Set<HistoricalSettlement>();
    public DbSet<TitleAdjustment> TitleAdjustments => Set<TitleAdjustment>();
    public DbSet<SettlementAgreement> SettlementAgreements => Set<SettlementAgreement>();
    public DbSet<AgreementTitle> AgreementTitles => Set<AgreementTitle>();
    public DbSet<Recurrence> Recurrences => Set<Recurrence>();
    public DbSet<RecurrencePrice> RecurrencePrices => Set<RecurrencePrice>();
    public DbSet<CardInvoice> CardInvoices => Set<CardInvoice>();
    public DbSet<InvoicePurchase> InvoicePurchases => Set<InvoicePurchase>();
    public DbSet<SettlementScenario> SettlementScenarios => Set<SettlementScenario>();
    public DbSet<ScenarioTitle> ScenarioTitles => Set<ScenarioTitle>();
    public DbSet<TravelBudget> TravelBudgets => Set<TravelBudget>();
    public DbSet<TravelItem> TravelItems => Set<TravelItem>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<ItemConversion> ItemConversions => Set<ItemConversion>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    public DbSet<SpaceRole> SpaceRoles => Set<SpaceRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MemberRole> MemberRoles => Set<MemberRole>();
    public DbSet<OnboardingRecord> OnboardingRecords => Set<OnboardingRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<Annotation> Annotations => Set<Annotation>();
    public DbSet<InformedReference> InformedReferences => Set<InformedReference>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportSourceRecord> ImportSourceRecords => Set<ImportSourceRecord>();
    public DbSet<ImportLink> ImportLinks => Set<ImportLink>();
    public DbSet<ImportAnnotation> ImportAnnotations => Set<ImportAnnotation>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        // Local password/cookie authentication uses the portable Identity v1 schema.
        model.Ignore<IdentityUserPasskey<Guid>>();
        model.Ignore<IdentityPasskeyData>();
        model.Ignore<Entity>();
        model.Ignore<ScopedEntity>();
        model.ApplyConfigurationsFromAssembly(typeof(FinanceiroDbContext).Assembly);
        FinancialModelConventions.Apply(model, Database.IsNpgsql());
        foreach (var entity in model.Model.GetEntityTypes().Where(e => typeof(ScopedEntity).IsAssignableFrom(e.ClrType)).ToArray())
            typeof(FinanceiroDbContext).GetMethod(nameof(ConfigureScope), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entity.ClrType).Invoke(this, [model]);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConfigureScope<T>(ModelBuilder model) where T : ScopedEntity => model.Entity<T>().HasQueryFilter(x =>
        _scope.IsMaintenance || (_scope.IsAuthorized && x.TenantId == _scope.TenantId && x.SpaceId == _scope.SpaceId));

    internal IDisposable BeginProvisioning()
    {
        if (ChangeTracker.Entries<ScopedEntity>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Provisioning cannot include existing financial mutations.");
        var previous = _scope;
        _scope = FinancialScope.ForMaintenance();
        return new ScopeRestore(() => _scope = previous);
    }

    private sealed class ScopeRestore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    private void PrepareChanges()
    {
        foreach (var entry in ChangeTracker.Entries<ScopedEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity.TenantId == Guid.Empty || entry.Entity.SpaceId == Guid.Empty)
                throw new AccessDeniedException("Financial scope is required.");
            if (!_scope.IsMaintenance && (!_scope.IsAuthorized || entry.Entity.TenantId != _scope.TenantId || entry.Entity.SpaceId != _scope.SpaceId ||
                (entry.State != EntityState.Added && (entry.Property(x => x.TenantId).OriginalValue != _scope.TenantId || entry.Property(x => x.SpaceId).OriginalValue != _scope.SpaceId))))
                throw new AccessDeniedException("Financial record is outside the authorized scope.");
        }
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version = checked(entry.Property(x => x.Version).OriginalValue + 1);
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
