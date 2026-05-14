using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoAuction.Web.Models;

public class ReferenceDataAdminViewModel
{
    public IReadOnlyList<Brand> Brands { get; set; } = [];
    public IReadOnlyList<CarModel> Models { get; set; } = [];
    public IReadOnlyList<CarAttributeOption> Options { get; set; } = [];
    public IReadOnlyList<SelectListItem> BrandItems { get; set; } = [];
    public IReadOnlyList<SelectListItem> AttributeTypes { get; set; } = [];
}

public class BrandFormViewModel
{
    public string Name { get; set; } = string.Empty;
}

public class CarModelFormViewModel
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AttributeOptionFormViewModel
{
    public AttributeOptionType Type { get; set; }
    public string Name { get; set; } = string.Empty;
}
