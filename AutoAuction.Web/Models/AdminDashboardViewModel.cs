using AutoAuction.Domain.Entities;
using AutoAuction.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoAuction.Web.Models;

public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int AuctionCount { get; set; }
    public int ActiveAuctionCount { get; set; }
    public int TransactionCount { get; set; }
    public decimal TransactionTotal { get; set; }
    public IReadOnlyList<ApplicationUser> RecentUsers { get; set; } = [];
    public IReadOnlyList<Auction> RecentAuctions { get; set; } = [];
}

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUsersViewModel
{
    public IReadOnlyList<AdminUserListItemViewModel> Users { get; set; } = [];
    public IReadOnlyList<SelectListItem> RoleItems { get; set; } = [];
}

public class ChangeUserRoleViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
