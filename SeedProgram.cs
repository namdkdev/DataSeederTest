using Bogus;
using DataSeederTest;
using Microsoft.EntityFrameworkCore;

namespace DataSeederTest;

public static class DataSeederRunner
{
    public static async Task RunAsync(
        AppDbContext db,
        int userTarget,
        int userBatchSize,
        int productCount,
        int orderCount)
    {
        await SeedUsersAsync(db, userTarget, userBatchSize);
        await SeedProductsAsync(db, productCount);
        await SeedOrdersAsync(db, orderCount);
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

        var faker = new Faker<AppUser>("vi")
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.Phone, f => f.Phone.PhoneNumber("0#########"))
            .RuleFor(x => x.CreatedAtUtc, f => f.Date.Past(8).ToUniversalTime())
            .RuleFor(x => x.Email, (f, x) =>
                $"{x.FirstName}.{x.LastName}.{Guid.NewGuid():N}@example.local".ToLowerInvariant());

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

    private static async Task SeedProductsAsync(AppDbContext db, int productCount)
    {
        var currentCount = await db.Products.CountAsync();
        if (currentCount >= productCount)
        {
            Console.WriteLine($"products already seeded: {currentCount:N0}/{productCount:N0}");
            return;
        }

        Console.WriteLine($"Seeding products to {productCount:N0}...");

        var faker = new Faker("en");
        var products = new List<Product>(productCount - currentCount);
        for (var i = currentCount + 1; i <= productCount; i++)
        {
            products.Add(new Product
            {
                Sku = $"SKU-{i:00000000}",
                Name = faker.Commerce.ProductName(),
                Price = decimal.Parse(faker.Commerce.Price(2m, 500m)),
                Stock = faker.Random.Int(0, 50_000)
            });
        }

        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();
    }

    private static async Task SeedOrdersAsync(AppDbContext db, int orderCount)
    {
        var currentOrders = await db.Orders.CountAsync();
        if (currentOrders >= orderCount)
        {
            Console.WriteLine($"orders already seeded: {currentOrders:N0}/{orderCount:N0}");
            return;
        }

        var userCount = await db.Users.LongCountAsync();
        if (userCount == 0)
        {
            Console.WriteLine("No users found. Skip order seeding.");
            return;
        }

        var products = await db.Products.AsNoTracking().Select(x => new { x.Id, x.Price }).ToListAsync();
        if (products.Count == 0)
        {
            Console.WriteLine("No products found. Skip order seeding.");
            return;
        }

        Console.WriteLine($"Seeding orders from {currentOrders:N0} to {orderCount:N0}...");
        var faker = new Faker("en");
        const int batchSize = 5_000;

        while (currentOrders < orderCount)
        {
            var size = Math.Min(batchSize, orderCount - currentOrders);
            var orders = new List<Order>(size);

            for (var i = 0; i < size; i++)
            {
                orders.Add(new Order
                {
                    UserId = faker.Random.Long(1, userCount),
                    OrderedAtUtc = faker.Date.Recent(365).ToUniversalTime(),
                    TotalAmount = 0m
                });
            }

            await db.Orders.AddRangeAsync(orders);
            await db.SaveChangesAsync();

            var details = new List<OrderDetail>(size * 3);
            foreach (var order in orders)
            {
                var detailCount = faker.Random.Int(1, 4);
                decimal total = 0m;

                for (var d = 0; d < detailCount; d++)
                {
                    var picked = products[faker.Random.Int(0, products.Count - 1)];
                    var quantity = faker.Random.Int(1, 5);
                    details.Add(new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = picked.Id,
                        Quantity = quantity,
                        UnitPrice = picked.Price
                    });

                    total += picked.Price * quantity;
                }

                order.TotalAmount = decimal.Round(total, 2);
            }

            await db.OrderDetails.AddRangeAsync(details);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            currentOrders += size;
            Console.WriteLine($"orders seeded: {currentOrders:N0}/{orderCount:N0}");
        }
    }
}
