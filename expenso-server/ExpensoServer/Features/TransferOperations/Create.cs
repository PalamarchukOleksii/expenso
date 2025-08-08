using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Constants;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public record Request(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Note, decimal? ExchangeRate);

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

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FromAccountId)
                .NotEmpty().WithMessage("FromAccountId is required.");

            RuleFor(x => x.ToAccountId)
                .NotEmpty().WithMessage("ToAccountId is required.");

            RuleFor(x => x)
                .Must(x => x.FromAccountId != x.ToAccountId)
                .WithMessage("Cannot transfer to the same account.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");

            RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).When(x => x.ExchangeRate.HasValue)
                .WithMessage("ExchangeRate must be greater than zero if provided.");
        }
    }

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var fromAccount = await dbContext.Accounts.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Id == request.FromAccountId, cancellationToken);

        if (fromAccount is null)
            return TypedResults.Problem(
                title: "FromAccount Not Found",
                detail: $"Account with ID '{request.FromAccountId}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        var toAccount = await dbContext.Accounts.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Id == request.ToAccountId, cancellationToken);

        if (toAccount is null)
            return TypedResults.Problem(
                title: "ToAccount Not Found",
                detail: $"Account with ID '{request.ToAccountId}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        if (fromAccount.Currency != toAccount.Currency && !request.ExchangeRate.HasValue)
            return TypedResults.Problem(
                title: "Missing Exchange Rate",
                detail: "ExchangeRate must be provided when transferring between accounts with different currencies.",
                statusCode: StatusCodes.Status400BadRequest);

        var convertedAmount = request.Amount;

        if (fromAccount.Currency != toAccount.Currency) convertedAmount *= request.ExchangeRate!.Value;

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += convertedAmount;

        var operation = new Operation
        {
            UserId = userId,
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            Amount = request.Amount,
            Currency = fromAccount.Currency,
            ConvertedAmount = request.ExchangeRate.HasValue ? convertedAmount : null,
            ConvertedCurrency = request.ExchangeRate.HasValue ? toAccount.Currency : null,
            ExchangeRate = request.ExchangeRate,
            Type = OperationType.Transfer,
            Note = request.Note
        };

        dbContext.Operations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{Routes.Prefix}/{Routes.Segments.TransferOperations}/{operation.Id}";

        var response = new Response(
            operation.Id,
            operation.FromAccountId!.Value,
            operation.ToAccountId!.Value,
            operation.Amount,
            operation.Currency.ToString(),
            operation.Timestamp,
            operation.Note,
            operation.ConvertedAmount,
            operation.ConvertedCurrency?.ToString(),
            operation.ExchangeRate
        );

        return TypedResults.Created(location, response);
    }
}