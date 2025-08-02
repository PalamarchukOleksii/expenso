namespace ExpensoServer.Data.Entities;

public class IncomingCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public User? User { get; set; }
    public ICollection<IncomingOperation> IncomingOperations { get; } = new List<IncomingOperation>();
}