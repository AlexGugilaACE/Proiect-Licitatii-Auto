using AutoAuction.Domain.Entities;
using AutoAuction.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<AuctionQuestion> AuctionQuestions => Set<AuctionQuestion>();
    public DbSet<AutoBid> AutoBids => Set<AutoBid>();
    public DbSet<AuctionImage> AuctionImages => Set<AuctionImage>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<CarAttributeOption> CarAttributeOptions => Set<CarAttributeOption>();
    public DbSet<DealerProfile> DealerProfiles => Set<DealerProfile>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Domain.Entities.Transaction> Transactions => Set<Domain.Entities.Transaction>();
    public DbSet<TransactionMessage> TransactionMessages => Set<TransactionMessage>();
    public DbSet<UserReport> UserReports => Set<UserReport>();
    public DbSet<VehicleConditionReport> VehicleConditionReports => Set<VehicleConditionReport>();
    public DbSet<VehicleDamage> VehicleDamages => Set<VehicleDamage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Auction>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Vin).HasMaxLength(17);
            entity.Property(x => x.StartingPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CurrentPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.MinimumBidIncrement).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CarModel).WithMany().HasForeignKey(x => x.CarModelId).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WinningBid)
                .WithMany()
                .HasForeignKey(x => x.WinningBidId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.FuelType).WithMany().HasForeignKey(x => x.FuelTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TransmissionType).WithMany().HasForeignKey(x => x.TransmissionTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BodyType).WithMany().HasForeignKey(x => x.BodyTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DriveType).WithMany().HasForeignKey(x => x.DriveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Color).WithMany().HasForeignKey(x => x.ColorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Bid>(entity =>
        {
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.AuctionId, x.Amount });
        });

        builder.Entity<AutoBid>(entity =>
        {
            entity.Property(x => x.MaxAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.AuctionId, x.BidderId }).IsUnique();
            entity.HasOne(x => x.Auction)
                .WithMany()
                .HasForeignKey(x => x.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuctionQuestion>(entity =>
        {
            entity.Property(x => x.Question).HasMaxLength(1000);
            entity.Property(x => x.Answer).HasMaxLength(1000);
            entity.HasOne(x => x.Auction)
                .WithMany()
                .HasForeignKey(x => x.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.AuctionId, x.CreatedAt });
        });

        builder.Entity<Brand>()
            .HasIndex(x => x.Name)
            .IsUnique();

        builder.Entity<CarModel>()
            .HasIndex(x => new { x.BrandId, x.Name })
            .IsUnique();

        builder.Entity<CarAttributeOption>()
            .HasIndex(x => new { x.Type, x.Name })
            .IsUnique();

        builder.Entity<Domain.Entities.Transaction>()
            .Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Domain.Entities.Transaction>()
            .Property(x => x.PaymentProofPath)
            .HasMaxLength(260);

        builder.Entity<Domain.Entities.Transaction>()
            .HasOne(x => x.Auction)
            .WithMany()
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TransactionMessage>(entity =>
        {
            entity.Property(x => x.Message).HasMaxLength(1200);
            entity.HasOne(x => x.Transaction)
                .WithMany()
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.TransactionId, x.CreatedAt });
        });

        builder.Entity<Review>(entity =>
        {
            entity.Property(x => x.Comment).HasMaxLength(1200);
            entity.HasOne(x => x.Auction)
                .WithMany()
                .HasForeignKey(x => x.AuctionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.SellerId, x.BuyerId }).IsUnique();
        });

        builder.Entity<UserReport>(entity =>
        {
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.HasOne(x => x.Auction)
                .WithMany()
                .HasForeignKey(x => x.AuctionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Review)
                .WithMany()
                .HasForeignKey(x => x.ReviewId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.TargetType, x.Status });
        });

        builder.Entity<ApplicationUser>()
            .Property(x => x.RatingAverage)
            .HasColumnType("decimal(5,2)");

        builder.Entity<ApplicationUser>()
            .Property(x => x.CompanyAddress)
            .HasMaxLength(300);

        builder.Entity<DealerProfile>(entity =>
        {
            entity.Property(x => x.CompanyName).HasMaxLength(160);
            entity.Property(x => x.FiscalCode).HasMaxLength(50);
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);
        });

        builder.Entity<AdminAuditLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(120);
            entity.Property(x => x.TargetType).HasMaxLength(80);
            entity.Property(x => x.TargetId).HasMaxLength(120);
            entity.Property(x => x.Details).HasMaxLength(1200);
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}
