using System.Diagnostics;
using DataSeederTest;
using Microsoft.EntityFrameworkCore;

var runSeeder = false;
const int userTarget = 10_000_000;
const int userBatchSize = 20_000;
const int productCount = 5_000;
const int orderCount = 100_000;

var connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING")
?? "Host=localhost;Port=5432;Database=dataseeder_test;Username=postgres;Password=your_password";

var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString, npgsql =>
    {
        npgsql.CommandTimeout(0);
        npgsql.EnableRetryOnFailure();
    })
    .EnableDetailedErrors()
    .Options;

await using var db = new AppDbContext(dbOptions);
try
{
    Console.WriteLine("Ensuring database and schema...");
    await db.Database.EnsureCreatedAsync();

    Console.WriteLine($"Seeder is {(runSeeder ? "ON" : "OFF")}.");
    if (runSeeder)
        await DataSeederRunner.RunAsync(db, userTarget, userBatchSize, productCount, orderCount);

    await BenchmarkReadAllAsync(db);
}
catch (Exception ex)
{
    Console.WriteLine($"Database run failed: {ex.Message}");
    Console.WriteLine("Set PG_CONNECTION_STRING, e.g.:");
    Console.WriteLine("PG_CONNECTION_STRING=Host=localhost;Port=5432;Database=dataseeder_test;Username=postgres;Password=your_password");
    Environment.ExitCode = 1;
}

return;

static async Task BenchmarkReadAllAsync(AppDbContext db)
{
    Console.WriteLine("Starting LINQ full read benchmark...");
    var totalStopwatch = Stopwatch.StartNew();

    await MeasureAsync("users", db.Users.AsNoTracking());
    await MeasureAsync("products", db.Products.AsNoTracking());
    await MeasureAsync("orders", db.Orders.AsNoTracking());
    await MeasureAsync("order_details", db.OrderDetails.AsNoTracking());

    totalStopwatch.Stop();
    Console.WriteLine($"TOTAL full read benchmark: {totalStopwatch.ElapsedMilliseconds:N0} ms");
}

static async Task MeasureAsync<T>(string tableName, IQueryable<T> query) where T : class
{
    var count = 0L;
    var stopwatch = Stopwatch.StartNew();

    await foreach (var _ in query.AsAsyncEnumerable())
    {
        count++;
    }

    stopwatch.Stop();
    Console.WriteLine($"{tableName}: {count:N0} rows in {stopwatch.ElapsedMilliseconds:N0} ms");
}
