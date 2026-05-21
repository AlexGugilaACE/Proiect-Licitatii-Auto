using AutoAuction.Application.Interfaces;
using AutoAuction.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AutoAuction.Web.Services;

public class AutoBidProcessingService(
    IServiceScopeFactory scopeFactory,
    IHubContext<AuctionHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
            var results = await auctionService.ProcessPendingAutoBidsAsync(stoppingToken);

            foreach (var result in results)
            {
                await hubContext.Clients.Group(AuctionHub.AuctionGroup(result.AuctionId))
                    .SendAsync("BidPlaced", result.AuctionId, result.CurrentPrice, result.BidCreatedAt, stoppingToken);

                foreach (var outbidUserId in result.OutbidUserIds)
                {
                    await hubContext.Clients.Group(AuctionHub.UserGroup(outbidUserId))
                        .SendAsync("Outbid", result.AuctionId, result.CurrentPrice, stoppingToken);
                }
            }
        }
    }
}
