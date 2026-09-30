using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PizzaFlow.Data;

public sealed class PizzaFlowDbContextFactory
    : IDesignTimeDbContextFactory<PizzaFlowDbContext>
{
    public PizzaFlowDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres não foi configurada.");

        var options = new DbContextOptionsBuilder<PizzaFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PizzaFlowDbContext(options);
    }
}
