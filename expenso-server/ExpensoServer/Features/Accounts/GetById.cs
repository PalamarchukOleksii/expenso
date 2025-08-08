using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class GetById
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", HandleAsync)
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new Response(a.Id, a.Name, a.Balance, a.Currency.ToString()))
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (response is null)
            return TypedResults.Problem(
                title: "Account Not Found",
                detail: $"The account with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        return TypedResults.Ok(response);
    }
}