using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KeyLens.Security;

public class UserDataProvider(IHttpContextAccessor httpContextAccessor) : IUserDataProvider
{
    /// <summary>
    /// The Entra object identifier. Connections are owned by it, so a fallback to a name-based claim
    /// would hand one user another's connections if the token shape ever changed.
    /// </summary>
    public Guid CurrentUserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue("oid"), out var id)
            ? id
            : throw new InvalidOperationException("No authenticated user.");
}
