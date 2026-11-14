using StandIn.AspNetCore;

namespace StandIn.Samples.Mvc;

/// <summary>MVC mit StandIn: Cookie-Anmeldung und User Picker in der View.</summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services
            .AddAuthentication()
            .AddStandIn(builder.Environment, options =>
            {
                options.AddUser(new StandInUser("admin", "Local Admin").AddRole("Administrator"));
                options.AddUser(new StandInUser("viewer", "Local Viewer").AddRole("Viewer"));
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStandIn();
        app.MapDefaultControllerRoute();

        app.Run();
    }
}
