using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
                .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
        }
    }

    public record Request(string Name, decimal Balance, Currency Currency);

    public record Response(Guid Id, string Name, decimal Balance, Currency Currency);

    public record ErrorResponse(string Message);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance must be zero or positive.");

            RuleFor(x => x.Currency)
                .IsInEnum().WithMessage("Invalid currency.");
        }
    }

    private static async
        Task<Results<Created<Response>, Conflict<ErrorResponse>, NotFound<ErrorResponse>, UnauthorizedHttpResult>>
        HandleAsync(Request request, ApplicationDbContext dbContext, HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return TypedResults.Unauthorized();

        if (!await dbContext.IsUserExistByIdAsync(userId, cancellationToken))
            return TypedResults.NotFound(new ErrorResponse("User not found."));

        var existedAccount = await dbContext.GetAccountByUserIdAndNameAsync(userId, request.Name, cancellationToken);
        if (existedAccount is not null)
            return TypedResults.Conflict(new ErrorResponse($"Account with name '{request.Name}' already exists."));

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
        return TypedResults.Created($"/accounts/{account.Id}", response);
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