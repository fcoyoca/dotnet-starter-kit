using Microsoft.AspNetCore.Http;

namespace FSH.Modules.Patient.Services;

/// <summary>
/// Resolves a snapshotted signature path/URL to an absolute URL: absolute URIs (S3) pass through;
/// relative local-storage paths are prefixed with the API origin. Mirrors Administration's resolver.
/// </summary>
public static class ReportSignatureUrl
{
    public static string? Resolve(string? stored, IHttpContextAccessor httpContextAccessor)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        if (Uri.TryCreate(stored, UriKind.Absolute, out _))
        {
            return stored;
        }

        HttpRequest? request = httpContextAccessor?.HttpContext?.Request;
        if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
        {
            string baseUri = $"{request.Scheme}://{request.Host.Value}{request.PathBase}".TrimEnd('/');
            return $"{baseUri}/{stored.TrimStart('/')}";
        }

        return stored.StartsWith('/') ? stored : $"/{stored}";
    }
}
