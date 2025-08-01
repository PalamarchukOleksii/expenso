using ExpensoServer.Features.Accounts;
using ExpensoServer.Features.Categories;
using ExpensoServer.Features.Operations;
using ExpensoServer.Shared;

namespace ExpensoServer.Features.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public Currency PreferredCurrency { get; set; } = Currency.UAH;

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Operation> Operations { get; set; } = [];
}
