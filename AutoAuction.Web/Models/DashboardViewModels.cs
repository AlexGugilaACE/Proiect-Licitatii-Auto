using AutoAuction.Domain.Entities;

namespace AutoAuction.Web.Models;

public class SellerDashboardViewModel
{
    public int AuctionCount { get; set; }
    public int ActiveAuctionCount { get; set; }
    public int EndedAuctionCount { get; set; }
    public int TransactionCount { get; set; }
    public IReadOnlyList<Auction> RecentAuctions { get; set; } = [];
    public IReadOnlyList<Transaction> RecentTransactions { get; set; } = [];
}

public class BuyerDashboardViewModel
{
    public int BidCount { get; set; }
    public int FavoriteCount { get; set; }
    public int WonTransactionCount { get; set; }
    public IReadOnlyList<BuyerBidAuctionViewModel> RecentBidAuctions { get; set; } = [];
    public IReadOnlyList<Favorite> Favorites { get; set; } = [];
    public IReadOnlyList<Transaction> RecentTransactions { get; set; } = [];
}

public class BuyerBidAuctionViewModel
{
    public int AuctionId { get; set; }
    public string AuctionTitle { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal HighestOwnBid { get; set; }
    public int OwnBidCount { get; set; }
    public DateTime LastBidAt { get; set; }
}
