using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class TransferOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public Guid FromAccountId { get; set; } = Guid.Empty;
    public Guid ToAccountId { get; set; } = Guid.Empty;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.UAH;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public Account FromAccount { get; set; } = null!;
    public Account ToAccount { get; set; } = null!;
}