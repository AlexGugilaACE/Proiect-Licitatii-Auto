namespace AutoAuction.Domain.Enums;

public enum NotificationType
{
    NewBid = 0,
    Outbid = 1,
    AuctionWon = 2,
    AuctionLost = 3,
    AuctionEnded = 4,
    TransactionCreated = 5,
    SellerApproved = 6,
    AuctionQuestion = 7,
    QuestionAnswered = 8,
    TransactionMessage = 9,
    SellerRejected = 10,
    PaymentProofUploaded = 11,
    PaymentProofAccepted = 12,
    PaymentProofRejected = 13
}
