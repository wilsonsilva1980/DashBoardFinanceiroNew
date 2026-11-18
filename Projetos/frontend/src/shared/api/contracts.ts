export interface SessionUser { id: string; displayName: string; email: string; tenantId: string | null; spaceId: string | null }
export interface FinancialSpace { tenantId: string; spaceId: string; name: string; currency: string }
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number }
export interface AuditEntry {
  id: string; instant: string; action: string; actionLabel: string; entity: string; entityId: string;
  userId: string | null; userDisplayName: string | null; correlationId: string; changes: Record<string, unknown>;
}
export interface CatalogEntry {
  id: string; version: number; active: boolean; name?: string; nature?: string | null; code?: string | null;
  institutionId?: string; holderId?: string | null; brandId?: string | null; type?: string | null;
  agency?: string | null; number?: string | null; checkDigit?: string | null; currency?: string;
  overdraftLimit?: string; dueDay?: number | null; closingDay?: number | null;
  creditLimit?: string | null; cashLimit?: string | null; foreignCashLimit?: string | null;
  pixLimit?: string | null; billLimit?: string | null; availableCredit?: string | null;
}
export interface AccountPosition {
  id: string; accountId: string; positionDate: string; currency: string; balance: string | null;
  savingsBalance: string | null; overdraftUsed: string | null; configuredOverdraftLimit: string; availableOverdraft: string | null; accountNumber: string | null; institutionName: string;
}
export type TitleState = 'Draft' | 'Forecast' | 'Effective' | 'Cancelled';
export type SettlementStatus = 'Open' | 'Partial' | 'Paid' | 'Overdue';
export interface Obligation {
  id: string; version: number; categoryId: string | null; responsibleId: string | null; description: string;
  creditor: string | null; kind: string; purchaseDate: string | null; purchaseDateContext: string | null;
  dueDay: number | null; installmentCount: number | null; currency: string; active: boolean;
}
export interface PayableTitle {
  id: string; version: number; obligationId: string; responsibleId: string | null; cardId: string | null;
  period: string; dueDate: string | null; installmentNumber: number | null; occurrenceKey: string;
  originalAmount: string; state: TitleState; cancelledAt: string | null;
  balance: string; settledAmount: string; excessCredit: string; settlementStatus: SettlementStatus;
}
export interface PlanningEntry { id: string; titleId: string; personId: string; amount: string; version: number }
export type PaymentState = 'Draft' | 'Confirmed' | 'Reversed';
export interface PaymentAllocation { titleId: string; amount: string }
export interface Payment {
  id: string; accountId: string; payerId: string; paymentDate: string; amount: string; currency: string;
  method: string; state: PaymentState; idempotencyKey: string; reversedAt: string | null; reversalReason: string | null;
  version: number; allocations: PaymentAllocation[];
}
export interface HistoricalSettlement {
  id: string; titleId: string; payerId: string | null; approvedBy: string; amount: string; effectiveDate: string | null;
  justification: string; previousReference: string | null; sourceStatus: string | null;
  revokedAt: string | null; revocationReason: string | null; version: number;
}
export interface AgreementTitle { titleId: string; nominalAmount: string; discount: string }
export interface Agreement {
  id: string; obligationId: string; proposalDate: string | null; validUntil: string | null; proposedAmount: string;
  state: 'Draft' | 'Confirmed'; paymentId: string | null; version: number; titles: AgreementTitle[];
}
export interface AgreementSimulation {
  id: string; obligationId: string; name: string; simulationDate: string; proposedAmount: string; notes: string | null;
  titles: AgreementTitle[];
}
export interface Recurrence {
  id: string; version: number; obligationId: string; anchorDate: string; nominalDay: number;
  endDate: string | null; active: boolean;
}
export interface RecurrencePrice {
  id: string; version: number; recurrenceId: string; startDate: string; endDate: string | null;
  cycleAmount: string; currency: string; intervalMonths: number;
}
export interface RecurrenceGenerationResult { created: number; skipped: number; }
export interface CardInvoice {
  id: string; version: number; cardId: string; titleId: string; period: string;
  mode: string; reportedAmount: string | null; notes: string | null;
}
export interface InvoicePurchase {
  id: string; version: number; invoiceId: string; purchaseObligationId: string | null;
  description: string; amount: string; installmentNumber: number | null; occurrenceKey: string;
}

export interface TravelBudget { id: string; name: string; startDate: string | null; endDate: string | null; version: number }
export interface TravelItem { id: string; travelId: string; obligationId: string | null; categoryId: string | null; responsibleId: string | null; description: string; state: string; currency: string | null; baseAmount: string | null; fees: string | null; version: number }
export interface ExchangeRate { id: string; sourceCurrency: string; destinationCurrency: string; rateDate: string | null; dateContext: string | null; rate: string; source: string; version: number }
export interface ItemConversion { id: string; itemId: string; rateId: string | null; destinationCurrency: string; mode: string; reportedAmount: string | null; dateContext: string | null; appliedRate: string | null; appliedRateDate: string | null; appliedRateSource: string | null; version: number }
export interface TravelDiscrepancy { kind: string; message: string; amount: string | null; itemId: string | null; conversionId: string | null; obligationId: string | null }
export interface TravelBudgetReconciliation { budgetId: string; balance: string | null; discrepancies: TravelDiscrepancy[] }

export interface DashboardAmountState { effective: string; forecast: string; open: string; partial: string; paid: string; overdue: string }
export interface DashboardCategorySummary { categoryId: string | null; categoryName: string; amounts: DashboardAmountState }
export interface DashboardResponsibleSummary { responsibleId: string | null; responsibleName: string; amounts: DashboardAmountState }
export interface DashboardDueDateSummary { dueDate: string | null; label: string; amounts: DashboardAmountState }
export interface DashboardMonthlyEvolution { month: string; effective: string; forecast: string; open: string; paid: string; overdue: string }
export interface DashboardAccountPositionSummary { accountId: string; accountLabel: string; positionDate: string | null; balance: string | null; savingsBalance: string | null; overdraftUsed: string | null; configuredOverdraftLimit: string; availableOverdraft: string | null }
export interface DashboardReconciliationIndicator { kind: string; scope: string; message: string; amount: string | null; titleId: string | null; travelBudgetId: string | null }
export interface DashboardSummary {
  from: string; until: string; currency: string; income: string | null; totals: DashboardAmountState;
  monthlyEvolution: DashboardMonthlyEvolution[];
  byCategory: DashboardCategorySummary[];
  byResponsible: DashboardResponsibleSummary[];
  byDueDate: DashboardDueDateSummary[];
  positions: DashboardAccountPositionSummary[];
  reconciliationIndicators: DashboardReconciliationIndicator[];
}
export type ImportState = 'Received' | 'Analyzed' | 'AwaitingReview' | 'Confirmed' | 'Failed' | 'Cancelled';
export type ImportDecision = 'Pending' | 'Approved' | 'Rejected' | 'Ignored';
export type ImportSeverity = 'Info' | 'Warning' | 'Error';
export interface ImportAnalysisResult { batchId: string; state: ImportState; totalRows: number; eligibleRows: number; errorCount: number; warningCount: number; infoCount: number }
export interface ImportBatch { id: string; originalFileName: string; sizeBytes: number; parserVersion: string; state: ImportState; statisticsJson: string; version: number; createdAt: string }
export interface ImportRow {
  id: string; sheetName: string; rowNumber: number; cellReference: string; sourceType: string; classification: string;
  derivedValuesJson: string; issueCode: string | null; issueMessage: string | null; severity: ImportSeverity; decision: ImportDecision; version: number;
}
export interface ImportConfirmation { batchId: string; state: ImportState; created: number; reused: number; rejected: number; failed: number }
