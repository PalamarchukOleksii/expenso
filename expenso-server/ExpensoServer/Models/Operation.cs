using ExpensoServer.Shared.Enums;

namespace ExpensoServer.Models;

public enum OperationType
{
    Incoming,
    Outgoing,
    Transfer
}

public class Operation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public OperationType Type { get; set; } = OperationType.Outgoing;
    public Guid AccountId { get; set; } = Guid.Empty;
    public Guid? ToAccountId { get; set; } = null;
    public Guid? CategoryId { get; set; } = null;
    public decimal Amount { get; set; } = 0.0m;
    public Currency Currency { get; set; } = Currency.UAH;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; } = null;

    public User User { get; set; } = new();
    public Account Account { get; set; } = new();
    public Account? ToAccount { get; set; } = null;
    public Category? Category { get; set; } = null;
}

