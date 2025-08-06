using ExpensoServer.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Operation> Operations { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Operation>()
            .HasOne(e => e.FromAccount)
            .WithMany(e => e.FromOperations)
            .HasForeignKey(e => e.FromAccountId);

        modelBuilder.Entity<Operation>()
            .HasOne(e => e.ToAccount)
            .WithMany(e => e.ToOperations)
            .HasForeignKey(e => e.ToAccountId);
    }
}