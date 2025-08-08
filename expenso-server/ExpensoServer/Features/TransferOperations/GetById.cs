using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

public static class GetById
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", HandleAsync);
        }
    }

    public record Response(
        Guid Id,
        Guid FromAccountId,
        Guid ToAccountId,
        decimal Amount,
        string Currency,
        DateTime Timestamp,
        string? Note,
        decimal? ConvertedAmount = null,
        string? ConvertedCurrency = null,
        decimal? ExchangeRate = null
    );

    private static async Task<IResult> HandleAsync(Guid id, ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Operations
            .Where(x => x.UserId == userId && x.Type == OperationType.Transfer)
            .Select(x => new Response(
                x.Id,
                x.FromAccountId!.Value,
                x.ToAccountId!.Value,
                x.Amount,
                x.Currency.ToString(),
                x.Timestamp,
                x.Note,
                x.ConvertedAmount,
                x.ConvertedCurrency!.Value.ToString(),
                x.ExchangeRate!.Value))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}