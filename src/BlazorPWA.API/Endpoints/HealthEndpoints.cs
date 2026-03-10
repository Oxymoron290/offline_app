using BlazorPWA.API.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorPWA.API.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/health", async (AppDbContext db) =>
        {
            var checks = new Dictionary<string, string>();

            try
            {
                await db.Database.CanConnectAsync();
                checks["database"] = "healthy";
            }
            catch
            {
                checks["database"] = "unhealthy";
            }

            checks["api"] = "healthy";

            var overallHealthy = checks.Values.All(v => v == "healthy");
            return overallHealthy ? Results.Ok(checks) : Results.Json(checks, statusCode: 503);
        }).WithTags("Health").WithName("HealthCheck");
    }
}
