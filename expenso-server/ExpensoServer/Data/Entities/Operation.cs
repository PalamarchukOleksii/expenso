using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class Operation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public OperationType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Account? FromAccount { get; set; }
    public Account? ToAccount { get; set; }
    public Category? Category { get; set; }
}