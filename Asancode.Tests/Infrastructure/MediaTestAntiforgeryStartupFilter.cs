using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Asancode.Tests.Infrastructure;

/// <summary>
/// Exposes a request token only in the in-memory integration-test host.
/// This middleware is never registered by the production application.
/// </summary>
public sealed class MediaTestAntiforgeryStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (HttpMethods.IsGet(context.Request.Method) &&
                    context.Request.Path.Equals("/__test/antiforgery"))
                {
                    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                    var tokens = antiforgery.GetAndStoreTokens(context);

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(
                        new { requestToken = tokens.RequestToken },
                        context.RequestAborted);
                    return;
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}
