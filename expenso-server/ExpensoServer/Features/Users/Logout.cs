using ExpensoServer.Common.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpensoServer.Features.Users;

public static class Logout
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/logout", HandleAsync);
        }
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }
}