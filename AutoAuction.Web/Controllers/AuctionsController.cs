using AutoAuction.Application.DTOs;
using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Helpers;
using AutoAuction.Web.Hubs;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

public class AuctionsController(
    IAuctionService auctionService,
    IReferenceDataService referenceData,
    IFavoriteService favoriteService,
    IWebHostEnvironment environment,
    IHubContext<AuctionHub> hubContext,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public async Task<IActionResult> Index([FromQuery] AuctionIndexViewModel filters, CancellationToken cancellationToken)
    {
        filters.Page = 1;
        await PopulateAuctionIndexModelAsync(filters, pageSize: 12, cancellationToken);
        return View(filters);
    }

    public async Task<IActionResult> Results([FromQuery] AuctionIndexViewModel filters, CancellationToken cancellationToken)
    {
        await PopulateAuctionIndexModelAsync(filters, pageSize: 12, cancellationToken);
        return View(filters);
    }

    private async Task PopulateAuctionIndexModelAsync(AuctionIndexViewModel filters, int pageSize, CancellationToken cancellationToken)
    {
        var search = new AuctionSearchDto
        {
            Query = filters.Query,
            BrandId = filters.BrandId,
            CarModelId = filters.CarModelId,
            FuelTypeId = filters.FuelTypeId,
            TransmissionTypeId = filters.TransmissionTypeId,
            BodyTypeId = filters.BodyTypeId,
            ConditionId = filters.ConditionId,
            MinYear = filters.MinYear,
            MaxYear = filters.MaxYear,
            MinMileage = filters.MinMileage,
            MaxMileage = filters.MaxMileage,
            MinEngineCapacityCm3 = filters.MinEngineCapacityCm3,
            MaxEngineCapacityCm3 = filters.MaxEngineCapacityCm3,
            MinHorsePower = filters.MinHorsePower,
            MaxHorsePower = filters.MaxHorsePower,
            MinPrice = filters.MinPrice,
            MaxPrice = filters.MaxPrice,
            SortBy = filters.SortBy,
            Page = filters.Page,
            PageSize = pageSize
        };

        var result = await auctionService.SearchAuctionsAsync(search, cancellationToken);
        filters.Auctions = result.Items;
        filters.TotalCount = result.TotalCount;
        filters.TotalPages = result.TotalPages;
        filters.Page = result.Page;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var favoriteIds = new HashSet<int>();
            foreach (var auction in filters.Auctions)
            {
                if (await favoriteService.IsFavoriteAsync(userId, auction.Id, cancellationToken))
                {
                    favoriteIds.Add(auction.Id);
                }
            }

            filters.FavoriteAuctionIds = favoriteIds;
        }

        filters.Brands = (await referenceData.GetBrandsAsync(cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == filters.BrandId))
            .Prepend(new SelectListItem("Alege marca", string.Empty))
            .ToList();

        filters.Models = filters.BrandId is > 0
            ? (await referenceData.GetModelsByBrandAsync(filters.BrandId.Value, cancellationToken))
                .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == filters.CarModelId))
                .Prepend(new SelectListItem("Alege modelul", string.Empty))
                .ToList()
            : [new SelectListItem("Alege modelul", string.Empty)];

        filters.FuelTypes = await BuildFilterOptionsAsync(AttributeOptionType.FuelType, "Combustibil", filters.FuelTypeId, cancellationToken);
        filters.TransmissionTypes = await BuildFilterOptionsAsync(AttributeOptionType.TransmissionType, "Transmisie", filters.TransmissionTypeId, cancellationToken);
        filters.BodyTypes = await BuildFilterOptionsAsync(AttributeOptionType.BodyType, "Caroserie", filters.BodyTypeId, cancellationToken);
        filters.Conditions = await BuildFilterOptionsAsync(AttributeOptionType.Condition, "Stare", filters.ConditionId, cancellationToken);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var auction = await auctionService.GetDetailsAsync(id, cancellationToken);
        if (auction is null)
        {
            return NotFound();
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            ViewBag.IsFavorite = await favoriteService.IsFavoriteAsync(userId, id, cancellationToken);
            if (User.IsInRole(AppRoles.Buyer))
            {
                ViewBag.AutoBidMaxAmount = await auctionService.GetAutoBidMaxAmountAsync(id, userId, cancellationToken);
                ViewBag.BidCooldownUntil = await auctionService.GetBidCooldownUntilAsync(id, userId, cancellationToken);
                ViewBag.IsHighestBidder = auction.Bids
                    .OrderByDescending(x => x.Amount)
                    .FirstOrDefault()?.BidderId == userId;
            }
        }

        var seller = await userManager.FindByIdAsync(auction.SellerId);
        if (seller is not null)
        {
            var dealerProfile = await db.DealerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == seller.Id, cancellationToken);
            var fallbackName = $"{seller.FirstName} {seller.LastName}".Trim();
            ViewBag.SellerSummary = new SellerReviewSummaryViewModel
            {
                SellerId = seller.Id,
                CompanyName = !string.IsNullOrWhiteSpace(dealerProfile?.CompanyName)
                    ? dealerProfile.CompanyName
                    : string.IsNullOrWhiteSpace(fallbackName) ? seller.Email ?? "Vanzator" : fallbackName,
                Email = seller.Email ?? string.Empty,
                RatingAverage = seller.RatingAverage,
                RatingCount = seller.RatingCount
            };
        }

        if (auction.Status == AuctionStatus.Ended && !string.IsNullOrWhiteSpace(auction.WinningBid?.BidderId))
        {
            var winner = await userManager.FindByIdAsync(auction.WinningBid.BidderId);
            if (winner is not null)
            {
                ViewBag.WinnerFullName = $"{winner.FirstName} {winner.LastName}".Trim();
                ViewBag.WinnerEmail = winner.Email;
            }
        }

        ViewBag.SimilarAuctions = await auctionService.GetSimilarAuctionsAsync(auction, cancellationToken: cancellationToken);
        ViewBag.Questions = await BuildAuctionQuestionsAsync(id, cancellationToken);

        return View(auction);
    }

    [Authorize(Roles = $"{AppRoles.Buyer},{AppRoles.Seller}")]
    public async Task<IActionResult> Bids(int id, CancellationToken cancellationToken)
    {
        var auction = await auctionService.GetDetailsAsync(id, cancellationToken);
        if (auction is null)
        {
            return NotFound();
        }

        return View(auction);
    }

    public async Task<IActionResult> ModelsByBrand(int brandId, CancellationToken cancellationToken)
    {
        var models = await referenceData.GetModelsByBrandAsync(brandId, cancellationToken);
        return Json(models.Select(x => new { id = x.Id, name = x.Name }));
    }

    [Authorize(Roles = AppRoles.Seller)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var auctions = await auctionService.GetSellerAuctionsAsync(sellerId!, cancellationToken);
        return View(auctions);
    }

    [Authorize(Roles = AppRoles.Seller)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await IsCurrentSellerApprovedAsync(cancellationToken))
        {
            TempData["Error"] = "Contul tau de vanzator trebuie aprobat de administrator inainte sa poti crea licitatii.";
            return RedirectToAction(nameof(Mine));
        }

        var model = await BuildFormModelAsync(new AuctionFormViewModel(), cancellationToken);
        return View(model);
    }

    [Authorize(Roles = AppRoles.Seller)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var auction = await auctionService.GetDetailsAsync(id, cancellationToken);
        if (auction is null || auction.SellerId != sellerId)
        {
            return NotFound();
        }

        if (auction.Status is AutoAuction.Domain.Enums.AuctionStatus.Ended or AutoAuction.Domain.Enums.AuctionStatus.Unsold or AutoAuction.Domain.Enums.AuctionStatus.Cancelled)
        {
            TempData["Error"] = "Licitația finalizată sau anulată nu mai poate fi editată.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new AuctionFormViewModel
        {
            Title = auction.Title,
            Description = auction.Description,
            Vin = auction.Vin,
            BrandId = auction.BrandId,
            CarModelId = auction.CarModelId,
            Year = auction.Year,
            Mileage = auction.Mileage,
            EngineCapacityCm3 = auction.EngineCapacityCm3,
            HorsePower = auction.HorsePower,
            FuelTypeId = auction.FuelTypeId,
            TransmissionTypeId = auction.TransmissionTypeId,
            BodyTypeId = auction.BodyTypeId,
            ConditionId = auction.ConditionId,
            DriveTypeId = auction.DriveTypeId,
            ColorId = auction.ColorId,
            StartingPrice = auction.StartingPrice,
            MinimumBidIncrement = auction.MinimumBidIncrement,
            StartTime = DateTimeDisplay.ToLocal(auction.StartTime),
            EndTime = DateTimeDisplay.ToLocal(auction.EndTime),
            OverallGrade = auction.ConditionReport?.OverallGrade ?? "B",
            ExteriorCondition = auction.ConditionReport?.ExteriorCondition ?? string.Empty,
            InteriorCondition = auction.ConditionReport?.InteriorCondition ?? string.Empty,
            MechanicalCondition = auction.ConditionReport?.MechanicalCondition ?? string.Empty,
            TireCondition = auction.ConditionReport?.TireCondition ?? string.Empty,
            HasAccidentHistory = auction.ConditionReport?.HasAccidentHistory ?? false,
            HasServiceHistory = auction.ConditionReport?.HasServiceHistory ?? false,
            ConditionNotes = auction.ConditionReport?.Notes ?? string.Empty,
            HasBids = auction.Bids.Count > 0,
            IsFinalized = auction.Status is AutoAuction.Domain.Enums.AuctionStatus.Ended or AutoAuction.Domain.Enums.AuctionStatus.Unsold or AutoAuction.Domain.Enums.AuctionStatus.Cancelled,
            ExistingImages = auction.Images
                .OrderBy(x => x.SortOrder)
                .Select(x => new AuctionImageViewModel
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    IsMainImage = x.IsMainImage
                })
                .ToList()
        };

        return View(await BuildFormModelAsync(model, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Seller)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AuctionFormViewModel model, CancellationToken cancellationToken)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var existingAuction = await auctionService.GetDetailsAsync(id, cancellationToken);
        if (existingAuction is null || existingAuction.SellerId != sellerId)
        {
            return NotFound();
        }

        model.HasBids = existingAuction.Bids.Count > 0;
        model.ExistingImages = existingAuction.Images
            .OrderBy(x => x.SortOrder)
            .Select(x => new AuctionImageViewModel
            {
                Id = x.Id,
                FileName = x.FileName,
                FilePath = x.FilePath,
                IsMainImage = x.IsMainImage
            })
            .ToList();

        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        var dto = MapToDto(model);
        var updated = await auctionService.UpdateAsync(id, sellerId, dto, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var uploadedImages = await SaveImagesAsync(model.Images ?? [], cancellationToken);
        if (uploadedImages.Count > 0)
        {
            await auctionService.AddImagesAsync(id, uploadedImages, cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Seller)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int auctionId, int imageId, CancellationToken cancellationToken)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var auction = await auctionService.GetDetailsAsync(auctionId, cancellationToken);
        var image = auction?.Images.FirstOrDefault(x => x.Id == imageId);
        var deleted = await auctionService.DeleteImageAsync(auctionId, imageId, sellerId, cancellationToken);

        if (deleted && image is not null && image.FilePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = image.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(environment.WebRootPath, relativePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        return RedirectToAction(nameof(Edit), new { id = auctionId });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Seller)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(int auctionId, int imageId, CancellationToken cancellationToken)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await auctionService.SetMainImageAsync(auctionId, imageId, sellerId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = auctionId });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Seller)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuctionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await IsCurrentSellerApprovedAsync(cancellationToken))
        {
            TempData["Error"] = "Contul tau de vanzator trebuie aprobat de administrator inainte sa poti crea licitatii.";
            return RedirectToAction(nameof(Mine));
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var dto = MapToDto(model);

        var auction = await auctionService.CreateAsync(sellerId, dto, cancellationToken);
        var uploadedImages = await SaveImagesAsync(model.Images ?? [], cancellationToken);
        if (uploadedImages.Count > 0)
        {
            await auctionService.AddImagesAsync(auction.Id, uploadedImages, cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id = auction.Id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Buyer)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bid(int id, decimal amount, CancellationToken cancellationToken)
    {
        var bidderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await auctionService.PlaceBidAsync(id, bidderId, amount, cancellationToken);
        if (result.Succeeded)
        {
            if (result.BidCreatedAt is not null)
            {
                await hubContext.Clients.Group(AuctionHub.AuctionGroup(id))
                    .SendAsync("BidPlaced", id, result.CurrentPrice, result.BidCreatedAt, cancellationToken);
            }

            foreach (var outbidUserId in result.OutbidUserIds)
            {
                await hubContext.Clients.Group(AuctionHub.UserGroup(outbidUserId))
                    .SendAsync("Outbid", id, result.CurrentPrice, cancellationToken);
            }

            TempData["Success"] = "Oferta a fost plasata.";
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id, string reason, CancellationToken cancellationToken)
    {
        var auction = await db.Auctions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (auction is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Completeaza motivul raportarii.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var alreadyReported = await db.UserReports.AnyAsync(x =>
            x.ReporterId == reporterId &&
            x.TargetType == ReportTargetType.Auction &&
            x.AuctionId == id &&
            x.Status == ReportStatus.Pending,
            cancellationToken);

        if (!alreadyReported)
        {
            db.UserReports.Add(new UserReport
            {
                ReporterId = reporterId,
                TargetType = ReportTargetType.Auction,
                AuctionId = id,
                Reason = reason.Trim()
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        TempData["Success"] = "Raportarea a fost trimisa catre administrator.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Buyer)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoBid(int id, decimal maxAmount, CancellationToken cancellationToken)
    {
        var bidderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await auctionService.ConfigureAutoBidAsync(id, bidderId, maxAmount, cancellationToken);
        if (result.Succeeded)
        {
            if (result.BidCreatedAt is not null)
            {
                await hubContext.Clients.Group(AuctionHub.AuctionGroup(id))
                    .SendAsync("BidPlaced", id, result.CurrentPrice, result.BidCreatedAt, cancellationToken);
            }

            foreach (var outbidUserId in result.OutbidUserIds)
            {
                await hubContext.Clients.Group(AuctionHub.UserGroup(outbidUserId))
                    .SendAsync("Outbid", id, result.CurrentPrice, cancellationToken);
            }

            TempData["Success"] = "Autobid-ul a fost setat.";
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Buyer)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopAutoBid(int id, CancellationToken cancellationToken)
    {
        var bidderId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var disabled = await auctionService.DisableAutoBidAsync(id, bidderId, cancellationToken);
        TempData[disabled ? "Success" : "Error"] = disabled
            ? "Autobid-ul a fost oprit."
            : "Nu exista un autobid activ pentru aceasta licitatie.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceClose(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var closed = await auctionService.ForceCloseAuctionAsync(id, userId, User.IsInRole(AppRoles.Administrator), cancellationToken);
        TempData[closed ? "Success" : "Error"] = closed
            ? "Licitatia a fost oprita fortat."
            : "Nu poti opri fortat aceasta licitatie.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Buyer)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskQuestion(int id, CreateAuctionQuestionViewModel model, CancellationToken cancellationToken)
    {
        var auction = await db.Auctions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (auction is null)
        {
            return NotFound();
        }

        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (auction.SellerId == buyerId)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Intrebarea trebuie completata si poate avea maximum 1000 de caractere.";
            return RedirectToAction(nameof(Details), new { id });
        }

        db.AuctionQuestions.Add(new AuctionQuestion
        {
            AuctionId = id,
            BuyerId = buyerId,
            Question = model.Question.Trim()
        });

        db.Notifications.Add(new Notification
        {
            UserId = auction.SellerId,
            Title = "Intrebare noua",
            Message = $"Ai primit o intrebare pentru licitatia {auction.Title}.",
            Type = NotificationType.AuctionQuestion
        });

        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Intrebarea a fost trimisa vanzatorului.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Seller)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnswerQuestion(int id, AnswerAuctionQuestionViewModel model, CancellationToken cancellationToken)
    {
        var question = await db.AuctionQuestions
            .Include(x => x.Auction)
            .FirstOrDefaultAsync(x => x.Id == model.QuestionId && x.AuctionId == id, cancellationToken);

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (question?.Auction is null || question.Auction.SellerId != sellerId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Raspunsul trebuie completat si poate avea maximum 1000 de caractere.";
            return RedirectToAction(nameof(Details), new { id });
        }

        question.Answer = model.Answer.Trim();
        question.AnsweredAt = DateTime.UtcNow;
        db.Notifications.Add(new Notification
        {
            UserId = question.BuyerId,
            Title = "Raspuns la intrebare",
            Message = $"Vanzatorul a raspuns la intrebarea ta pentru licitatia {question.Auction.Title}.",
            Type = NotificationType.QuestionAnswered
        });

        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Raspunsul a fost publicat.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<AuctionFormViewModel> BuildFormModelAsync(AuctionFormViewModel model, CancellationToken cancellationToken)
    {
        model.Brands = (await referenceData.GetBrandsAsync(cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var selectedBrandId = model.BrandId;
        if (selectedBrandId == 0)
        {
            selectedBrandId = (await referenceData.GetBrandsAsync(cancellationToken)).FirstOrDefault()?.Id ?? 0;
            model.BrandId = selectedBrandId;
        }

        model.Models = (await referenceData.GetModelsByBrandAsync(selectedBrandId, cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == model.CarModelId))
            .ToList();

        model.FuelTypes = await BuildOptionsAsync(AttributeOptionType.FuelType, cancellationToken);
        model.TransmissionTypes = await BuildOptionsAsync(AttributeOptionType.TransmissionType, cancellationToken);
        model.BodyTypes = await BuildOptionsAsync(AttributeOptionType.BodyType, cancellationToken);
        model.Conditions = await BuildOptionsAsync(AttributeOptionType.Condition, cancellationToken);
        model.DriveTypes = await BuildOptionsAsync(AttributeOptionType.DriveType, cancellationToken);
        model.Colors = await BuildOptionsAsync(AttributeOptionType.Color, cancellationToken);

        return model;
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildOptionsAsync(AttributeOptionType type, CancellationToken cancellationToken)
    {
        return (await referenceData.GetOptionsAsync(type, cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private async Task<bool> IsCurrentSellerApprovedAsync(CancellationToken cancellationToken)
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sellerId is null)
        {
            return false;
        }

        return await db.DealerProfiles
            .AnyAsync(x => x.UserId == sellerId && x.IsVerified, cancellationToken);
    }

    private async Task<IReadOnlyList<AuctionQuestionListItemViewModel>> BuildAuctionQuestionsAsync(int auctionId, CancellationToken cancellationToken)
    {
        var questions = await db.AuctionQuestions
            .AsNoTracking()
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var buyerIds = questions.Select(x => x.BuyerId).Distinct().ToList();
        var buyers = await db.Users
            .AsNoTracking()
            .Where(x => buyerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return questions.Select(question =>
        {
            buyers.TryGetValue(question.BuyerId, out var buyer);
            var buyerName = buyer is null ? "Cumparator" : $"{buyer.FirstName} {buyer.LastName}".Trim();

            return new AuctionQuestionListItemViewModel
            {
                Id = question.Id,
                BuyerName = string.IsNullOrWhiteSpace(buyerName) ? buyer?.Email ?? "Cumparator" : buyerName,
                Question = question.Question,
                Answer = question.Answer,
                CreatedAt = question.CreatedAt,
                AnsweredAt = question.AnsweredAt
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildFilterOptionsAsync(AttributeOptionType type, string emptyLabel, int? selectedId, CancellationToken cancellationToken)
    {
        return (await referenceData.GetOptionsAsync(type, cancellationToken))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == selectedId))
            .Prepend(new SelectListItem(emptyLabel, string.Empty))
            .ToList();
    }

    private async Task<IReadOnlyList<(string FileName, string FilePath)>> SaveImagesAsync(IReadOnlyList<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return [];
        }

        var uploadRoot = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadRoot);
        var saved = new List<(string FileName, string FilePath)>();

        foreach (var file in files.Where(x => x.Length > 0))
        {
            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (file.Length > MaxImageSizeBytes)
            {
                continue;
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadRoot, fileName);

            await using var stream = System.IO.File.Create(physicalPath);
            await file.CopyToAsync(stream, cancellationToken);
            saved.Add((file.FileName, $"/uploads/{fileName}"));
        }

        return saved;
    }

    private static AuctionCreateDto MapToDto(AuctionFormViewModel model)
    {
        return new AuctionCreateDto
        {
            Title = model.Title,
            Description = model.Description,
            Vin = model.Vin.Trim().ToUpperInvariant(),
            BrandId = model.BrandId,
            CarModelId = model.CarModelId,
            Year = model.Year,
            Mileage = model.Mileage,
            EngineCapacityCm3 = model.EngineCapacityCm3,
            HorsePower = model.HorsePower,
            FuelTypeId = model.FuelTypeId,
            TransmissionTypeId = model.TransmissionTypeId,
            BodyTypeId = model.BodyTypeId,
            ConditionId = model.ConditionId,
            DriveTypeId = model.DriveTypeId,
            ColorId = model.ColorId,
            StartingPrice = model.StartingPrice,
            MinimumBidIncrement = model.MinimumBidIncrement,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            OverallGrade = model.OverallGrade,
            ExteriorCondition = model.ExteriorCondition,
            InteriorCondition = model.InteriorCondition,
            MechanicalCondition = model.MechanicalCondition,
            TireCondition = model.TireCondition,
            HasAccidentHistory = model.HasAccidentHistory,
            HasServiceHistory = model.HasServiceHistory,
            ConditionNotes = model.ConditionNotes
        };
    }
}
