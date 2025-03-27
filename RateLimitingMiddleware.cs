namespace RateLimiterApi
{
    using System.Collections.Concurrent;

    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, List<DateTime>> _userRequests = new();
        private const int LIMIT = 3;
        private static readonly TimeSpan TIME_WINDOW = TimeSpan.FromMinutes(1);

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var user = GetUser(context);
            
            if (user == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var now = DateTime.UtcNow;

            var requests = _userRequests.GetOrAdd(user, _ => new List<DateTime>());

            lock (requests)
            {
                requests.RemoveAll(r => (now - r) > TIME_WINDOW);

                if (requests.Count >= LIMIT)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return;
                }

                requests.Add(now);
            }

            await _next(context);
        }

        private string? GetUser(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                return null;

            var authHeaderValue = authHeader.ToString();
            if (!authHeaderValue.StartsWith("Basic ")) return null;

            try
            {
                var encoded = authHeaderValue["Basic ".Length..];
                var credentialBytes = Convert.FromBase64String(encoded);
                var credentials = System.Text.Encoding.UTF8.GetString(credentialBytes).Split(':');
                return credentials[0]; // username
            }
            catch
            {
                return null;
            }
        }
    }

}
