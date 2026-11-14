using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StandIn.AspNetCore;

internal static class StandInViewHelper
{
    /// <summary>Empty if StandIn is not active in this environment.</summary>
    public static string Render(HttpContext context, string? returnUrl)
    {
        var options = context.RequestServices.GetStandInOptions();

        if (options is null)
        {
            return string.Empty;
        }

        // Without a value, back to the current page.
        var target = returnUrl ?? $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

        return StandInPickerRenderer.RenderPicker(options, context.User, context.Request.PathBase.Value ?? string.Empty, target);
    }
}

/// <summary>Razor: <c>@Html.StandInUserPicker()</c></summary>
public static class StandInHtmlHelperExtensions
{
    public static IHtmlContent StandInUserPicker(this IHtmlHelper html, string? returnUrl = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        return new HtmlString(StandInViewHelper.Render(html.ViewContext.HttpContext, returnUrl));
    }
}
