using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Features.Accounts;
using ExpensoServer.Features.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace ExpensoServer;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Prefix);

        endpoints.MapUserEndpoints();
        endpoints.MapAccountEndpoints();
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
            .MapEndpoint<Create.Endpoint>()
            .MapEndpoint<Update.Endpoint>()
            .MapEndpoint<Delete.Endpoint>()
            .MapEndpoint<GetById.Endpoint>()
            .MapEndpoint<GetAll.Endpoint>();
    }

    private static RouteGroupBuilder MapPublicGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .AllowAnonymous();
    }

    private static RouteGroupBuilder MapAuthorizedGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .RequireAuthorization();
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}