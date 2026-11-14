using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StandIn.AspNetCore;

internal static class StandInViewHelper
{
    /// <summary>Leer, wenn StandIn in dieser Umgebung nicht aktiv ist.</summary>
    public static string Render(HttpContext context, string? returnUrl)
    {
        var services = context.RequestServices;

        if (services.GetService<StandInMarker>() is null)
        {
            return string.Empty;
        }

        var options = services.GetRequiredService<IOptionsMonitor<StandInOptions>>()
            .Get(StandInDefaults.AuthenticationScheme);

        // Ohne Angabe zurueck auf die aktuelle Seite.
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

/// <summary>Razor: <c>&lt;standin-picker /&gt;</c> (braucht <c>@addTagHelper *, StandIn.AspNetCore</c>).</summary>
[HtmlTargetElement("standin-picker", TagStructure = TagStructure.WithoutEndTag)]
public sealed class StandInPickerTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        output.Content.SetHtmlContent(StandInViewHelper.Render(ViewContext.HttpContext, ReturnUrl));
    }
}
