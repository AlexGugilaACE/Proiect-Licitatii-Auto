namespace AutoAuction.Domain.Entities;

public class VehicleConditionReport
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public string OverallGrade { get; set; } = "B";
    public string ExteriorCondition { get; set; } = string.Empty;
    public string InteriorCondition { get; set; } = string.Empty;
    public string MechanicalCondition { get; set; } = string.Empty;
    public string TireCondition { get; set; } = string.Empty;
    public bool HasAccidentHistory { get; set; }
    public bool HasServiceHistory { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<VehicleDamage> Damages { get; set; } = new List<VehicleDamage>();
}
