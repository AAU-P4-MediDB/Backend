using Microsoft.Extensions.Primitives;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task Invoke(HttpContext context)
    {
        // Add security headers before the response is sent
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            var path = context.Request.Path.Value ?? "";
            
            // Security headers
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            headers["X-XSS-Protection"] = "0";

            // CSP
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' https: data:; " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self'; " +
                "connect-src 'self' https:; " +
                "form-action 'self'";

            // HSTS - always send with proper max-age
            var isDevelopment = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
            var hstsMaxAge = isDevelopment ? "86400" : "31536000"; // 1 day vs 1 year
            headers["Strict-Transport-Security"] = 
                $"max-age={hstsMaxAge}; includeSubDomains; preload";

            // Cache Control - prevent caching of sensitive data
            if (path.StartsWith("/api"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
                headers["Expires"] = "0";
            }

            return Task.CompletedTask;
        });

        await _next(context);

        // Add Retry-After to 429 responses
        if (context.Response.StatusCode == 429 && 
            !context.Response.Headers.ContainsKey("Retry-After"))
        {
            context.Response.Headers["Retry-After"] = "60";
        }
    }
}