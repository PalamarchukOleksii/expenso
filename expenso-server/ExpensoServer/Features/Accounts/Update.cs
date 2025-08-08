using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
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
                .AddEndpointFilter<RequestValidationFilter<Request>>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public record Request(string? Name, decimal? Balance, string? Currency);

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long.")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters long.")
                .When(x => x.Name is not null);

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance must be zero or positive.")
                .When(x => x.Balance.HasValue);

            RuleFor(x => x.Currency)
                .Must(value => Enum.TryParse<Currency>(value, false, out _))
                .WithMessage("Invalid currency.")
                .When(x => x.Currency is not null);
        }
    }

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