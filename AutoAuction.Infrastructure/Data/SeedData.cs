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

        var sellerProfile = await db.DealerProfiles.FirstOrDefaultAsync(x => x.UserId == seller.Id);
        if (sellerProfile is null)
        {
            db.DealerProfiles.Add(new DealerProfile
            {
                UserId = seller.Id,
                CompanyName = "AutoAuction Demo SRL",
                FiscalCode = "RO12345678",
                IsVerified = true
            });
        }
        else if (!sellerProfile.IsVerified)
        {
            sellerProfile.IsVerified = true;
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

        await SeedBrandsAndModelsAsync(db);

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

        await SeedDemoReviewsAsync(db, userManager, seller.Id);
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

    private static async Task SeedBrandsAndModelsAsync(ApplicationDbContext db)
    {
        var brandModels = new Dictionary<string, string[]>
        {
            ["Audi"] = ["A3", "A4", "A5", "A6", "A7", "A8", "Q3", "Q5", "Q7", "Q8", "e-tron"],
            ["BMW"] = ["Seria 1", "Seria 3", "Seria 5", "Seria 7", "X1", "X3", "X5", "X6", "i3", "i4", "iX"],
            ["Volkswagen"] = ["Golf", "Passat", "Polo", "Tiguan", "Touareg", "Arteon", "T-Roc", "ID.3", "ID.4"],
            ["Mercedes-Benz"] = ["A-Class", "C-Class", "E-Class", "S-Class", "CLA", "GLA", "GLC", "GLE", "GLS", "EQC"],
            ["Skoda"] = ["Fabia", "Octavia", "Superb", "Scala", "Kamiq", "Karoq", "Kodiaq", "Enyaq"],
            ["Renault"] = ["Clio", "Megane", "Talisman", "Captur", "Kadjar", "Austral", "Arkana", "Zoe"],
            ["Ford"] = ["Fiesta", "Focus", "Mondeo", "Kuga", "Puma", "Mustang", "Explorer", "Ranger"],
            ["Toyota"] = ["Yaris", "Corolla", "Camry", "C-HR", "RAV4", "Highlander", "Prius", "Land Cruiser"],
            ["Hyundai"] = ["i10", "i20", "i30", "Elantra", "Tucson", "Santa Fe", "Kona", "Ioniq 5"],
            ["Kia"] = ["Rio", "Ceed", "ProCeed", "Sportage", "Sorento", "Stonic", "Niro", "EV6"],
            ["Dacia"] = ["Logan", "Sandero", "Duster", "Jogger", "Spring"],
            ["Peugeot"] = ["208", "308", "508", "2008", "3008", "5008", "Rifter"],
            ["Opel"] = ["Corsa", "Astra", "Insignia", "Mokka", "Crossland", "Grandland", "Zafira"],
            ["Seat"] = ["Ibiza", "Leon", "Arona", "Ateca", "Tarraco"],
            ["Volvo"] = ["S60", "S90", "V60", "V90", "XC40", "XC60", "XC90"],
            ["Nissan"] = ["Micra", "Juke", "Qashqai", "X-Trail", "Leaf", "Navara"],
            ["Mazda"] = ["Mazda2", "Mazda3", "Mazda6", "CX-3", "CX-30", "CX-5", "MX-5"],
            ["Honda"] = ["Jazz", "Civic", "Accord", "HR-V", "CR-V", "e"],
            ["Fiat"] = ["500", "Panda", "Tipo", "Punto", "500X", "Doblo"],
            ["Tesla"] = ["Model 3", "Model S", "Model X", "Model Y"],
            ["Porsche"] = ["911", "Cayenne", "Macan", "Panamera", "Taycan"],
            ["Land Rover"] = ["Range Rover", "Range Rover Sport", "Evoque", "Discovery", "Defender"],
            ["Jeep"] = ["Renegade", "Compass", "Cherokee", "Grand Cherokee", "Wrangler"],
            ["Citroen"] = ["C3", "C4", "C5 Aircross", "Berlingo", "DS3"],
            ["Alfa Romeo"] = ["Giulia", "Stelvio", "Tonale", "Giulietta"],
            ["Mitsubishi"] = ["Space Star", "ASX", "Eclipse Cross", "Outlander", "L200"],
            ["Subaru"] = ["Impreza", "Legacy", "Forester", "Outback", "XV", "BRZ"],
            ["Suzuki"] = ["Swift", "Vitara", "S-Cross", "Ignis", "Jimny"],
            ["Mini"] = ["Cooper", "Clubman", "Countryman", "Paceman"],
            ["Lexus"] = ["CT", "IS", "ES", "NX", "RX", "UX", "LS"]
        };

        foreach (var (brandName, modelNames) in brandModels)
        {
            var brand = await db.Brands.FirstOrDefaultAsync(x => x.Name == brandName);
            if (brand is null)
            {
                brand = new Brand { Name = brandName };
                db.Brands.Add(brand);
                await db.SaveChangesAsync();
            }

            var existingModels = await db.CarModels
                .Where(x => x.BrandId == brand.Id)
                .Select(x => x.Name)
                .ToListAsync();
            var existingSet = existingModels.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var modelName in modelNames)
            {
                if (!existingSet.Contains(modelName))
                {
                    db.CarModels.Add(new CarModel { BrandId = brand.Id, Name = modelName });
                }
            }
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

    private static async Task SeedDemoReviewsAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, string sellerId)
    {
        if (await db.Reviews.AnyAsync(x => x.SellerId == sellerId))
        {
            return;
        }

        var reviewers = new[]
        {
            new { Email = "buyer.review1@autoauction.local", FirstName = "Andrei", LastName = "Popescu" },
            new { Email = "buyer.review2@autoauction.local", FirstName = "Mihai", LastName = "Ionescu" },
            new { Email = "buyer.review3@autoauction.local", FirstName = "Elena", LastName = "Marin" },
            new { Email = "buyer.review4@autoauction.local", FirstName = "Radu", LastName = "Stan" }
        };

        var reviewerUsers = new List<ApplicationUser>();
        foreach (var reviewer in reviewers)
        {
            var user = await userManager.FindByEmailAsync(reviewer.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = reviewer.Email,
                    Email = reviewer.Email,
                    EmailConfirmed = true,
                    FirstName = reviewer.FirstName,
                    LastName = reviewer.LastName
                };

                await userManager.CreateAsync(user, "Buyer123!");
                await userManager.AddToRoleAsync(user, AppRoles.Buyer);
            }

            reviewerUsers.Add(user);
        }

        var sellerAuctions = await db.Auctions
            .Where(x => x.SellerId == sellerId)
            .OrderBy(x => x.Id)
            .Take(4)
            .ToListAsync();

        var reviews = new[]
        {
            new { Rating = 5, Comment = "Comunicare foarte buna si descriere corecta a masinii." },
            new { Rating = 4, Comment = "Proces rapid, documentele au fost pregatite la timp." },
            new { Rating = 5, Comment = "Vanzator serios, masina a corespuns pozelor si raportului." },
            new { Rating = 4, Comment = "Experienta buna per total, raspuns prompt la intrebari." }
        };

        for (var i = 0; i < reviews.Length; i++)
        {
            db.Reviews.Add(new Review
            {
                SellerId = sellerId,
                BuyerId = reviewerUsers[i].Id,
                AuctionId = sellerAuctions.Count > 0 ? sellerAuctions[i % sellerAuctions.Count].Id : null,
                Rating = reviews[i].Rating,
                Comment = reviews[i].Comment,
                CreatedAt = DateTime.UtcNow.AddDays(-(reviews.Length - i))
            });
        }

        var seller = await userManager.FindByIdAsync(sellerId);
        if (seller is not null)
        {
            seller.RatingCount = reviews.Length;
            seller.RatingAverage = Math.Round((decimal)reviews.Average(x => x.Rating), 1);
            await userManager.UpdateAsync(seller);
        }

        await db.SaveChangesAsync();
    }
}
