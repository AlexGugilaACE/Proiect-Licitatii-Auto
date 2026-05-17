using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoAuction.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@autoauction.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "AutoAuction"
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
        }

        const string sellerEmail = "seller@autoauction.local";
        var seller = await userManager.FindByEmailAsync(sellerEmail);
        if (seller is null)
        {
            seller = new ApplicationUser
            {
                UserName = sellerEmail,
                Email = sellerEmail,
                EmailConfirmed = true,
                FirstName = "Vanzator",
                LastName = "Demo"
            };
            await userManager.CreateAsync(seller, "Seller123!");
            await userManager.AddToRoleAsync(seller, AppRoles.Seller);
        }

        const string buyerEmail = "buyer@autoauction.local";
        var buyer = await userManager.FindByEmailAsync(buyerEmail);
        if (buyer is null)
        {
            buyer = new ApplicationUser
            {
                UserName = buyerEmail,
                Email = buyerEmail,
                EmailConfirmed = true,
                FirstName = "Cumparator",
                LastName = "Demo"
            };
            await userManager.CreateAsync(buyer, "Buyer123!");
            await userManager.AddToRoleAsync(buyer, AppRoles.Buyer);
        }

        if (!await db.Brands.AnyAsync())
        {
            var bmw = new Brand { Name = "BMW" };
            var audi = new Brand { Name = "Audi" };
            var vw = new Brand { Name = "Volkswagen" };
            db.Brands.AddRange(bmw, audi, vw);
            db.CarModels.AddRange(
                new CarModel { Brand = bmw, Name = "Seria 3" },
                new CarModel { Brand = bmw, Name = "X5" },
                new CarModel { Brand = audi, Name = "A4" },
                new CarModel { Brand = audi, Name = "Q5" },
                new CarModel { Brand = vw, Name = "Golf" },
                new CarModel { Brand = vw, Name = "Passat" });
        }

        if (!await db.CarAttributeOptions.AnyAsync())
        {
            AddOptions(db, AttributeOptionType.FuelType, "Benzina", "Diesel", "Hibrid", "Electric", "GPL");
            AddOptions(db, AttributeOptionType.TransmissionType, "Manuala", "Automata");
            AddOptions(db, AttributeOptionType.BodyType, "Sedan", "Hatchback", "Break", "SUV", "Coupe", "Cabrio");
            AddOptions(db, AttributeOptionType.Condition, "Noua", "Rulata", "Avariata");
            AddOptions(db, AttributeOptionType.DriveType, "Fata", "Spate", "Integrala");
            AddOptions(db, AttributeOptionType.Color, "Alb", "Negru", "Gri", "Rosu", "Albastru", "Argintiu");
        }

        await db.SaveChangesAsync();

        if (!await db.Auctions.AnyAsync())
        {
            await SeedDemoAuctionsAsync(db, seller.Id);
        }
    }

    private static void AddOptions(ApplicationDbContext db, AttributeOptionType type, params string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            db.CarAttributeOptions.Add(new CarAttributeOption
            {
                Type = type,
                Name = names[i],
                SortOrder = i + 1
            });
        }
    }

    private static async Task SeedDemoAuctionsAsync(ApplicationDbContext db, string sellerId)
    {
        var bmw = await db.Brands.FirstAsync(x => x.Name == "BMW");
        var audi = await db.Brands.FirstAsync(x => x.Name == "Audi");
        var bmwModel = await db.CarModels.FirstAsync(x => x.BrandId == bmw.Id && x.Name == "Seria 3");
        var audiModel = await db.CarModels.FirstAsync(x => x.BrandId == audi.Id && x.Name == "A4");

        var fuelDiesel = await OptionAsync(db, AttributeOptionType.FuelType, "Diesel");
        var fuelBenzina = await OptionAsync(db, AttributeOptionType.FuelType, "Benzina");
        var manual = await OptionAsync(db, AttributeOptionType.TransmissionType, "Manuala");
        var automatic = await OptionAsync(db, AttributeOptionType.TransmissionType, "Automata");
        var sedan = await OptionAsync(db, AttributeOptionType.BodyType, "Sedan");
        var rulata = await OptionAsync(db, AttributeOptionType.Condition, "Rulata");
        var spate = await OptionAsync(db, AttributeOptionType.DriveType, "Spate");
        var fata = await OptionAsync(db, AttributeOptionType.DriveType, "Fata");
        var negru = await OptionAsync(db, AttributeOptionType.Color, "Negru");
        var gri = await OptionAsync(db, AttributeOptionType.Color, "Gri");

        db.Auctions.AddRange(
            new Auction
            {
                SellerId = sellerId,
                BrandId = bmw.Id,
                CarModelId = bmwModel.Id,
                Title = "BMW Seria 3 320d",
                Description = "Sedan diesel, intretinut, potrivit pentru demo licitatie.",
                Vin = "WBA8E11020K123456",
                Year = 2020,
                Mileage = 118000,
                EngineCapacityCm3 = 1995,
                HorsePower = 190,
                FuelTypeId = fuelDiesel.Id,
                TransmissionTypeId = automatic.Id,
                BodyTypeId = sedan.Id,
                ConditionId = rulata.Id,
                DriveTypeId = spate.Id,
                ColorId = negru.Id,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddDays(2),
                StartingPrice = 14500,
                CurrentPrice = 14500,
                MinimumBidIncrement = 250,
                Status = AuctionStatus.Active,
                ConditionReport = new VehicleConditionReport
                {
                    OverallGrade = "B",
                    ExteriorCondition = "Urme normale de utilizare",
                    InteriorCondition = "Ingrijit",
                    MechanicalCondition = "Fara probleme cunoscute",
                    TireCondition = "Buna",
                    HasServiceHistory = true,
                    Notes = "Vehicul demo pentru prezentare."
                }
            },
            new Auction
            {
                SellerId = sellerId,
                BrandId = audi.Id,
                CarModelId = audiModel.Id,
                Title = "Audi A4 2.0 TFSI",
                Description = "Benzina, cutie manuala, stare buna.",
                Vin = "WAUZZZF47JA123456",
                Year = 2018,
                Mileage = 132000,
                EngineCapacityCm3 = 1984,
                HorsePower = 190,
                FuelTypeId = fuelBenzina.Id,
                TransmissionTypeId = manual.Id,
                BodyTypeId = sedan.Id,
                ConditionId = rulata.Id,
                DriveTypeId = fata.Id,
                ColorId = gri.Id,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddDays(4),
                StartingPrice = 11900,
                CurrentPrice = 11900,
                MinimumBidIncrement = 200,
                Status = AuctionStatus.Scheduled,
                ConditionReport = new VehicleConditionReport
                {
                    OverallGrade = "B",
                    ExteriorCondition = "Buna",
                    InteriorCondition = "Buna",
                    MechanicalCondition = "Revizie recenta",
                    TireCondition = "Medie",
                    HasServiceHistory = true,
                    Notes = "Licitatia incepe in curand."
                }
            });

        await db.SaveChangesAsync();
    }

    private static Task<CarAttributeOption> OptionAsync(ApplicationDbContext db, AttributeOptionType type, string name)
    {
        return db.CarAttributeOptions.FirstAsync(x => x.Type == type && x.Name == name);
    }
}
