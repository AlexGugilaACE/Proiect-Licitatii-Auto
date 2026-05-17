namespace AutoAuction.Application.DTOs;

public class AuctionCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public int CarModelId { get; set; }
    public int Year { get; set; }
    public int Mileage { get; set; }
    public int EngineCapacityCm3 { get; set; }
    public int HorsePower { get; set; }
    public int FuelTypeId { get; set; }
    public int TransmissionTypeId { get; set; }
    public int BodyTypeId { get; set; }
    public int ConditionId { get; set; }
    public int DriveTypeId { get; set; }
    public int ColorId { get; set; }
    public decimal StartingPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal MinimumBidIncrement { get; set; } = 100;
    public string OverallGrade { get; set; } = "B";
    public string ExteriorCondition { get; set; } = string.Empty;
    public string InteriorCondition { get; set; } = string.Empty;
    public string MechanicalCondition { get; set; } = string.Empty;
    public string TireCondition { get; set; } = string.Empty;
    public bool HasAccidentHistory { get; set; }
    public bool HasServiceHistory { get; set; }
    public string ConditionNotes { get; set; } = string.Empty;
}
