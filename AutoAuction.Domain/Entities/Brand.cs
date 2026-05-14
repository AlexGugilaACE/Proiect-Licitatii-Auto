namespace AutoAuction.Domain.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<CarModel> Models { get; set; } = new List<CarModel>();
}
