using Microsoft.AspNetCore.Http;

namespace FSH.Modules.Administration.Services;

internal static class ProviderSignatureUrl
{
    public static string? Resolve(string? stored, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        // Absolute URLs (e.g., S3) pass through unchanged.
        if (Uri.TryCreate(stored, UriKind.Absolute, out _))
        {
            return stored;
        }

        // For relative paths from local storage, prefix with the API origin.
        HttpRequest? request = httpContextAccessor.HttpContext?.Request;
        if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
        {
            return $"{request.Scheme}://{request.Host.Value}{request.PathBase}".TrimEnd('/') + "/" + stored.TrimStart('/');
        }

        // Fallback: ensure the path starts with a slash.
        return stored.StartsWith('/') ? stored : "/" + stored;
    }
}
