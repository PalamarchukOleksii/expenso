using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Currency PreferredCurrency { get; set; } = Currency.UAH;

    public ICollection<Account> Accounts { get; } = new List<Account>();
    public ICollection<OutgoingCategory> Categories { get; } = new List<OutgoingCategory>();
    public ICollection<IncomingCategory> IncomingCategories { get; } = new List<IncomingCategory>();
    public ICollection<OutgoingOperation> OutgoingOperations { get; } = new List<OutgoingOperation>();
    public ICollection<IncomingOperation> IncomingOperations { get; } = new List<IncomingOperation>();
    public ICollection<TransferOperation> TransferOperations { get; } = new List<TransferOperation>();
}