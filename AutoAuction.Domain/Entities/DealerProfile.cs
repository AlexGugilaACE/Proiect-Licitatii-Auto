namespace AutoAuction.Domain.Entities;

public class DealerProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string FiscalCode { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsRejected { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
