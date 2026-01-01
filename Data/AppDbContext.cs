using Microsoft.EntityFrameworkCore;
using PDVNow.Auth.Entities;
using PDVNow.Entities;

namespace PDVNow.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.Property(x => x.Username).IsRequired().HasMaxLength(100);
            b.HasIndex(x => x.Username).IsUnique();

            b.Property(x => x.Email).HasMaxLength(200);
            b.HasIndex(x => x.Email);

            b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.UserType).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            b.HasIndex(x => x.TokenHash);

            b.Property(x => x.ExpiresAtUtc).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.RevokedAtUtc);

            b.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.UserId, x.TokenHash }).IsUnique();
        });

        modelBuilder.Entity<Supplier>(b =>
        {
            b.ToTable("suppliers");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.Name);

            b.Property(x => x.TradeName).HasMaxLength(200);

            b.Property(x => x.Cnpj).HasMaxLength(14);
            b.HasIndex(x => x.Cnpj);

            b.Property(x => x.StateRegistration).HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(30);

            b.Property(x => x.AddressLine1).HasMaxLength(300);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.State).HasMaxLength(2);
            b.Property(x => x.PostalCode).HasMaxLength(10);

            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.Name);

            b.Property(x => x.Description).HasMaxLength(1000);

            b.Property(x => x.Sku).HasMaxLength(60);
            b.HasIndex(x => x.Sku).IsUnique();

            b.Property(x => x.Barcode).HasMaxLength(30);
            b.HasIndex(x => x.Barcode).IsUnique();

            b.Property(x => x.Unit).IsRequired().HasMaxLength(20);

            b.Property(x => x.CostPrice).HasPrecision(18, 4);
            b.Property(x => x.SalePrice).HasPrecision(18, 4);
            b.Property(x => x.StockQuantity).HasPrecision(18, 4);
            b.Property(x => x.MinStockQuantity).HasPrecision(18, 4);

            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
