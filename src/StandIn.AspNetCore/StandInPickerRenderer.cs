using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace StandIn.AspNetCore;

/// <summary>Renders the HTML for the user picker and the login page. Without JavaScript (details/summary).</summary>
internal static class StandInPickerRenderer
{
    private const string Css = """
        .standin{position:fixed;top:0;z-index:2147483647;font:13px/1.4 system-ui,sans-serif}
        .standin-left{left:1rem}.standin-right{right:1rem}.standin-center{left:50%;transform:translateX(-50%)}
        .standin summary{cursor:pointer;background:#1f2937;color:#fff;padding:.25rem .75rem;border-radius:0 0 .5rem .5rem;list-style:none}
        .standin ul{margin:0;padding:.25rem 0;list-style:none;background:#fff;color:#111;border:1px solid #d1d5db;border-radius:.25rem;box-shadow:0 4px 12px rgba(0,0,0,.2)}
        .standin-bottom{top:auto;bottom:0}
        .standin-bottom summary{border-radius:.5rem .5rem 0 0}
        .standin-bottom ul{position:absolute;bottom:100%;left:0;white-space:nowrap}
        .standin a{display:block;padding:.25rem .75rem;color:inherit;text-decoration:none}
        .standin a:hover{background:#e5e7eb}
        .standin .standin-active{font-weight:700}
        """;

    public static string RenderPicker(StandInOptions options, ClaimsPrincipal? principal, string pathBase, string? returnUrl)
    {
        var activeId = principal?.FindFirst(StandInDefaults.UserIdClaimType)?.Value;
        var activeName = options.FindUser(activeId)?.Name;

        var html = new StringBuilder();
        html.Append("<style>").Append(Css).Append("</style>");
        html.Append("<div class=\"standin standin-").Append(AlignmentClass(options.UserPickerAlignment))
            .Append(options.UserPickerVerticalPosition == StandInVerticalPosition.Bottom ? " standin-bottom" : string.Empty)
            .Append("\">");
        html.Append("<details><summary>StandIn: ")
            .Append(WebUtility.HtmlEncode(activeName ?? "anonymous"))
            .Append("</summary><ul>");

        foreach (var user in options.Users)
        {
            var title = string.IsNullOrWhiteSpace(user.Description)
                ? $"Source: {user.Source}"
                : $"{user.Description} Source: {user.Source}";

            html.Append("<li><a")
                .Append(user.Id == activeId ? " class=\"standin-active\"" : string.Empty)
                .Append(" href=\"").Append(WebUtility.HtmlEncode(SignInLink(options, pathBase, user, returnUrl))).Append('"')
                .Append(" title=\"").Append(WebUtility.HtmlEncode(title)).Append("\">")
                .Append(WebUtility.HtmlEncode(user.Name))
                .Append("</a></li>");
        }

        html.Append("<li><a href=\"").Append(WebUtility.HtmlEncode(SignOutLink(options, pathBase, returnUrl)))
            .Append("\">Logout</a></li>");
        html.Append("</ul></details></div>");

        return html.ToString();
    }

    public static string RenderLoginPage(StandInOptions options, PathString pathBase, string? returnUrl)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>StandIn</title>")
            .Append("<style>body{font:16px system-ui,sans-serif;margin:2rem}li{margin:.5rem 0}</style></head><body>")
            .Append("<h1>StandIn: choose a user</h1><ul>");

        var basePath = pathBase.HasValue ? pathBase.Value! : string.Empty;

        foreach (var user in options.Users)
        {
            html.Append("<li><a href=\"")
                .Append(WebUtility.HtmlEncode(SignInLink(options, basePath, user, returnUrl)))
                .Append("\">").Append(WebUtility.HtmlEncode(user.Name)).Append("</a>");

            if (!string.IsNullOrWhiteSpace(user.Description))
            {
                html.Append(" &ndash; ").Append(WebUtility.HtmlEncode(user.Description));
            }

            html.Append("</li>");
        }

        html.Append("</ul></body></html>");
        return html.ToString();
    }

    private static string SignInLink(StandInOptions options, string pathBase, StandInUser user, string? returnUrl)
    {
        var link = $"{pathBase}{options.SignInPath}?{StandInDefaults.OverrideUserIdQueryKey}={Uri.EscapeDataString(user.Id)}";
        return returnUrl is null ? link : $"{link}&{StandInDefaults.ReturnUrlQueryKey}={Uri.EscapeDataString(returnUrl)}";
    }

    private static string SignOutLink(StandInOptions options, string pathBase, string? returnUrl)
    {
        var link = $"{pathBase}{options.SignOutPath}";
        return returnUrl is null ? link : $"{link}?{StandInDefaults.ReturnUrlQueryKey}={Uri.EscapeDataString(returnUrl)}";
    }

    private static string AlignmentClass(StandInAlignment alignment) => alignment switch
    {
        StandInAlignment.Center => "center",
        StandInAlignment.Right => "right",
        _ => "left",
    };
}
