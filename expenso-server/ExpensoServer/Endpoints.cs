using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Features.Accounts;
using ExpensoServer.Features.IncomingCategories;
using ExpensoServer.Features.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace ExpensoServer;

public static class Endpoints
{
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

    public static void MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Prefix)
            .WithOpenApi();

        endpoints.MapUserEndpoints();
        endpoints.MapAccountEndpoints();
        endpoints.MapIncomingCategoryEndpoints();
    }

    private static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Segments.Users)
            .WithTags(ApiRoutes.Segments.Users);

        endpoints.MapPublicGroup()
            .MapEndpoint<Register.Endpoint>()
            .MapEndpoint<Login.Endpoint>();

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Logout.Endpoint>();
    }

    private static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Segments.Accounts)
            .WithTags(ApiRoutes.Segments.Accounts);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<CreateAccount.Endpoint>()
            .MapEndpoint<UpdateAccount.Endpoint>()
            .MapEndpoint<DeleteAccount.Endpoint>()
            .MapEndpoint<GetAccountById.Endpoint>();
    }

    private static void MapIncomingCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Segments.IncomingCategories)
            .WithTags(ApiRoutes.Segments.IncomingCategories);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<CreateIncomingCategory.Endpoint>();
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
            .WithMetadata(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized))
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