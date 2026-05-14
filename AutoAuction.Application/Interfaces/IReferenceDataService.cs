using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;

namespace AutoAuction.Application.Interfaces;

public interface IReferenceDataService
{
    Task<IReadOnlyList<Brand>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarModel>> GetModelsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarModel>> GetModelsByBrandAsync(int brandId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CarAttributeOption>> GetOptionsAsync(AttributeOptionType type, CancellationToken cancellationToken = default);
}
