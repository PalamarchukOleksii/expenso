using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class Operation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public OperationType Type { get; set; } = OperationType.Outgoing;
    public Guid AccountId { get; set; } = Guid.Empty;
    public Guid? ToAccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.UAH;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public User User { get; set; } = new();
    public Account Account { get; set; } = new();
    public Account? ToAccount { get; set; } 
    public Category? Category { get; set; }
}