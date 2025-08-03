using ExpensoServer.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<OutgoingOperation> OutgoingOperations { get; set; }
    public DbSet<IncomingOperation> IncomingOperations { get; set; }
    public DbSet<TransferOperation> TransferOperations { get; set; }
    public DbSet<OutgoingCategory> OutgoingCategories { get; set; }
    public DbSet<IncomingCategory> IncomingCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasMany(u => u.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Categories)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(u => u.IncomingCategories)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(u => u.OutgoingOperations)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.IncomingOperations)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.TransferOperations)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasMany(a => a.OutgoingOperations)
                .WithOne(o => o.Account)
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.IncomingOperations)
                .WithOne(o => o.Account)
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.IncomingTransfers)
                .WithOne(t => t.ToAccount)
                .HasForeignKey(t => t.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(a => a.OutgoingTransfers)
                .WithOne(t => t.FromAccount)
                .HasForeignKey(t => t.FromAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomingCategory>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasMany(c => c.IncomingOperations)
                .WithOne(o => o.IncomingCategory)
                .HasForeignKey(o => o.IncomingCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutgoingCategory>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasMany(c => c.OutgoingOperations)
                .WithOne(o => o.OutgoingCategory)
                .HasForeignKey(o => o.OutgoingCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncomingOperation>(entity => { entity.HasKey(o => o.Id); });

        modelBuilder.Entity<OutgoingOperation>(entity => { entity.HasKey(o => o.Id); });

        modelBuilder.Entity<TransferOperation>(entity => { entity.HasKey(t => t.Id); });

        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<IncomingOperation>()
            .Property(o => o.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OutgoingOperation>()
            .Property(o => o.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<TransferOperation>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");

        base.OnModelCreating(modelBuilder);
    }
}