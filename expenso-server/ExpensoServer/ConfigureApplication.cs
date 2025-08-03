using ExpensoServer.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer;

public static class ConfigureApplication
{
    public static async Task Configure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseReDoc(options => { options.SpecUrl = "/openapi/v1.json"; });

            await app.EnsureDatabaseCreated();
        }

        app.UseHttpLogging();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapEndpoints();
    }

    private static async Task EnsureDatabaseCreated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}