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
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<CashSessionDenomination> CashSessionDenominations => Set<CashSessionDenomination>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<SaleEvent> SaleEvents => Set<SaleEvent>();
    public DbSet<AdminOverrideCode> AdminOverrideCodes => Set<AdminOverrideCode>();
    public DbSet<AdminOverrideRequest> AdminOverrideRequests => Set<AdminOverrideRequest>();
    public DbSet<CashSessionReopenEvent> CashSessionReopenEvents => Set<CashSessionReopenEvent>();

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
            b.Property(x => x.Excluded).IsRequired();
            b.Property(x => x.UserType).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasQueryFilter(x => !x.Excluded);
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

            b.Property(x => x.Code)
                .ValueGeneratedOnAdd();
            b.HasIndex(x => x.Code).IsUnique();

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
            b.Property(x => x.Excluded).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasQueryFilter(x => !x.Excluded);
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code)
                .ValueGeneratedOnAdd();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.Name);

            b.Property(x => x.Description).HasMaxLength(1000);

            b.Property(x => x.Sku).HasMaxLength(60);
            b.HasIndex(x => x.Sku).IsUnique();

            b.Property(x => x.Barcode).HasMaxLength(30);
            b.HasIndex(x => x.Barcode).IsUnique();

            b.Property(x => x.Unit)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            b.Property(x => x.CostPrice).HasPrecision(18, 4);
            b.Property(x => x.SalePrice).HasPrecision(18, 4);
            b.Property(x => x.StockQuantity).HasPrecision(18, 4);
            b.Property(x => x.MinStockQuantity).HasPrecision(18, 4);

            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.Excluded).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasQueryFilter(x => !x.Excluded);
        });

        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("customers");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code)
                .ValueGeneratedOnAdd();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.PersonType).IsRequired();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.Name);

            b.Property(x => x.TradeName).HasMaxLength(200);

            b.Property(x => x.Document).HasMaxLength(14);
            b.HasIndex(x => x.Document).IsUnique();

            b.Property(x => x.Email).HasMaxLength(200);
            b.HasIndex(x => x.Email);

            b.Property(x => x.Phone).HasMaxLength(30);
            b.Property(x => x.Mobile).HasMaxLength(30);

            b.Property(x => x.AddressLine1).HasMaxLength(300);
            b.Property(x => x.AddressLine2).HasMaxLength(300);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.State).HasMaxLength(2);
            b.Property(x => x.PostalCode).HasMaxLength(10);

            b.Property(x => x.Notes).HasMaxLength(1000);

            b.Property(x => x.CreditLimit).HasPrecision(18, 4);
            b.Property(x => x.CurrentBalance).HasPrecision(18, 4);

            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.Excluded).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasQueryFilter(x => !x.Excluded);
        });

        modelBuilder.Entity<CashRegister>(b =>
        {
            b.ToTable("cash_registers");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code)
                .ValueGeneratedOnAdd();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(80);
            b.HasIndex(x => x.Name).IsUnique();

            b.Property(x => x.Location)
                .HasMaxLength(120);

            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);
        });

        modelBuilder.Entity<CashSession>(b =>
        {
            b.ToTable("cash_sessions");
            b.HasKey(x => x.Id);

            b.Property(x => x.OpenedAtUtc).IsRequired();
            b.Property(x => x.ClosedAtUtc);

            b.Property(x => x.OpeningFloatAmount)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.ClosingCountedAmount)
                .HasPrecision(18, 4);

            b.Property(x => x.ClosingNotes)
                .HasMaxLength(1000);

            b.HasOne(x => x.CashRegister)
                .WithMany()
                .HasForeignKey(x => x.CashRegisterId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.CashRegisterId);
            b.HasIndex(x => new { x.CashRegisterId, x.ClosedAtUtc })
                .IsUnique()
                .HasFilter("\"ClosedAtUtc\" IS NULL");

            b.HasIndex(x => x.OpenedAtUtc);
        });

        modelBuilder.Entity<CashMovement>(b =>
        {
            b.ToTable("cash_movements");
            b.HasKey(x => x.Id);

            b.Property(x => x.Type).IsRequired();
            b.Property(x => x.Amount)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.Notes)
                .HasMaxLength(1000);

            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasOne(x => x.CashSession)
                .WithMany()
                .HasForeignKey(x => x.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.AdminOverrideCode)
                .WithMany()
                .HasForeignKey(x => x.AdminOverrideCodeId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.CashSessionId);
            b.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<AdminOverrideCode>(b =>
        {
            b.ToTable("admin_override_codes");
            b.HasKey(x => x.Id);

            b.Property(x => x.CodeHash)
                .IsRequired()
                .HasMaxLength(128);
            b.HasIndex(x => x.CodeHash).IsUnique();

            b.Property(x => x.Purpose).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ExpiresAtUtc).IsRequired();
            b.Property(x => x.UsedAtUtc);

            b.Property(x => x.Justification)
                .HasMaxLength(500);

            b.HasIndex(x => x.ExpiresAtUtc);
            b.HasIndex(x => x.UsedAtUtc);
        });

        modelBuilder.Entity<AdminOverrideRequest>(b =>
        {
            b.ToTable("admin_override_requests");
            b.HasKey(x => x.Id);

            b.Property(x => x.Purpose).IsRequired();
            b.Property(x => x.RequestedAtUtc).IsRequired();
            b.Property(x => x.ApprovedAtUtc);

            b.Property(x => x.Justification)
                .HasMaxLength(500);
            b.Property(x => x.RejectionReason)
                .HasMaxLength(500);

            b.HasIndex(x => x.RequestedAtUtc);
            b.HasIndex(x => x.ApprovedAtUtc);
        });

        modelBuilder.Entity<CashSessionReopenEvent>(b =>
        {
            b.ToTable("cash_session_reopen_events");
            b.HasKey(x => x.Id);

            b.Property(x => x.ReopenedAtUtc).IsRequired();
            b.Property(x => x.Justification)
                .IsRequired()
                .HasMaxLength(500);

            b.HasIndex(x => x.CashSessionId);
            b.HasIndex(x => x.ReopenedAtUtc);
        });

        modelBuilder.Entity<CashSessionDenomination>(b =>
        {
            b.ToTable("cash_session_denominations");
            b.HasKey(x => x.Id);

            b.Property(x => x.Denomination)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.Quantity)
                .IsRequired();

            b.HasOne(x => x.CashSession)
                .WithMany()
                .HasForeignKey(x => x.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.CashSessionId);
            b.HasIndex(x => new { x.CashSessionId, x.Denomination }).IsUnique();
        });

        modelBuilder.Entity<Sale>(b =>
        {
            b.ToTable("sales");
            b.HasKey(x => x.Id);

            b.Property(x => x.Code)
                .ValueGeneratedOnAdd();
            b.HasIndex(x => x.Code).IsUnique();

            b.Property(x => x.Status).IsRequired();

            b.Property(x => x.SubtotalAmount).HasPrecision(18, 4);
            b.Property(x => x.ItemDiscountTotalAmount).HasPrecision(18, 4);
            b.Property(x => x.SaleDiscountAmount).HasPrecision(18, 4);
            b.Property(x => x.TotalAmount).HasPrecision(18, 4);
            b.Property(x => x.PaidAmount).HasPrecision(18, 4);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);
            b.Property(x => x.FinalizedAtUtc);
            b.Property(x => x.CancelledAtUtc);

            b.Property(x => x.CancelReason)
                .HasMaxLength(500);

            b.HasOne(x => x.CashRegister)
                .WithMany()
                .HasForeignKey(x => x.CashRegisterId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.CashSession)
                .WithMany()
                .HasForeignKey(x => x.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.CashRegisterId);
            b.HasIndex(x => x.CashSessionId);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<SaleItem>(b =>
        {
            b.ToTable("sale_items");
            b.HasKey(x => x.Id);

            b.Property(x => x.Quantity)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.UnitPriceOriginal)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.UnitPriceFinal)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.DiscountAmount)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.LineTotalAmount)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasOne(x => x.Sale)
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.SaleId);
            b.HasIndex(x => new { x.SaleId, x.ProductId }).IsUnique();
        });

        modelBuilder.Entity<SalePayment>(b =>
        {
            b.ToTable("sale_payments");
            b.HasKey(x => x.Id);

            b.Property(x => x.Method).IsRequired();

            b.Property(x => x.Amount)
                .HasPrecision(18, 4)
                .IsRequired();

            b.Property(x => x.AmountReceived)
                .HasPrecision(18, 4);

            b.Property(x => x.ChangeGiven)
                .HasPrecision(18, 4);

            b.Property(x => x.AuthorizationCode)
                .HasMaxLength(80);

            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasOne(x => x.Sale)
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.SaleId);
            b.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<SaleEvent>(b =>
        {
            b.ToTable("sale_events");
            b.HasKey(x => x.Id);

            b.Property(x => x.Type).IsRequired();

            b.Property(x => x.Details)
                .HasMaxLength(2000);

            b.Property(x => x.PerformedAtUtc).IsRequired();

            b.HasIndex(x => x.SaleId);
            b.HasIndex(x => x.PerformedAtUtc);
        });
    }
}
