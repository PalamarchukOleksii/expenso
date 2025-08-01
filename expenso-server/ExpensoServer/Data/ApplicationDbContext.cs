using ExpensoServer.Features.Accounts;
using ExpensoServer.Features.Categories;
using ExpensoServer.Features.Operations;
using ExpensoServer.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Operation> Operations { get; set; }
    public DbSet<Category> Categories { get; set; }
}
