using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
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
            app.MapGet("/{id:guid}", HandleAsync)
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound);
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

    private static async Task<IResult> HandleAsync(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Operations
            .Where(x => x.UserId == userId && x.Type == OperationType.Transfer && x.Id == id)
            .Select(x => new Response(
                x.Id,
                x.FromAccountId!.Value,
                x.ToAccountId!.Value,
                x.Amount,
                x.Currency.ToString(),
                x.Timestamp,
                x.Note,
                x.ConvertedAmount,
                x.ConvertedCurrency.HasValue ? x.ConvertedCurrency.Value.ToString() : null,
                x.ExchangeRate))
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
            return TypedResults.Problem(
                title: "Transfer Operation Not Found",
                detail: $"Transfer operation with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        return TypedResults.Ok(response);
    }
}