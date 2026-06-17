public class SecurityHeadersMiddleware
{
  private readonly RequestDelegate _next;

  public SecurityHeadersMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task Invoke(HttpContext context)
  {
    var headers = context.Response.Headers;

    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    headers["Permissions-Policy"] =
      "geolocation=(), microphone=(), camera=()";

    headers["X-XSS-Protection"] = "0";

    
    //csp
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
    
    
    //if https
    if (context.Request.IsHttps)
    {
      headers["Strict-Transport-Security"] =
        "max-age=31536000; includeSubDomains";
    }
    
    
    // Prevent caching of sensitive auth/MFA responses
    if (context.Request.Path.StartsWithSegments("api/um/ac"))
    {
      headers["Cache-Control"] = "no-store";
      headers["Pragma"] = "no-cache";
      headers["Expires"] = "0";
    }


    await _next(context);
  }
}