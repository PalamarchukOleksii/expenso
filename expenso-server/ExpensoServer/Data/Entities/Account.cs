using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public Currency Currency { get; set; } = Currency.UAH;

    public User User { get; set; } = null!;
    public ICollection<OutgoingOperation> OutgoingOperations { get; } = new List<OutgoingOperation>();
    public ICollection<IncomingOperation> IncomingOperations { get; } = new List<IncomingOperation>();
    public ICollection<TransferOperation> IncomingTransfers { get; } = new List<TransferOperation>();
    public ICollection<TransferOperation> OutgoingTransfers { get; } = new List<TransferOperation>();
}