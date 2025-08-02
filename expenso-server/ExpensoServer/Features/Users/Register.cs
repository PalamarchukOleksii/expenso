using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Constants;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Register
{
    private static async Task<Results<Created<Response>, Conflict<ErrorResponse>>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var normalizedName = request.Name.Trim();

        var conflictMessage =
            await CheckForExistingUserAsync(normalizedName, normalizedEmail, dbContext, cancellationToken);
        if (conflictMessage is not null)
            return TypedResults.Conflict(new ErrorResponse(conflictMessage));

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
        return TypedResults.Created($"/users/{user.Id}", response);
    }

    private static async Task<string?> CheckForExistingUserAsync(
        string name,
        string email,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users
            .Where(u => u.Name == name || u.Email == email)
            .Select(u => new { u.Name, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is null)
            return null;

        if (existingUser.Name == name)
            return "Name is already taken.";
        if (existingUser.Email == email)
            return "Email is already taken.";

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
            PasswordHasherParameters._hashAlgorithmName,
            PasswordHasherParameters.HashSize
        );

        var result = $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        Array.Clear(salt);
        Array.Clear(valueBytes);
        Array.Clear(hash);

        return result;
    }

    public record Request(string Name, string Email, string Password);

    public record Response(Guid Id, string Name, string Email);

    public record ErrorResponse(string Message);

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

    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/register", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>()
                .Produces<Response>(StatusCodes.Status201Created)
                .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
                .ProducesValidationProblem();
        }
    }
}