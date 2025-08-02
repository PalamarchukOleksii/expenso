using ExpensoServer.Shared.Enums;

namespace ExpensoServer.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Currency PreferredCurrency { get; set; } = Currency.UAH;

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Operation> Operations { get; set; } = [];
}
