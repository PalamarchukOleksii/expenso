using System.Reflection;
using ExpensoServer.Common.Api;
using ExpensoServer.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpensoServer;

public static class ConfigureServices
{
    public static void AddServices(this WebApplicationBuilder builder)
    {
        builder.AddDatabase();
        builder.AddAuthentication();
        builder.AddValidators();
        builder.AddEndpoints();
        builder.AddSwagger();
    }

    private static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            options.InferSecuritySchemes();
        });
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
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static void AddEndpoints(this WebApplicationBuilder builder)
    {
        var endpointServiceDescriptors = Assembly.GetExecutingAssembly()
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        builder.Services.TryAddEnumerable(endpointServiceDescriptors);
    }
}