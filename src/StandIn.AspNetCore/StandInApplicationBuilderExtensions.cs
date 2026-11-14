using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace StandIn.AspNetCore;

public static class StandInApplicationBuilderExtensions
{
    /// <summary>
    /// Inserts the user picker automatically before <c>&lt;/body&gt;</c> in HTML pages (only GET requests that expect HTML).
    /// Add it after <c>UseAuthentication()</c>. If StandIn is not active in this environment, nothing happens.
    /// </summary>
    public static IApplicationBuilder UseStandInPicker(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices.GetStandInOptions();

        if (options is null)
        {
            return app;
        }

        return app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            if (!WantsHtmlPage(context.Request, options))
            {
                await next(context);
                return;
            }

            // The response is buffered because the picker is inserted into the finished HTML text.
            var original = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next(context);
            }
            finally
            {
                context.Response.Body = original;
            }

            var body = buffer.ToArray();

            if (IsUtf8Html(context.Response))
            {
                var html = Encoding.UTF8.GetString(body);
                var end = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

                if (end >= 0)
                {
                    body = Encoding.UTF8.GetBytes(html.Insert(end, StandInViewHelper.Render(context, returnUrl: null)));
                }
            }

            if (context.Response.ContentLength.HasValue && !context.Response.HasStarted)
            {
                context.Response.ContentLength = body.Length;
            }

            await original.WriteAsync(body);
        });
    }

    private static bool WantsHtmlPage(HttpRequest request, StandInOptions options)
    {
        return HttpMethods.IsGet(request.Method)
            && request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
            && !(options.RootPath.Length > 0 && request.Path.StartsWithSegments(options.RootPath));
    }

    private static bool IsUtf8Html(HttpResponse response)
    {
        var contentType = response.ContentType;

        return response.StatusCode == StatusCodes.Status200OK
            && StringValues.IsNullOrEmpty(response.Headers.ContentEncoding)
            && contentType is not null
            && contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
            && (!contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("charset=utf-8", StringComparison.OrdinalIgnoreCase));
    }
}
