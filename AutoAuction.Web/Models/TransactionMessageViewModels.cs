using System.ComponentModel.DataAnnotations;

namespace AutoAuction.Web.Models;

public class TransactionMessageListItemViewModel
{
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTransactionMessageViewModel
{
    [Required]
    [StringLength(1200)]
    public string Message { get; set; } = string.Empty;
}
