using ExpensoServer.Common.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ExpensoServer.Features.Users;

public static class Logout
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/logout", HandleAsync)
                .Produces(StatusCodes.Status204NoContent);
        }
    }

    private static async Task<NoContent> HandleAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }
}