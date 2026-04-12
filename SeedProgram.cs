using Bogus;
using Microsoft.EntityFrameworkCore;

namespace DataSeederTest;

public static class DataSeederRunner
{
    private const int DefaultUserTarget = 10_000_000;
    private const int DefaultUserBatchSize = 50_000;
    private const int DefaultProductCount = 5_000;
    private const int DefaultProductBatchSize = 5_000;
    private const int DefaultOrderCount = 1_000_000;
    private const int DefaultOrderBatchSize = 100_000;
    private const int DefaultActivityLogMultiplier = 3;
    private const int DefaultActivityLogBatchSize = 50_000;

    public static async Task RunAsync(AppDbContext db)
    {
        await SeedUsersAsync(db, DefaultUserTarget, DefaultUserBatchSize);
        await SeedProductsAsync(db, DefaultProductCount, DefaultProductBatchSize);
        await SeedOrdersAsync(db, DefaultOrderCount, DefaultOrderBatchSize);
        await SeedActivityLogsAsync(db, DefaultOrderCount * DefaultActivityLogMultiplier, DefaultActivityLogBatchSize);
        Console.WriteLine("Seed completed.");
    }

    private static async Task SeedUsersAsync(AppDbContext db, int targetUsers, int batchSize)
    {
        var currentCount = await db.Users.LongCountAsync();
        if (currentCount >= targetUsers)
        {
            Console.WriteLine($"users already seeded: {currentCount:N0}/{targetUsers:N0}");
            return;
        }

        Console.WriteLine($"Seeding users from {currentCount:N0} to {targetUsers:N0} (batch: {batchSize:N0})...");

        var faker = new Faker<User>("en")
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.Username, f => f.Internet.UserName())
            .RuleFor(x => x.Email, f => f.Internet.Email().ToLowerInvariant())
            .RuleFor(x => x.PasswordHash, f => f.Random.Hash())
            .RuleFor(x => x.FullName, f => f.Name.FullName())
            .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber("+1-###-###-####"))
            .RuleFor(x => x.Address, f => f.Address.StreetAddress())
            .RuleFor(x => x.City, f => f.Address.City())
            .RuleFor(x => x.Country, f => f.Address.Country())
            .RuleFor(x => x.PostalCode, f => f.Address.ZipCode())
            .RuleFor(x => x.CreatedAt, f => f.Date.Past(8).ToUniversalTime())
            .RuleFor(x => x.LastLoginAt, f => f.Random.Bool(0.7f) ? f.Date.Recent(120).ToUniversalTime() : null)
            .RuleFor(x => x.UpdatedAt, f => f.Random.Bool(0.5f) ? f.Date.Recent(30).ToUniversalTime() : null)
            .RuleFor(x => x.IsActive, f => f.Random.Bool(0.95f))
            .RuleFor(x => x.EmailConfirmed, f => f.Random.Bool(0.85f))
            .RuleFor(x => x.PhoneConfirmed, f => f.Random.Bool(0.7f))
            .RuleFor(x => x.Balance, f => decimal.Round(f.Random.Decimal(0, 20000), 2))
            .RuleFor(x => x.LoyaltyPoints, f => f.Random.Int(0, 25000))
            .RuleFor(x => x.PreferredLanguage, f => f.PickRandom("en", "es", "fr", "de", "it"))
            .RuleFor(x => x.TimeZone, f => f.PickRandom("UTC", "America/New_York", "Europe/London", "Asia/Singapore"));

        var inserted = currentCount;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            while (inserted < targetUsers)
            {
                var size = (int)Math.Min(batchSize, targetUsers - inserted);
                var users = faker.Generate(size);
                await db.Users.AddRangeAsync(users);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                inserted += size;
                Console.WriteLine($"users seeded: {inserted:N0}/{targetUsers:N0}");
            }
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    private static async Task SeedProductsAsync(AppDbContext db, int productCount, int batchSize)
    {
        var currentCount = await db.Products.CountAsync();
        if (currentCount >= productCount)
        {
            Console.WriteLine($"products already seeded: {currentCount:N0}/{productCount:N0}");
            return;
        }

        Console.WriteLine($"Seeding products to {productCount:N0}...");

        var faker = new Faker("en");
        while (currentCount < productCount)
        {
            var size = Math.Min(batchSize, productCount - currentCount);
            var products = new List<Product>(size);

            for (var i = 0; i < size; i++)
            {
                var skuIndex = currentCount + i + 1;
                products.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    Name = faker.Commerce.ProductName(),
                    Description = faker.Commerce.ProductDescription(),
                    SKU = $"SKU-{skuIndex:00000000}",
                    Category = faker.Commerce.Categories(1).FirstOrDefault() ?? "General",
                    Brand = faker.Company.CompanyName(),
                    Color = faker.Commerce.Color(),
                    Size = faker.PickRandom("XS", "S", "M", "L", "XL"),
                    Price = decimal.Round(decimal.Parse(faker.Commerce.Price(2m, 500m)), 2),
                    DiscountPrice = faker.Random.Bool(0.3f)
                        ? decimal.Round(decimal.Parse(faker.Commerce.Price(1m, 450m)), 2)
                        : null,
                    Weight = decimal.Round(faker.Random.Decimal(0.1m, 50m), 3),
                    StockQuantity = faker.Random.Int(0, 50_000),
                    ReorderLevel = faker.Random.Int(10, 500),
                    IsActive = faker.Random.Bool(0.95f),
                    IsFeatured = faker.Random.Bool(0.2f),
                    CreatedAt = faker.Date.Past(4).ToUniversalTime(),
                    UpdatedAt = faker.Random.Bool(0.5f) ? faker.Date.Recent(90).ToUniversalTime() : null,
                    LastRestockedAt = faker.Random.Bool(0.4f) ? faker.Date.Recent(60).ToUniversalTime() : null,
                    ImageUrl = faker.Internet.Avatar(),
                    Tags = string.Join(',', faker.Lorem.Words(faker.Random.Int(2, 5)))
                });
            }

            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            currentCount += size;
            Console.WriteLine($"products seeded: {currentCount:N0}/{productCount:N0}");
        }
    }

    private static async Task SeedOrdersAsync(AppDbContext db, int orderCount, int batchSize)
    {
        var currentOrders = await db.Orders.CountAsync();
        if (currentOrders >= orderCount)
        {
            Console.WriteLine($"orders already seeded: {currentOrders:N0}/{orderCount:N0}");
            return;
        }

        var userIds = await db.Users
            .AsNoTracking()
            .Select(x => x.Id)
            .Take(50_000)
            .ToListAsync();
        if (userIds.Count == 0)
        {
            Console.WriteLine("No users found. Skip order seeding.");
            return;
        }

        var products = await db.Products
            .AsNoTracking()
            .Select(x => new { x.Id, x.Price, x.Color, x.Size })
            .ToListAsync();
        if (products.Count == 0)
        {
            Console.WriteLine("No products found. Skip order seeding.");
            return;
        }

        Console.WriteLine($"Seeding orders from {currentOrders:N0} to {orderCount:N0}...");
        var faker = new Faker("en");

        while (currentOrders < orderCount)
        {
            var size = Math.Min(batchSize, orderCount - currentOrders);
            var orders = new List<Order>(size);

            for (var i = 0; i < size; i++)
            {
                var orderDate = faker.Date.Recent(365).ToUniversalTime();
                orders.Add(new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = faker.PickRandom(userIds),
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{faker.Random.Number(100000, 999999)}",
                    OrderDate = orderDate,
                    ShippedDate = faker.Random.Bool(0.65f) ? orderDate.AddDays(faker.Random.Int(1, 5)) : null,
                    DeliveredDate = faker.Random.Bool(0.5f) ? orderDate.AddDays(faker.Random.Int(2, 10)) : null,
                    Status = faker.PickRandom("Pending", "Processing", "Shipped", "Delivered", "Cancelled"),
                    PaymentMethod = faker.PickRandom("CreditCard", "PayPal", "BankTransfer", "CashOnDelivery"),
                    PaymentStatus = faker.PickRandom("Unpaid", "Paid", "Refunded"),
                    ShippingAddress = faker.Address.StreetAddress(),
                    ShippingCity = faker.Address.City(),
                    ShippingCountry = faker.Address.Country(),
                    ShippingPostalCode = faker.Address.ZipCode(),
                    Notes = faker.Random.Bool(0.15f) ? faker.Lorem.Sentence() : null,
                    TrackingNumber = faker.Random.Bool(0.7f) ? faker.Random.AlphaNumeric(12).ToUpperInvariant() : null,
                    CreatedAt = orderDate,
                    UpdatedAt = faker.Random.Bool(0.4f) ? faker.Date.Recent(30).ToUniversalTime() : null,
                    Subtotal = 0m,
                    ShippingFee = 0m,
                    DiscountAmount = 0m,
                    TaxAmount = 0m,
                    TotalAmount = 0m
                });
            }

            await db.Orders.AddRangeAsync(orders);
            await db.SaveChangesAsync();

            var details = new List<OrderDetail>(size * 3);
            foreach (var order in orders)
            {
                var detailCount = faker.Random.Int(1, 4);
                decimal subtotal = 0m;

                for (var d = 0; d < detailCount; d++)
                {
                    var picked = products[faker.Random.Int(0, products.Count - 1)];
                    var quantity = faker.Random.Int(1, 5);
                    var discount = decimal.Round(faker.Random.Decimal(0, picked.Price * 0.15m), 2);
                    var totalPrice = decimal.Round(quantity * (picked.Price - discount), 2);

                    details.Add(new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = picked.Id,
                        Quantity = quantity,
                        UnitPrice = picked.Price,
                        Discount = discount,
                        TotalPrice = totalPrice,
                        Size = picked.Size,
                        Color = picked.Color,
                        CreatedAt = order.CreatedAt
                    });

                    subtotal += totalPrice;
                }

                var shippingFee = decimal.Round(faker.Random.Decimal(0, 30), 2);
                var discountAmount = decimal.Round(faker.Random.Decimal(0, 20), 2);
                var taxAmount = decimal.Round(subtotal * 0.08m, 2);
                var total = decimal.Round(subtotal + shippingFee + taxAmount - discountAmount, 2);

                order.Subtotal = subtotal;
                order.ShippingFee = shippingFee;
                order.DiscountAmount = discountAmount;
                order.TaxAmount = taxAmount;
                order.TotalAmount = total;
            }

            await db.OrderDetails.AddRangeAsync(details);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            currentOrders += size;
            Console.WriteLine($"orders seeded: {currentOrders:N0}/{orderCount:N0}");
        }
    }

    private static async Task SeedActivityLogsAsync(AppDbContext db, int targetLogs, int batchSize)
    {
        var currentCount = await db.ActivityLogs.LongCountAsync();
        if (currentCount >= targetLogs)
        {
            Console.WriteLine($"activity logs already seeded: {currentCount:N0}/{targetLogs:N0}");
            return;
        }

        var userIds = await db.Users
            .AsNoTracking()
            .Select(x => x.Id)
            .Take(100_000)
            .ToListAsync();
        if (userIds.Count == 0)
        {
            Console.WriteLine("No users found. Skip activity log seeding.");
            return;
        }

        var orderIds = await db.Orders
            .AsNoTracking()
            .Select(x => x.Id)
            .Take(100_000)
            .ToListAsync();

        var productIds = await db.Products
            .AsNoTracking()
            .Select(x => x.Id)
            .Take(100_000)
            .ToListAsync();

        Console.WriteLine($"Seeding activity logs from {currentCount:N0} to {targetLogs:N0}...");
        var faker = new Faker("en");

        while (currentCount < targetLogs)
        {
            var size = (int)Math.Min(batchSize, targetLogs - currentCount);
            var logs = new List<ActivityLog>(size);

            for (var i = 0; i < size; i++)
            {
                var entityType = faker.PickRandom("User", "Order", "Product");
                var entityId = entityType switch
                {
                    "Order" when orderIds.Count > 0 => faker.PickRandom(orderIds).ToString(),
                    "Product" when productIds.Count > 0 => faker.PickRandom(productIds).ToString(),
                    _ => faker.PickRandom(userIds).ToString()
                };

                logs.Add(new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserId = faker.PickRandom(userIds),
                    Action = faker.PickRandom("Login", "ViewProduct", "AddToCart", "PlaceOrder", "UpdateProfile", "Logout"),
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValue = faker.Random.Bool(0.25f) ? faker.Lorem.Sentence() : null,
                    NewValue = faker.Random.Bool(0.25f) ? faker.Lorem.Sentence() : null,
                    IpAddress = faker.Internet.Ip(),
                    UserAgent = faker.Internet.UserAgent(),
                    Description = faker.Lorem.Sentence(),
                    ActionTime = faker.Date.Recent(180).ToUniversalTime(),
                    AdditionalData = faker.Random.Bool(0.3f) ? $"{{\"source\":\"{faker.PickRandom("web", "mobile", "api")}\"}}" : null
                });
            }

            await db.ActivityLogs.AddRangeAsync(logs);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            currentCount += size;
            Console.WriteLine($"activity logs seeded: {currentCount:N0}/{targetLogs:N0}");
        }
    }
}
