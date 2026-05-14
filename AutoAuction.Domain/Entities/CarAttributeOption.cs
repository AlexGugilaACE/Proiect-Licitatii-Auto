using AutoAuction.Domain.Enums;

namespace AutoAuction.Domain.Entities;

public class CarAttributeOption
{
    public int Id { get; set; }
    public AttributeOptionType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
