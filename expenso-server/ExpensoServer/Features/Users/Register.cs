using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Features.Users.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Register
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/register", HandleAsync)
                .WithRequestValidation<Request>()
                .ProducesProblem(StatusCodes.Status409Conflict)
                .Produces<Response>();
        }
    }

    public record Request(string Name, string Email, string Password);

    public record Response(Guid Id, string Name, string Email);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long.")
                .MaximumLength(50).WithMessage("Name must not exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var normalizedName = request.Name.Trim();

        var conflictMessage =
            await dbContext.CheckForExistingUserAsync(normalizedName, normalizedEmail, cancellationToken);
        if (conflictMessage is not null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: conflictMessage,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            );

        var passwordHash = HashPassword(request.Password);

        var user = new User
        {
            Name = normalizedName,
            Email = normalizedEmail,
            PasswordHash = passwordHash
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(user.Id, user.Name, user.Email);
        return TypedResults.Created($"api/users/{user.Id}", response);
    }

    private static async Task<string?> CheckForExistingUserAsync(
        this ApplicationDbContext dbContext,
        string name,
        string email,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users
            .Where(u => u.Name == name || u.Email == email)
            .Select(u => new { u.Name, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is null)
            return null;

        if (existingUser.Name == name)
            return "A user with this name already exists.";
        if (existingUser.Email == email)
            return "A user with this email already exists.";

        return null;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(PasswordHasherParameters.SaltSize);
        var valueBytes = Encoding.UTF8.GetBytes(password);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            valueBytes,
            salt,
            PasswordHasherParameters.Iterations,
            PasswordHasherParameters.HashAlgorithmName,
            PasswordHasherParameters.HashSize
        );

        var result = $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        Array.Clear(salt);
        Array.Clear(valueBytes);
        Array.Clear(hash);

        return result;
    }
}