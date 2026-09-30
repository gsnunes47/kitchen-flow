using Microsoft.EntityFrameworkCore;
using KitchenFlow.Data;

public static class DatabaseInit
{
    public static IServiceCollection AddPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres não foi configurada.");

        services.AddDbContext<KitchenFlow.Data.DbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KitchenFlow.Data.DbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
