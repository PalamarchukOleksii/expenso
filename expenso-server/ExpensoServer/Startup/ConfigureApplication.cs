using ExpensoServer.Common.Api;
using ExpensoServer.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Startup;

public static class ConfigureApplication
{
    public static async Task Configure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            await app.EnsureDatabaseCreated();
        }

        app.UseHttpLogging();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapEndpoints("api");
    }

    private static async Task EnsureDatabaseCreated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}