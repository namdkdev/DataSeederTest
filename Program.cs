using System.Diagnostics;
using System.Text.Json;
using DataSeederTest;
using Microsoft.EntityFrameworkCore;

var runSeeder = true;

var connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING")
?? "Host=localhost;Port=5432;Database=dataseeder_test;Username=postgres;Password=drakekarto";

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
        await DataSeederRunner.RunAsync(db);

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

    var usersStopwatch = Stopwatch.StartNew();
    var users = await db.Users
        .AsNoTracking()
        .ToListAsync();

    usersStopwatch.Stop();

    Console.WriteLine($"users preview: {users.Count:N0} rows in {usersStopwatch.ElapsedMilliseconds:N0} ms");

    var usersJson = JsonSerializer.Serialize(users.First(), new JsonSerializerOptions
    {
        WriteIndented = true,
    });
    Console.WriteLine(usersJson);
}
