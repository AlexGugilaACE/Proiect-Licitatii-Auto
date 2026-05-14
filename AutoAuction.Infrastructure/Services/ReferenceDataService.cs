using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class ReferenceDataService(ApplicationDbContext db) : IReferenceDataService
{
    public async Task<IReadOnlyList<Brand>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await db.Brands.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarModel>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        return await db.CarModels.Include(x => x.Brand).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarModel>> GetModelsByBrandAsync(int brandId, CancellationToken cancellationToken = default)
    {
        return await db.CarModels
            .Where(x => x.BrandId == brandId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarAttributeOption>> GetOptionsAsync(AttributeOptionType type, CancellationToken cancellationToken = default)
    {
        return await db.CarAttributeOptions
            .Where(x => x.Type == type && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
