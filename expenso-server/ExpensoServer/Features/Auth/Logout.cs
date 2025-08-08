using ExpensoServer.Common.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpensoServer.Features.Auth;

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