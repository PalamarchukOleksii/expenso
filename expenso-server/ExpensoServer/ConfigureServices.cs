using ExpensoServer.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer;

public static class ConfigureServices
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.AddDatabase();
        builder.AddAuthentication();
        builder.AddAuthorization();
        builder.AddValidators();
        builder.AddOpenApi();
        builder.AddRequestsLogging();
    }

    private static void AddOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
    }

    private static void AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    }

    private static void AddValidators(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    }

    private static void AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
    }

    private static void AddAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();
    }

    private static void AddRequestsLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpLogging();
    }
}