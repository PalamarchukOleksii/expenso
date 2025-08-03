using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class UpdateAccount
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("{id:guid}", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);;
        }
    }

    public record Request(string? Name, decimal? Balance, Currency? Currency);
    public record Response(Guid Id, string Name, decimal Balance, Currency Currency);

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
                .IsInEnum().WithMessage("Invalid currency.")
                .When(x => x.Currency.HasValue);
        }
    }

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var account = await dbContext.GetAccountByUserIdAndAccountIdAsync(userId, id, cancellationToken);
        if (account == null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title :"NotFound",
                detail: $"No account found with ID '{id}' for the current user.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            );

        if (request.Name is not null && request.Name != account.Name)
        {
            var existingAccount =
                await dbContext.GetAccountByUserIdAndNameAsync(userId, request.Name, cancellationToken);
            if (existingAccount is not null)
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    detail: $"An account with the name '{request.Name}' already exists for this user.",
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"
                );
        }

        if (request.Name is not null) account.Name = request.Name;
        if (request.Balance is not null) account.Balance = request.Balance.Value;
        if (request.Currency is not null) account.Currency = request.Currency.Value;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(account.Id, account.Name, account.Balance, account.Currency);
        return TypedResults.Ok(response);
    }

    private static async Task<Account?> GetAccountByUserIdAndAccountIdAsync(
        this ApplicationDbContext context,
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId,
            cancellationToken);
    }

    private static async Task<Account?> GetAccountByUserIdAndNameAsync(
        this ApplicationDbContext dbContext,
        Guid userId,
        string accountName,
        CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name == accountName, cancellationToken);
    }
}