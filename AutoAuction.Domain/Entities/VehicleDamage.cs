namespace AutoAuction.Domain.Entities;

public class VehicleDamage
{
    public int Id { get; set; }
    public int VehicleConditionReportId { get; set; }
    public VehicleConditionReport? VehicleConditionReport { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}
