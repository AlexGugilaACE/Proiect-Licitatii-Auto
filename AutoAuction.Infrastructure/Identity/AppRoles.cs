namespace AutoAuction.Infrastructure.Identity;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Seller = "Vanzator";
    public const string Buyer = "Cumparator";

    public static readonly string[] All = [Administrator, Seller, Buyer];
}
