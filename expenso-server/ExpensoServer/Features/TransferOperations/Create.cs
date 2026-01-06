using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
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

    [CannotTransferToSameAccount]
    public class Request
    {
        [GuidRequired(ErrorMessage = "FromAccountId is required.")]
        public Guid FromAccountId { get; set; }

        [GuidRequired(ErrorMessage = "ToAccountId is required.")]
        public Guid ToAccountId { get; set; }

        [Range(0.0000001, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }

        [DecimalGreaterThanZeroIfHasValue(ErrorMessage = "ExchangeRate must be greater than zero if provided.")]
        public decimal? ExchangeRate { get; set; }
    }


    [AttributeUsage(AttributeTargets.Property)]
    public sealed class GuidRequiredAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Guid guid && guid == Guid.Empty)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DecimalGreaterThanZeroIfHasValueAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal decimalValue && decimalValue <= 0)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CannotTransferToSameAccountAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Request request)
            {
                if (request.FromAccountId == request.ToAccountId)
                    return new ValidationResult("Cannot transfer to the same account.", new[] { "FromAccountId", "ToAccountId" });
            }

            return ValidationResult.Success;
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
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{EndpointRoutes.Prefix}/{EndpointRoutes.Segments.TransferOperations}/{operation.Id}";

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