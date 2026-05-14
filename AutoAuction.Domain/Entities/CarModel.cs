namespace AutoAuction.Domain.Entities;

public class CarModel
{
    public int Id { get; set; }
    public int BrandId { get; set; }
    public Brand? Brand { get; set; }
    public string Name { get; set; } = string.Empty;
}
