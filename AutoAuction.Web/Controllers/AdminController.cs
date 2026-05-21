using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private const int AdminPageSize = 10;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new AdminDashboardViewModel
        {
            UserCount = await userManager.Users.CountAsync(cancellationToken),
            AuctionCount = await db.Auctions.CountAsync(cancellationToken),
            ActiveAuctionCount = await db.Auctions.CountAsync(x => x.Status == AuctionStatus.Active, cancellationToken),
            TransactionCount = await db.Transactions.CountAsync(cancellationToken),
            TransactionTotal = await db.Transactions.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0,
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
        var items = new List<AdminUserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}",
                Roles = string.Join(", ", roles),
                IsDisabled = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
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

        return RedirectToAction(nameof(Users));
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
            await db.SaveChangesAsync(cancellationToken);
        }

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
}
