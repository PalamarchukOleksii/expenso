using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Features;
using ExpensoServer.Features.Auth;

namespace ExpensoServer;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Prefix);

        endpoints.MapUserEndpoints();
        endpoints.MapAccountEndpoints();
        endpoints.MapIncomeCategoryEndpoints();
    }

    private static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Segments.Auth)
            .WithTags(ApiRoutes.Segments.Auth);

        endpoints.MapPublicGroup()
            .MapEndpoint<Features.Auth.Register.Endpoint>()
            .MapEndpoint<Features.Auth.Login.Endpoint>();

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Auth.Logout.Endpoint>();
    }

    private static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Segments.Accounts)
            .WithTags(ApiRoutes.Segments.Accounts);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Accounts.Create.Endpoint>()
            .MapEndpoint<Features.Accounts.Update.Endpoint>()
            .MapEndpoint<Features.Accounts.Delete.Endpoint>()
            .MapEndpoint<Features.Accounts.GetById.Endpoint>()
            .MapEndpoint<Features.Accounts.GetAll.Endpoint>();
    }

    private static void MapIncomeCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(ApiRoutes.Segments.IncomeCategories)
            .WithTags(ApiRoutes.Segments.IncomeCategories);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.IncomeCategories.Create.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.Update.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.Delete.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.GetById.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.GetAll.Endpoint>();
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