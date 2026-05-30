using System.ComponentModel.DataAnnotations;

namespace AutoAuction.Web.Models;

public class AuctionQuestionListItemViewModel
{
    public int Id { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
}

public class CreateAuctionQuestionViewModel
{
    [Required]
    [StringLength(1000)]
    public string Question { get; set; } = string.Empty;
}

public class AnswerAuctionQuestionViewModel
{
    public int QuestionId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Answer { get; set; } = string.Empty;
}
