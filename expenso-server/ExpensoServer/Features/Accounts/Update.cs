using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/{id:guid}", HandleAsync)
                .ProducesValidationProblem()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public class Request
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
        public string? Name { get; set; }

        [BalanceValidation]
        public decimal? Balance { get; set; }

        [CurrencyValidation]
        public string? Currency { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BalanceValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not decimal balance)
                return ValidationResult.Success;

            if (balance < 0)
                return new ValidationResult("Balance must be zero or positive.");

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CurrencyValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string strValue)
                return ValidationResult.Success;

            if (!Enum.TryParse<Currency>(strValue, ignoreCase: true, out _))
                return new ValidationResult("Invalid currency.");

            return ValidationResult.Success;
        }
    }

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        Currency? currencyEnum = null;

        if (request.Currency is not null)
        {
            if (!Enum.TryParse<Currency>(request.Currency, false, out var parsedCurrency))
                return TypedResults.Problem(
                    title: "Invalid Currency",
                    detail: $"The currency '{request.Currency}' is not supported.",
                    statusCode: StatusCodes.Status400BadRequest);
            currencyEnum = parsedCurrency;
        }

        var userId = claimsPrincipal.GetUserId();

        var account = await dbContext.Accounts
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account is null)
            return TypedResults.Problem(
                title: "Account Not Found",
                detail: $"The account with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        if (request.Name is not null && request.Name != account.Name)
        {
            var isNameConflict = await dbContext.Accounts.AnyAsync(x =>
                x.UserId == userId &&
                x.Name == request.Name &&
                x.Id != id, cancellationToken);

            if (isNameConflict)
                return TypedResults.Problem(
                    title: "Account Name Conflict",
                    detail: $"An account with the name '{request.Name}' already exists for this user.",
                    statusCode: StatusCodes.Status409Conflict);
        }

        var hasChanges = false;

        if (request.Name is not null && request.Name != account.Name)
        {
            account.Name = request.Name;
            hasChanges = true;
        }

        if (request.Balance is not null && request.Balance.Value != account.Balance)
        {
            account.Balance = request.Balance.Value;
            hasChanges = true;
        }

        if (currencyEnum is not null && currencyEnum.Value != account.Currency)
        {
            account.Currency = currencyEnum.Value;
            hasChanges = true;
        }

        if (!hasChanges)
            return TypedResults.Ok(
                new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(
            new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));
    }
}