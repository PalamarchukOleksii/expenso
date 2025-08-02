using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class OutgoingOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public Guid AccountId { get; set; } = Guid.Empty;
    public Guid OutgoingCategoryId { get; set; } = Guid.Empty;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.UAH;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public OutgoingCategory OutgoingCategory { get; set; } = null!;
}