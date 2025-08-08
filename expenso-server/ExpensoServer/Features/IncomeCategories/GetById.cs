using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeCategories;

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

    public record Response(Guid Id, string Name);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Categories
            .Where(c => (c.UserId == userId || c.IsDefault) && c.Type == CategoryType.Income)
            .Select(a => new Response(a.Id, a.Name))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (response is null)
            return TypedResults.Problem(
                title: "Income category Not Found",
                detail: $"Income category with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        return TypedResults.Ok(response);
    }
}