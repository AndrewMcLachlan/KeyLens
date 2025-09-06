namespace KeyLens.Api;

public class HttpContextAccessTokenProvider(IHttpContextAccessor httpContextAccessor) : IAccessTokenProvider
{
    public string GetAccessToken()
    {
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) == true)
        {
            return authHeader.ToString().Replace("Bearer ", "");
        }

        throw new InvalidOperationException("No access token found.");
    }
}
