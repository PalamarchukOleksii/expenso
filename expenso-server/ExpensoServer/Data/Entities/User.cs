namespace ExpensoServer.Data.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];

    public ICollection<Account> Accounts { get; } = new List<Account>();
    public ICollection<Category> Categories { get; } = new List<Category>();
    public ICollection<Operation> Operations { get; } = new List<Operation>();
}