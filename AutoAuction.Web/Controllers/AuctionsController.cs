using AutoAuction.Application.DTOs;
using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Hubs;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

public class AuctionsController(
    IAuctionService auctionService,
    IReferenceDataService referenceData,
    IFavoriteService favoriteService,
    IWebHostEnvironment environment,
    IHubContext<AuctionHub> hubContext) : Controller
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public async Task<IActionResult> Index([FromQuery] AuctionIndexViewModel filters, CancellationToken cancellationToken)
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
            Page = filters.Page
        };

        var result = await auctionService.SearchAuctionsAsync(search, cancellationToken);
        filters.Auctions = result.Items;
        filters.TotalCount = result.TotalCount;
        filters.TotalPages = result.TotalPages;
        filters.Page = result.Page;
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

        return View(filters);
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
        }

        return View(auction);
    }

    [Authorize]
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
            TempData["Error"] = "Licitatia finalizata sau anulata nu mai poate fi editata.";
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
            StartTime = auction.StartTime.ToLocalTime(),
            EndTime = auction.EndTime.ToLocalTime(),
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
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(nameof(model.EndTime), "Data de final trebuie sa fie dupa data de start.");
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildFormModelAsync(model, cancellationToken));
        }

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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
        if (model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(nameof(model.EndTime), "Data de final trebuie sa fie dupa data de start.");
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
            await hubContext.Clients.Group(AuctionHub.AuctionGroup(id))
                .SendAsync("BidPlaced", id, result.CurrentPrice, result.BidCreatedAt, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.OutbidUserId))
            {
                await hubContext.Clients.Group(AuctionHub.UserGroup(result.OutbidUserId))
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
