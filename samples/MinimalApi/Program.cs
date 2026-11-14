using System.Security.Claims;
using StandIn.AspNetCore;

namespace StandIn.Samples.MinimalApi;

/// <summary>REST-API mit StandIn: Bearer-Token und Token-Endpunkt (fuer Clients ohne Cookie).</summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddAuthentication()
            .AddStandIn(builder.Environment, options =>
            {
                options.AddUser(new StandInUser("admin", "Local Admin").AddRole("Administrator").SetAccessToken("dev-admin-token"));
                options.AddUser(new StandInUser("viewer", "Local Viewer").AddRole("Viewer").SetAccessToken("dev-viewer-token"));
                options.EnableTokenEndpoint();
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStandIn();

        app.MapGet("/", () => "StandIn Minimal API sample");
        app.MapGet("/me", (ClaimsPrincipal user) => new { name = user.Identity?.Name }).RequireAuthorization();
        app.MapGet("/admin", () => "admins only").RequireAuthorization(policy => policy.RequireRole("Administrator"));

        app.Run();
    }
}
