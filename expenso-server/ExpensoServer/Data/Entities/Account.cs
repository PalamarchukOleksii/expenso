using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public Currency Currency { get; set; } = Currency.UAH;

    public User User { get; set; } = null!;
    public ICollection<Operation> FromOperations { get; } = new List<Operation>();
    public ICollection<Operation> ToOperations { get; } = new List<Operation>();
}