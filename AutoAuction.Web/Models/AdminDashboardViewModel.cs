using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
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
    public int PendingSellerCount { get; set; }
    public int PendingReportCount { get; set; }
    public int PendingPaymentProofCount { get; set; }
    public IReadOnlyList<AdminPendingSellerViewModel> PendingSellers { get; set; } = [];
    public IReadOnlyList<UserReportListItemViewModel> RecentReports { get; set; } = [];
    public IReadOnlyList<AdminAuditLogListItemViewModel> RecentAuditLogs { get; set; } = [];
}

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public bool IsSeller { get; set; }
    public bool IsSellerApproved { get; set; }
    public bool IsSellerRejected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUsersViewModel
{
    public IReadOnlyList<AdminUserListItemViewModel> Users { get; set; } = [];
    public IReadOnlyList<SelectListItem> RoleItems { get; set; } = [];
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class AdminAuctionsViewModel
{
    public IReadOnlyList<Auction> Auctions { get; set; } = [];
    public string? Query { get; set; }
    public AuctionStatus? Status { get; set; }
    public IReadOnlyList<SelectListItem> StatusItems { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class ChangeUserRoleViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AdminSellerProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string FiscalCode { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsRejected { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime UserCreatedAt { get; set; }
    public DateTime ProfileCreatedAt { get; set; }
    public int AuctionCount { get; set; }
    public int TransactionCount { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
}

public class RejectSellerViewModel
{
    public string UserId { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class AdminPendingSellerViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminAuditLogListItemViewModel
{
    public string AdminName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminReportsViewModel
{
    public IReadOnlyList<UserReportListItemViewModel> Reports { get; set; } = [];
    public ReportStatus? Status { get; set; }
    public IReadOnlyList<SelectListItem> StatusItems { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class UserReportListItemViewModel
{
    public int Id { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public string ReportedUserName { get; set; } = string.Empty;
    public string ReportedUserEmail { get; set; } = string.Empty;
    public ReportTargetType TargetType { get; set; }
    public int? AuctionId { get; set; }
    public string? AuctionTitle { get; set; }
    public int? ReviewId { get; set; }
    public string? ReviewComment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
