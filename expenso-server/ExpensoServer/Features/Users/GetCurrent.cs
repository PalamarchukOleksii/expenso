using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class GetCurrent
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/current", HandleAsync)
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public record Response(Guid Id, string Email);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new Response(u.Id, u.Email))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
            return TypedResults.Problem(
                title: "User Not Found",
                detail: $"User with ID '{userId}' was not found.",
                statusCode: StatusCodes.Status404NotFound);

        return TypedResults.Ok(response);
    }
}