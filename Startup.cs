using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Graph;
using System.Net;
using graph_office.Graph;

namespace graph_office
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        

        public void ConfigureServices(IServiceCollection services)
        {
            // Add Microsoft Identity Platform sign-in
            services.AddSignIn(options =>
            {
                Configuration.Bind("AzureAd", options);

                options.Prompt = "select_account";

                var authCodeHandler = options.Events.OnAuthorizationCodeReceived;
                options.Events.OnAuthorizationCodeReceived = async context => {
                    // Invoke the original handler first
                    // This allows the Microsoft.Identity.Web library to
                    // add the user to its token cache
                    await authCodeHandler(context);

                    var tokenAcquisition = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenAcquisition>() as ITokenAcquisition;

                    var graphClient = GraphServiceClientFactory
                        .GetAuthenticatedGraphClient(async () =>
                        {
                            return await tokenAcquisition
                                .GetAccessTokenForUserAsync(GraphConstants.Scopes);
                        }
                    );

                    // Get user information from Graph
                    var user = await graphClient.Me.Request()
                        .Select(u => new {
                            u.DisplayName,
                            u.Mail,
                            u.UserPrincipalName,
                            u.MailboxSettings
                        })
                        .GetAsync();

                    context.Principal.AddUserGraphInfo(user);

                    // Get the user's photo
                    // If the user doesn't have a photo, this throws
                    try
                    {
                        var photo = await graphClient.Me
                            .Photos["48x48"]
                            .Content
                            .Request()
                            .GetAsync();

                        context.Principal.AddUserGraphPhoto(photo);
                    }
                    catch (ServiceException ex)
                    {
                        if (ex.IsMatch("ErrorItemNotFound"))
                        {
                            context.Principal.AddUserGraphPhoto(null);
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                };

                options.Events.OnAuthenticationFailed = context => {
                    var error = WebUtility.UrlEncode(context.Exception.Message);
                    context.Response
                        .Redirect($"/Home/ErrorWithMessage?message=Authentication+error&debug={error}");
                    context.HandleResponse();

                    return Task.FromResult(0);
                };

                options.Events.OnRemoteFailure = context => {
                    if (context.Failure is OpenIdConnectProtocolException)
                    {
                        var error = WebUtility.UrlEncode(context.Failure.Message);
                        context.Response
                            .Redirect($"/Home/ErrorWithMessage?message=Sign+in+error&debug={error}");
                        context.HandleResponse();
                    }

                    return Task.FromResult(0);
                };
            }, options =>
            {
                Configuration.Bind("AzureAd", options);
            });

            // Add ability to call web API (Graph)
            // and get access tokens
            services.AddWebAppCallsProtectedWebApi(Configuration,
                GraphConstants.Scopes)
                // Use in-memory token cache
                // See https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization
                .AddInMemoryTokenCaches();

            // Require authentication
            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            // Add the Microsoft Identity UI pages for signin/out
            .AddMicrosoftIdentityUI();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
