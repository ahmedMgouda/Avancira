namespace Avancira.Domain.Transactions;
public enum TransactionStatus
{
    Created = 1,
    Withhold = 2,
    Completed = 3,
    Refunded = 4,
    Failed = 5
}
