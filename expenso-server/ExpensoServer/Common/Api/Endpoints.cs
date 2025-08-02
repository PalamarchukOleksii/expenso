using ExpensoServer.Features.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;

namespace ExpensoServer.Common.Api;

public static class Endpoints
{
    private const string Users = "users";

    private static readonly OpenApiSecurityScheme SecurityScheme = new()
    {
        Type = SecuritySchemeType.ApiKey,
        Name = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme,
        In = ParameterLocation.Cookie,
        Scheme = CookieAuthenticationDefaults.AuthenticationScheme,
        Description = "Cookie-based authentication",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = CookieAuthenticationDefaults.AuthenticationScheme
        }
    };

    public static void MapEndpoints(this WebApplication app, string? prefix = null)
    {
        var endpoints = app.MapGroup(prefix ?? string.Empty)
            .WithOpenApi();

        endpoints.MapUserEndpoints();
    }

    private static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(Users)
            .WithTags(Users);

        endpoints.MapPublicGroup()
            .MapEndpoint<Register.Endpoint>()
            .MapEndpoint<Login.Endpoint>();

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Logout.Endpoint>();
    }

    private static RouteGroupBuilder MapPublicGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .AllowAnonymous();
    }

    private static RouteGroupBuilder MapAuthorizedGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .RequireAuthorization()
            .WithOpenApi(x => new OpenApiOperation(x)
            {
                Security = [new OpenApiSecurityRequirement { [SecurityScheme] = [] }]
            });
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}