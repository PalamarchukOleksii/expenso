using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Abstractions;
using ExpensoServer.Data;
using ExpensoServer.Filters;
using ExpensoServer.Models;
using ExpensoServer.Shared.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Register
{
    public record Request(string Name, string Email, string Password);

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
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost($"{EndpointTags.Users}/register", HandleAsync)
                .WithOpenApi()
                .WithTags(EndpointTags.Users)
                .AddEndpointFilter<ValidationFilter<Request>>()
                .AllowAnonymous();
        }
    }

    private static async Task<Results<Created, Conflict<string>, ValidationProblem>> HandleAsync(Request request,
        ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var conflictMessage = await CheckForExistingUserAsync(request, dbContext, cancellationToken);
        if (conflictMessage is not null)
            return TypedResults.Conflict(conflictMessage);

        var passwordHash = HashPassword(request.Password);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = passwordHash
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/users/{user.Id}");
    }

    private static async Task<string?> CheckForExistingUserAsync(
        Request request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users
            .Where(u => u.Name == request.Name || u.Email == request.Email)
            .Select(u => new { Login = u.Name, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is null)
            return null;

        if (existingUser.Login == request.Name)
            return "Name is already taken.";
        if (existingUser.Email == request.Email)
            return "Email is already taken.";

        return null;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(PasswordHasherParameters.SaltSize);
        var valueBytes = Encoding.UTF8.GetBytes(password);

        var hash = Rfc2898DeriveBytes.Pbkdf2(valueBytes, salt, PasswordHasherParameters.Iterations,
            PasswordHasherParameters._hashAlgorithmName, PasswordHasherParameters.HashSize);

        var result = $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        Array.Clear(salt);
        Array.Clear(valueBytes);
        Array.Clear(hash);

        return result;
    }
}