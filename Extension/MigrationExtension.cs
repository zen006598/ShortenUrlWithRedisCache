using Microsoft.EntityFrameworkCore;
using ShortenUrlWithRedis.Data;

namespace ShortenUrlWithRedis.Extension;

public static class MigrationExtension
{
    public static void ApplyMigrateDatabase(this WebApplication app)
    {
        using var serviceScope = app.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}
