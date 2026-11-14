using System;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace StandIn.AspNetCore.Tests;

public class UserAndOptionsTests
{
    [Fact]
    public void User_uses_default_claim_types()
    {
        var user = new StandInUser("u1", "User One");

        Assert.Equal("name", user.NameClaimType);
        Assert.Equal("role", user.RoleClaimType);
    }

    [Fact]
    public void User_without_id_gets_a_guid()
    {
        var user = new StandInUser("User One");

        Assert.True(Guid.TryParse(user.Id, out _));
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData(" ", "x")]
    [InlineData("x", "")]
    [InlineData("x", "  ")]
    public void User_rejects_empty_values(string id, string name)
    {
        Assert.Throws<ArgumentException>(() => new StandInUser(id, name));
    }

    [Fact]
    public void AddRole_and_AddName_use_the_users_claim_types()
    {
        var user = new StandInUser("u1", "User One", "given_name", "groups")
            .AddName("Ada")
            .AddRole("Admin");

        Assert.Contains(user.Claims, c => c.Type == "given_name" && c.Value == "Ada");
        Assert.Contains(user.Claims, c => c.Type == "groups" && c.Value == "Admin");
    }

    [Fact]
    public void Principal_contains_id_name_roles_and_no_duplicates()
    {
        var user = new StandInUser("u1", "User One").AddName("User One").AddRole("Admin");

        var principal = user.ToPrincipal("StandIn", accessToken: "tok");

        Assert.Equal("User One", principal.Identity?.Name);
        Assert.True(principal.IsInRole("Admin"));
        Assert.Equal("u1", principal.FindFirst(StandInDefaults.UserIdClaimType)?.Value);
        Assert.Equal("tok", principal.FindFirst(StandInDefaults.AccessTokenClaimType)?.Value);
        Assert.Equal(1, principal.Claims.Count(c => c.Type == "name"));
        Assert.Equal("u1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void Options_reject_duplicate_user_ids()
    {
        var options = new StandInOptions().AddUser(new StandInUser("u1", "One"));

        Assert.Throws<InvalidOperationException>(() => options.AddUser(new StandInUser("u1", "Other")));
    }

    [Fact]
    public void AddUser_marks_the_source_as_local()
    {
        var options = new StandInOptions().AddUser(new StandInUser("u1", "One"));

        Assert.Equal(StandInDefaults.LocalSource, options.Users.Single().Source);
    }

    [Fact]
    public void Root_path_is_normalized()
    {
        var options = new StandInOptions { RootPath = "dev/" };

        Assert.Equal("/dev", options.RootPath);
        Assert.Equal("/dev/sign-in", options.SignInPath);
    }

    [Fact]
    public void Json_in_stuntman_format_is_loaded()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, """
                {
                  "Users": [
                    { "Id": "a", "Name": "Ann", "AccessToken": "tok-a", "Description": "Admin",
                      "Claims": [ { "Type": "role", "Value": "Administrator" } ] },
                    { "Id": "b", "Name": "Ben" }
                  ]
                }
                """);

            var options = new StandInOptions().AddUsersFromJson(path, user => user.AddClaim("tenant", "t1"));

            Assert.Equal(2, options.Users.Count);

            var ann = options.Users.Single(u => u.Id == "a");
            Assert.Equal("tok-a", ann.AccessToken);
            Assert.Equal("Admin", ann.Description);
            Assert.Equal(path, ann.Source);
            Assert.Contains(ann.Claims, c => c.Type == "role" && c.Value == "Administrator");
            Assert.Contains(ann.Claims, c => c.Type == "tenant" && c.Value == "t1");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Json_without_required_fields_is_rejected()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "{ \"Users\": [ { \"Id\": \"a\" } ] }");

            Assert.Throws<InvalidDataException>(() => new StandInOptions().AddUsersFromJson(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryAddConfigurationFromServer_reports_errors_instead_of_throwing()
    {
        Exception? reported = null;

        // Port 1 ist nicht belegt: die Verbindung schlaegt sofort fehl.
        new StandInOptions().TryAddConfigurationFromServer("http://127.0.0.1:1/", e => reported = e);

        Assert.NotNull(reported);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/home?x=1", true)]
    [InlineData("//evil.example", false)]
    [InlineData("/\\evil.example", false)]
    [InlineData("https://evil.example/x", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Return_urls_must_be_local(string? url, bool expected)
    {
        Assert.Equal(expected, new StandInOptions().IsAllowedReturnUrl(url));
    }

    [Fact]
    public void Allowed_origins_permit_a_frontend_on_another_port()
    {
        var options = new StandInOptions().AddAllowedReturnOrigin("https://localhost:5173/");

        Assert.True(options.IsAllowedReturnUrl("https://localhost:5173/dashboard"));
        Assert.False(options.IsAllowedReturnUrl("https://localhost:5174/dashboard"));
    }
}
