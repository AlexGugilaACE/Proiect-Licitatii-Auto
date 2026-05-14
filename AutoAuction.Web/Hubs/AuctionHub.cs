using Microsoft.AspNetCore.SignalR;

namespace AutoAuction.Web.Hubs;

public class AuctionHub : Hub
{
    public Task JoinAuction(int auctionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(Context.UserIdentifier));
        }

        await base.OnConnectedAsync();
    }

    public static string AuctionGroup(int auctionId) => $"auction-{auctionId}";
    public static string UserGroup(string userId) => $"user-{userId}";
}
