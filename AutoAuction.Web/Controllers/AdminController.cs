using AutoAuction.Domain.Enums;
using AutoAuction.Domain.Entities;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class AdminController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    private const int AdminPageSize = 10;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var sellers = await userManager.GetUsersInRoleAsync(AppRoles.Seller);
        var sellerIds = sellers.Select(x => x.Id).ToList();
        var sellerProfiles = await db.DealerProfiles
            .Where(x => sellerIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);
        var sellerById = sellers.ToDictionary(x => x.Id);
        var pendingSellerProfiles = sellerProfiles
            .Where(x => !x.IsVerified && !x.IsRejected)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToList();
        var recentReports = await db.UserReports
            .Include(x => x.Auction)
            .Include(x => x.Review)
            .Where(x => x.Status == ReportStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        var reporterIds = recentReports.Select(x => x.ReporterId).Distinct().ToList();
        var reportedUserIds = GetReportedUserIds(recentReports);
        var reporters = await db.Users
            .Where(x => reporterIds.Contains(x.Id) || reportedUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var recentAuditLogs = await db.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(7)
            .ToListAsync(cancellationToken);
        var adminIds = recentAuditLogs.Select(x => x.AdminId).Distinct().ToList();
        var admins = await db.Users
            .Where(x => adminIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var model = new AdminDashboardViewModel
        {
            UserCount = await userManager.Users.CountAsync(cancellationToken),
            AuctionCount = await db.Auctions.CountAsync(cancellationToken),
            ActiveAuctionCount = await db.Auctions.CountAsync(x => x.Status == AuctionStatus.Active, cancellationToken),
            TransactionCount = await db.Transactions.CountAsync(cancellationToken),
            TransactionTotal = await db.Transactions.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0,
            PendingSellerCount = sellerProfiles.Count(x => !x.IsVerified && !x.IsRejected),
            PendingReportCount = await db.UserReports.CountAsync(x => x.Status == ReportStatus.Pending, cancellationToken),
            PendingPaymentProofCount = await db.Transactions.CountAsync(x => x.PaymentProofStatus == PaymentProofStatus.PendingReview, cancellationToken),
            PendingSellers = pendingSellerProfiles.Select(profile =>
            {
                sellerById.TryGetValue(profile.UserId, out var seller);
                return new AdminPendingSellerViewModel
                {
                    UserId = profile.UserId,
                    FullName = seller is null ? "Vanzator" : $"{seller.FirstName} {seller.LastName}".Trim(),
                    Email = seller?.Email ?? string.Empty,
                    CompanyName = profile.CompanyName,
                    CreatedAt = profile.CreatedAt
                };
            }).ToList(),
            RecentReports = BuildReportItems(recentReports, reporters),
            RecentAuditLogs = recentAuditLogs.Select(log =>
            {
                admins.TryGetValue(log.AdminId, out var admin);
                var adminName = admin is null ? "Administrator" : $"{admin.FirstName} {admin.LastName}".Trim();
                return new AdminAuditLogListItemViewModel
                {
                    AdminName = string.IsNullOrWhiteSpace(adminName) ? admin?.Email ?? "Administrator" : adminName,
                    Action = log.Action,
                    TargetType = log.TargetType,
                    TargetId = log.TargetId,
                    Details = log.Details,
                    CreatedAt = log.CreatedAt
                };
            }).ToList(),
            RecentUsers = await userManager.Users.OrderByDescending(x => x.CreatedAt).Take(7).ToListAsync(cancellationToken),
            RecentAuctions = await db.Auctions
                .Include(x => x.Brand)
                .Include(x => x.CarModel)
                .Include(x => x.Images)
                .OrderByDescending(x => x.CreatedAt)
                .Take(7)
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> SellerProfile(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(AppRoles.Seller))
        {
            return NotFound();
        }

        var profile = await db.DealerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
        var model = new AdminSellerProfileViewModel
        {
            UserId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            CompanyName = profile?.CompanyName ?? string.Empty,
            FiscalCode = profile?.FiscalCode ?? string.Empty,
            CompanyAddress = user.CompanyAddress,
            IsVerified = profile?.IsVerified == true,
            IsRejected = profile?.IsRejected == true,
            RejectionReason = profile?.RejectionReason ?? string.Empty,
            UserCreatedAt = user.CreatedAt,
            ProfileCreatedAt = profile?.CreatedAt ?? user.CreatedAt,
            AuctionCount = await db.Auctions.CountAsync(x => x.SellerId == id, cancellationToken),
            TransactionCount = await db.Transactions.CountAsync(x => x.SellerId == id, cancellationToken),
            RatingAverage = user.RatingAverage,
            RatingCount = user.RatingCount
        };

        return View(model);
    }

    public async Task<IActionResult> Users(string? query, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var usersQuery = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            usersQuery = usersQuery.Where(x =>
                (x.Email != null && x.Email.Contains(term)) ||
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term));
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)AdminPageSize));
        page = Math.Min(page, totalPages);
        var users = await usersQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * AdminPageSize)
            .Take(AdminPageSize)
            .ToListAsync(cancellationToken);
        var userIds = users.Select(x => x.Id).ToList();
        var dealerProfiles = await db.DealerProfiles
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);
        var items = new List<AdminUserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dealerProfiles.TryGetValue(user.Id, out var dealerProfile);
            items.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}",
                Roles = string.Join(", ", roles),
                IsDisabled = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                IsSeller = roles.Contains(AppRoles.Seller),
                IsSellerApproved = dealerProfile?.IsVerified == true,
                IsSellerRejected = dealerProfile?.IsRejected == true,
                CreatedAt = user.CreatedAt
            });
        }

        var model = new AdminUsersViewModel
        {
            Users = items,
            RoleItems = AppRoles.All.Select(x => new SelectListItem(x, x)).ToList(),
            Query = query,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUserRole(ChangeUserRoleViewModel model)
    {
        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null || !AppRoles.All.Contains(model.Role))
        {
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, model.Role);
        AddAudit("Schimbare rol utilizator", "User", user.Id, $"Rol nou: {model.Role}");
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSeller(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return RedirectToAction(nameof(Users));
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(AppRoles.Seller))
        {
            return RedirectToAction(nameof(Users));
        }

        var profile = await db.DealerProfiles.FirstOrDefaultAsync(x => x.UserId == id, cancellationToken);
        if (profile is null)
        {
            profile = new Domain.Entities.DealerProfile
            {
                UserId = id,
                CompanyName = $"{user.FirstName} {user.LastName}".Trim(),
                FiscalCode = string.Empty
            };
            db.DealerProfiles.Add(profile);
        }

        profile.IsVerified = true;
        profile.IsRejected = false;
        profile.RejectionReason = string.Empty;
        db.Notifications.Add(new Domain.Entities.Notification
        {
            UserId = id,
            Title = "Cont de vanzator aprobat",
            Message = "Contul tau de vanzator a fost aprobat. Acum poti publica licitatii.",
            Type = NotificationType.SellerApproved
        });
        AddAudit("Aprobare seller", "Seller", id, $"Seller aprobat: {user.Email}");
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(SellerProfile), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectSeller(RejectSellerViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(SellerProfile), new { id = model.UserId });
        }

        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return RedirectToAction(nameof(Users));
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(AppRoles.Seller))
        {
            return RedirectToAction(nameof(Users));
        }

        var profile = await db.DealerProfiles.FirstOrDefaultAsync(x => x.UserId == model.UserId, cancellationToken);
        if (profile is null)
        {
            profile = new DealerProfile
            {
                UserId = model.UserId,
                CompanyName = $"{user.FirstName} {user.LastName}".Trim(),
                FiscalCode = string.Empty
            };
            db.DealerProfiles.Add(profile);
        }

        profile.IsVerified = false;
        profile.IsRejected = true;
        profile.RejectionReason = model.Reason.Trim();
        db.Notifications.Add(new Notification
        {
            UserId = model.UserId,
            Title = "Cont de vanzator respins",
            Message = $"Cererea ta de vanzator a fost respinsa. Motiv: {profile.RejectionReason}",
            Type = NotificationType.SellerRejected
        });
        AddAudit("Respingere seller", "Seller", model.UserId, profile.RejectionReason);
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(SellerProfile), new { id = model.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserDisabled(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return RedirectToAction(nameof(Users));
        }

        var disabled = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = disabled ? null : DateTimeOffset.UtcNow.AddYears(100);
        await userManager.UpdateAsync(user);
        AddAudit(disabled ? "Activare utilizator" : "Dezactivare utilizator", "User", id, user.Email ?? string.Empty);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Auctions(string? query, AuctionStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var auctionsQuery = db.Auctions
            .Include(x => x.Brand)
            .Include(x => x.CarModel)
            .Include(x => x.FuelType)
            .Include(x => x.TransmissionType)
            .Include(x => x.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            auctionsQuery = auctionsQuery.Where(x =>
                x.Title.Contains(term) ||
                x.Vin.Contains(term) ||
                x.Brand!.Name.Contains(term) ||
                x.CarModel!.Name.Contains(term));
        }

        if (status.HasValue)
        {
            auctionsQuery = auctionsQuery.Where(x => x.Status == status.Value);
        }

        var totalCount = await auctionsQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)AdminPageSize));
        page = Math.Min(page, totalPages);
        var auctions = await auctionsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * AdminPageSize)
            .Take(AdminPageSize)
            .ToListAsync(cancellationToken);

        var model = new AdminAuctionsViewModel
        {
            Auctions = auctions,
            Query = query,
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            StatusItems = Enum.GetValues<AuctionStatus>()
                .Select(x => new SelectListItem(GetAuctionStatusLabel(x), x.ToString(), x == status))
                .Prepend(new SelectListItem("Toate statusurile", string.Empty, !status.HasValue))
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> ReferenceData(CancellationToken cancellationToken)
    {
        var brands = await db.Brands.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var model = new ReferenceDataAdminViewModel
        {
            Brands = brands,
            Models = await db.CarModels.Include(x => x.Brand).OrderBy(x => x.Brand!.Name).ThenBy(x => x.Name).ToListAsync(cancellationToken),
            Options = await db.CarAttributeOptions.OrderBy(x => x.Type).ThenBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken),
            BrandItems = brands.Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.Name, x.Id.ToString())).ToList(),
            AttributeTypes = Enum.GetValues<AttributeOptionType>()
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.ToString(), ((int)x).ToString()))
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Reports(ReportStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var reportsQuery = db.UserReports
            .Include(x => x.Auction)
            .Include(x => x.Review)
            .AsQueryable();

        if (status.HasValue)
        {
            reportsQuery = reportsQuery.Where(x => x.Status == status.Value);
        }

        var totalCount = await reportsQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)AdminPageSize));
        page = Math.Min(page, totalPages);
        var reports = await reportsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * AdminPageSize)
            .Take(AdminPageSize)
            .ToListAsync(cancellationToken);
        var reporterIds = reports.Select(x => x.ReporterId).Distinct().ToList();
        var reportedUserIds = GetReportedUserIds(reports);
        var reporters = await db.Users
            .Where(x => reporterIds.Contains(x.Id) || reportedUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var model = new AdminReportsViewModel
        {
            Reports = BuildReportItems(reports, reporters),
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            StatusItems = Enum.GetValues<ReportStatus>()
                .Select(x => new SelectListItem(GetReportStatusLabel(x), x.ToString(), x == status))
                .Prepend(new SelectListItem("Toate raportarile", string.Empty, !status.HasValue))
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> ReportDetails(int id, CancellationToken cancellationToken)
    {
        var report = await db.UserReports
            .Include(x => x.Auction)
            .Include(x => x.Review)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var userIds = GetReportedUserIds([report]).Append(report.ReporterId).Distinct().ToList();
        var users = await db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var model = BuildReportItems([report], users).First();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReportStatus(int id, ReportStatus status, bool returnToDetails = false, CancellationToken cancellationToken = default)
    {
        if (status is not ReportStatus.Resolved and not ReportStatus.Rejected)
        {
            return RedirectToAction(nameof(Reports));
        }

        var report = await db.UserReports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (report is null)
        {
            return RedirectToAction(nameof(Reports));
        }

        report.Status = status;
        report.ResolvedAt = DateTime.UtcNow;
        AddAudit("Actualizare raportare", "Report", id.ToString(), $"Status nou: {status}");
        await db.SaveChangesAsync(cancellationToken);
        if (returnToDetails)
        {
            return RedirectToAction(nameof(ReportDetails), new { id });
        }

        return RedirectToAction(nameof(Reports));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBrand(BrandFormViewModel model, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(model.Name) && !await db.Brands.AnyAsync(x => x.Name == model.Name, cancellationToken))
        {
            db.Brands.Add(new Domain.Entities.Brand { Name = model.Name.Trim() });
            await db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(ReferenceData));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModel(CarModelFormViewModel model, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            var exists = await db.CarModels.AnyAsync(x => x.BrandId == model.BrandId && x.Name == model.Name, cancellationToken);
            if (!exists)
            {
                db.CarModels.Add(new Domain.Entities.CarModel { BrandId = model.BrandId, Name = model.Name.Trim() });
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return RedirectToAction(nameof(ReferenceData));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttributeOption(AttributeOptionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            var exists = await db.CarAttributeOptions.AnyAsync(x => x.Type == model.Type && x.Name == model.Name, cancellationToken);
            if (!exists)
            {
                var sortOrder = await db.CarAttributeOptions
                    .Where(x => x.Type == model.Type)
                    .MaxAsync(x => (int?)x.SortOrder, cancellationToken) ?? 0;

                db.CarAttributeOptions.Add(new Domain.Entities.CarAttributeOption
                {
                    Type = model.Type,
                    Name = model.Name.Trim(),
                    SortOrder = sortOrder + 1
                });
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return RedirectToAction(nameof(ReferenceData));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAttributeOption(int id, CancellationToken cancellationToken)
    {
        var option = await db.CarAttributeOptions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (option is not null)
        {
            option.IsActive = !option.IsActive;
            await db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(ReferenceData));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAuction(int id, CancellationToken cancellationToken)
    {
        var auction = await db.Auctions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (auction is not null)
        {
            auction.Status = AuctionStatus.Cancelled;
            AddAudit("Anulare licitatie", "Auction", id.ToString(), auction.Title);
            await db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Auctions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAuction(int id, CancellationToken cancellationToken)
    {
        var auction = await db.Auctions
            .Include(x => x.Images)
            .Include(x => x.ConditionReport)
                .ThenInclude(x => x!.Damages)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (auction is null)
        {
            return RedirectToAction(nameof(Auctions));
        }

        auction.WinningBidId = null;
        await db.SaveChangesAsync(cancellationToken);

        var bids = await db.Bids.Where(x => x.AuctionId == id).ToListAsync(cancellationToken);
        var autoBids = await db.AutoBids.Where(x => x.AuctionId == id).ToListAsync(cancellationToken);
        var favorites = await db.Favorites.Where(x => x.AuctionId == id).ToListAsync(cancellationToken);
        var transactions = await db.Transactions.Where(x => x.AuctionId == id).ToListAsync(cancellationToken);
        var reviews = await db.Reviews.Where(x => x.AuctionId == id).ToListAsync(cancellationToken);
        var questions = await db.AuctionQuestions.Where(x => x.AuctionId == id).ToListAsync(cancellationToken);
        var reviewIds = reviews.Select(x => x.Id).ToList();
        var reports = await db.UserReports
            .Where(x => x.AuctionId == id || (x.ReviewId != null && reviewIds.Contains(x.ReviewId.Value)))
            .ToListAsync(cancellationToken);
        var affectedSellerIds = reviews.Select(x => x.SellerId).Distinct().ToList();
        var paymentProofPaths = transactions.Select(x => x.PaymentProofPath).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        db.Bids.RemoveRange(bids);
        db.AutoBids.RemoveRange(autoBids);
        db.Favorites.RemoveRange(favorites);
        db.Transactions.RemoveRange(transactions);
        db.Reviews.RemoveRange(reviews);
        db.AuctionQuestions.RemoveRange(questions);
        db.UserReports.RemoveRange(reports);

        if (auction.ConditionReport is not null)
        {
            db.VehicleDamages.RemoveRange(auction.ConditionReport.Damages);
            db.VehicleConditionReports.Remove(auction.ConditionReport);
        }

        db.AuctionImages.RemoveRange(auction.Images);
        db.Auctions.Remove(auction);
        AddAudit("Stergere licitatie", "Auction", id.ToString(), auction.Title);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var image in auction.Images)
        {
            DeleteUploadedFile(image.FilePath);
        }

        foreach (var paymentProofPath in paymentProofPaths)
        {
            DeleteUploadedFile(paymentProofPath);
        }

        foreach (var sellerId in affectedSellerIds)
        {
            await RefreshSellerRatingAsync(sellerId, cancellationToken);
        }

        TempData["SuccessMessage"] = "Licitatia a fost stearsa.";
        return RedirectToAction(nameof(Auctions));
    }

    private static string GetAuctionStatusLabel(AuctionStatus status)
    {
        return status switch
        {
            AuctionStatus.Draft => "Draft",
            AuctionStatus.Scheduled => "Programată",
            AuctionStatus.Active => "Activă",
            AuctionStatus.Ended => "Încheiată",
            AuctionStatus.Unsold => "Nevândută",
            AuctionStatus.Cancelled => "Anulată",
            _ => status.ToString()
        };
    }

    private static string GetReportStatusLabel(ReportStatus status)
    {
        return status switch
        {
            ReportStatus.Pending => "In asteptare",
            ReportStatus.Resolved => "Rezolvata",
            ReportStatus.Rejected => "Respinsa",
            _ => status.ToString()
        };
    }

    private void DeleteUploadedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !filePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var absolutePath = Path.Combine(environment.WebRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }

    private void AddAudit(string action, string targetType, string targetId, string details)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details
        });
    }

    private static IReadOnlyList<UserReportListItemViewModel> BuildReportItems(
        IReadOnlyList<UserReport> reports,
        IReadOnlyDictionary<string, ApplicationUser> users)
    {
        return reports.Select(report =>
        {
            users.TryGetValue(report.ReporterId, out var reporter);
            var reporterName = reporter is null
                ? "Utilizator"
                : $"{reporter.FirstName} {reporter.LastName}".Trim();
            var reportedUserId = report.TargetType == ReportTargetType.Auction
                ? report.Auction?.SellerId
                : report.Review?.BuyerId;
            ApplicationUser? reportedUser = null;
            if (!string.IsNullOrWhiteSpace(reportedUserId))
            {
                users.TryGetValue(reportedUserId, out reportedUser);
            }
            var reportedUserName = reportedUser is null
                ? "Utilizator indisponibil"
                : $"{reportedUser.FirstName} {reportedUser.LastName}".Trim();

            return new UserReportListItemViewModel
            {
                Id = report.Id,
                ReporterName = string.IsNullOrWhiteSpace(reporterName) ? reporter?.Email ?? "Utilizator" : reporterName,
                ReportedUserName = string.IsNullOrWhiteSpace(reportedUserName) ? reportedUser?.Email ?? "Utilizator indisponibil" : reportedUserName,
                ReportedUserEmail = reportedUser?.Email ?? string.Empty,
                TargetType = report.TargetType,
                AuctionId = report.AuctionId,
                AuctionTitle = report.Auction?.Title,
                ReviewId = report.ReviewId,
                ReviewComment = report.Review?.Comment,
                Reason = report.Reason,
                Status = report.Status,
                CreatedAt = report.CreatedAt
            };
        }).ToList();
    }

    private static IReadOnlyList<string> GetReportedUserIds(IEnumerable<UserReport> reports)
    {
        return reports
            .Select(report => report.TargetType == ReportTargetType.Auction ? report.Auction?.SellerId : report.Review?.BuyerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
    }

    private async Task RefreshSellerRatingAsync(string sellerId, CancellationToken cancellationToken)
    {
        var ratings = await db.Reviews
            .Where(x => x.SellerId == sellerId)
            .Select(x => x.Rating)
            .ToListAsync(cancellationToken);
        var seller = await userManager.FindByIdAsync(sellerId);
        if (seller is null)
        {
            return;
        }

        seller.RatingCount = ratings.Count;
        seller.RatingAverage = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 1);
        await userManager.UpdateAsync(seller);
    }
}
