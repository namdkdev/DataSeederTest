using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataSeederTest;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=dataseeder_test;Username=postgres;Password=your_password";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.EnableRetryOnFailure();
            npgsql.CommandTimeout(0);
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
