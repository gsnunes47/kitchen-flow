using Microsoft.EntityFrameworkCore;
using PizzaFlow.Data;

public static class DatabaseInit
{
    public static IServiceCollection AddPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres não foi configurada.");

        services.AddDbContext<PizzaFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PizzaFlowDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
