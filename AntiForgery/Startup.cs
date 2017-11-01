using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AntiForgery
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IAntiforgery antiforgery, ILoggerFactory loggerFactory)
        {
            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                var logger = loggerFactory.CreateLogger("ValidRequestMW");
                
                //Don't validate POST for login
                if (context.Request.Path.Value.Contains("login"))
                {
                    await next();
                    return;
                }
                
                logger.LogInformation(context.Request.Cookies["XSRF-TOKEN"]);
                logger.LogInformation(context.Request.Headers["x-XSRF-TOKEN"]);
                
                //On POST requests it will validate the XSRF header
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    
                    /****************************************************
                     *
                     *
                     * For some reason when the cookie and the header are sent in on the /create POST this validation always fails
                     * 
                     * 
                     ***************************************************/
                    context.Response.StatusCode = 401;
                    
                    logger.LogError("INVALID XSRF TOKEN");
                    return;
                }
                await next();
            });

            app.UseRouter(r =>
            {
                r.MapGet("", async context => { await context.Response.WriteAsync("hello world"); });

                //This returns a XSRF-TOKEN cookie
                //Client will take this value and add it as a X-XSRF-TOKEN header and POST to /create
                r.MapPost("login", async (context) =>
                {
                    antiforgery.SetCookieTokenAndHeader(context);
                    context.Response.Redirect("/");
                });
                
                //If XSRF validaiton is correct we should hit this route
                r.MapPost("create", async context =>
                {
                    context.Response.StatusCode = 201;
                    await context.Response.WriteAsync("Created");
                });
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(x => x.AddConsole());

            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-XSRF-TOKEN";
                options.Cookie.Name = "XSRF-TOKEN";
                options.Cookie.HttpOnly = false;
            });

//            services.AddAuthentication("MyCookieMW")
//                .AddCookie("MyCookieMW", cookieOptions =>
//                {
//                    cookieOptions.Cookie.Name = "MyCookie";
//                    cookieOptions.Cookie.HttpOnly = true;
//                    cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
//                    cookieOptions.SlidingExpiration = true;
//                });

            services.AddRouting();
        }
    }
}