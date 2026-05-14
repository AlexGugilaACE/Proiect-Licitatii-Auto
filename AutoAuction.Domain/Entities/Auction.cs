using AutoAuction.Domain.Enums;

namespace AutoAuction.Domain.Entities;

public class Auction
{
    public int Id { get; set; }
    public string SellerId { get; set; } = string.Empty;

    public int BrandId { get; set; }
    public Brand? Brand { get; set; }

    public int CarModelId { get; set; }
    public CarModel? CarModel { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Mileage { get; set; }

    public int FuelTypeId { get; set; }
    public CarAttributeOption? FuelType { get; set; }

    public int TransmissionTypeId { get; set; }
    public CarAttributeOption? TransmissionType { get; set; }

    public int BodyTypeId { get; set; }
    public CarAttributeOption? BodyType { get; set; }

    public int ConditionId { get; set; }
    public CarAttributeOption? Condition { get; set; }

    public int DriveTypeId { get; set; }
    public CarAttributeOption? DriveType { get; set; }

    public int ColorId { get; set; }
    public CarAttributeOption? Color { get; set; }

    public SaleChannel SaleChannel { get; set; } = SaleChannel.TimedAuction;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinimumBidIncrement { get; set; } = 100;
    public AuctionStatus Status { get; set; } = AuctionStatus.Draft;
    public int? WinningBidId { get; set; }
    public Bid? WinningBid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AuctionImage> Images { get; set; } = new List<AuctionImage>();
    public VehicleConditionReport? ConditionReport { get; set; }
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
