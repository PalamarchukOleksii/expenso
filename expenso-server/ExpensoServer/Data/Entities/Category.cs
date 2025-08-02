using ExpensoServer.Data.Enums;

namespace ExpensoServer.Data.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; } = null;
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; } = CategoryType.Outgoing;
    public bool IsDefault { get; set; } = false;

    public Guid? User { get; set; } = null;
}
