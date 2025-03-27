using RateLimiterApi;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<RateLimitingMiddleware>();

app.MapGet("/api/resource", (HttpContext context) =>
{
    return Results.Ok(new { Message = "Success!" });
});

app.Run();
