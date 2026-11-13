namespace Financeiro.Domain;

/// <summary>Derived settlement status, kept distinct from the title lifecycle state.</summary>
public enum SettlementStatus { Open, Partial, Paid, Overdue }

/// <summary>
/// Derived view of a payable title: the balance comes from the original amount, adjustments,
/// confirmed allocations, and active historical settlements. Negative balances stay visible
/// as excess credit instead of being truncated to zero.
/// </summary>
public readonly record struct TitleBalance(decimal Balance, decimal SettledAmount, decimal ExcessCredit, SettlementStatus Status)
{
    public static TitleBalance Calculate(
        decimal originalAmount,
        decimal adjustmentAmount,
        decimal allocationAmount,
        decimal historicalSettlementAmount,
        DateOnly? dueDate,
        DateOnly asOf)
    {
        var settled = allocationAmount + historicalSettlementAmount;
        var balance = originalAmount + adjustmentAmount - settled;
        var excess = balance < 0 ? -balance : 0m;
        var status = balance <= 0 ? SettlementStatus.Paid
            : dueDate is { } due && due < asOf ? SettlementStatus.Overdue
            : settled > 0 ? SettlementStatus.Partial
            : SettlementStatus.Open;
        return new(balance, settled, excess, status);
    }
}
