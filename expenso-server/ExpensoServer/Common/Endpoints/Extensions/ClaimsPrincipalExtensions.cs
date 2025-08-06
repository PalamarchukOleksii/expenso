using System.Security.Claims;

namespace ExpensoServer.Common.Endpoints.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        if (!Guid.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            throw new InvalidOperationException("Invalid UserId");

        return id;
    }
}