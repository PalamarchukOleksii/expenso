using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeOperations;

public static class GetById
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", HandleAsync);
        }
    }

    public record Response(Guid Id, Guid AccountId, Guid CategoryId, decimal Amount, string Currency, DateTime Timestamp, string? Note);

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Operations
            .Where(x => x.UserId == userId && x.Type == OperationType.Income)
            .Select(x => new Response(
                x.Id,
                x.ToAccountId!.Value,
                x.CategoryId!.Value,
                x.Amount,
                x.Currency.ToString(),
                x.Timestamp,
                x.Note))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}