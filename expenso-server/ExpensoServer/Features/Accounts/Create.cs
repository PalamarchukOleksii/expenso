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

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .WithRequestValidation<Request>();
        }
    }

    public record Request(string Name, decimal Balance, Currency Currency);

    public record Response(Guid Id, string Name, decimal Balance, Currency Currency);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long.")
                .MaximumLength(50).WithMessage("Name must be at most 100 characters long.");

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance must be zero or positive.");

            RuleFor(x => x.Currency)
                .IsInEnum().WithMessage("Invalid currency.");
        }
    }

    private static async Task<Results<Created<Response>, Conflict>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var existedAccount = await dbContext.GetAccountByUserIdAndNameAsync(userId, request.Name, cancellationToken);
        if (existedAccount is not null)
            return TypedResults.Conflict();

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Balance = request.Balance,
            Currency = request.Currency
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(account.Id, account.Name, account.Balance, account.Currency);
        return TypedResults.Created($"api/accounts/{account.Id}", response);
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

    private static async Task<bool> IsUserExistByIdAsync(this ApplicationDbContext dbContext, Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
    }
}