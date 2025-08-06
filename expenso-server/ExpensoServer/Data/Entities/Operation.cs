using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class Operation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public OperationType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public User User { get; set; } = null!;
    public Account? FromAccount { get; set; }
    public Account? ToAccount { get; set; }
    public Category? Category { get; set; }
}