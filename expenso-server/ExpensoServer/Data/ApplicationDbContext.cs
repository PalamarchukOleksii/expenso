using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
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

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
        }

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = Guid.Parse("181f61df-3da5-4f89-ab05-8b0718d25aa2"),
                Name = "Salary",
                Type = CategoryType.Income,
                IsDefault = true
            },
            new Category
            {
                Id = Guid.Parse("ceca4e62-81f6-4aa7-b37c-9f57b0ef4a71"),
                Name = "Investments",
                Type = CategoryType.Income,
                IsDefault = true
            },
            new Category
            {
                Id = Guid.Parse("ddabd24b-40c3-4a3b-aa9b-3de111054a63"),
                Name = "Other",
                Type = CategoryType.Income,
                IsDefault = true
            }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = Guid.Parse("430b7c2c-bdd5-4bee-8609-e08c8f406a39"),
                Name = "Food",
                Type = CategoryType.Expense,
                IsDefault = true
            },
            new Category
            {
                Id = Guid.Parse("bc539863-1619-4d83-a168-2b828f694c3e"),
                Name = "Home",
                Type = CategoryType.Expense,
                IsDefault = true
            },
            new Category
            {
                Id = Guid.Parse("eb34eabf-891c-4eee-98d2-c64c4315055d"),
                Name = "Other",
                Type = CategoryType.Expense,
                IsDefault = true
            }
        );
    }
}