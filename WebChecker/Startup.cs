using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Http;
using AhDung.WebChecker.Models;
using System;
using System.IO;
using AhDung.WebChecker.Jobs;
using AhDung.WebChecker.Services;
using AhDung.WebChecker.Services.Jobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AhDung.WebChecker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            AppSettings.Configuration = configuration;
            Vars.EnabledWebs.AddRange(configuration.GetSection("Webs")?.Get<List<Web>>().Where(x => x.Enabled) ?? Enumerable.Empty<Web>());
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddWebEncoders(options => options.TextEncoderSettings = new(UnicodeRanges.All));

            //让应用重启认证cookie也不会失效
            var keyPath = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "keys"));
            services.AddDataProtection()
                    .PersistKeysToFileSystem(keyPath);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                     {
                         options.LoginPath      = "/signin";
                         options.ExpireTimeSpan = TimeSpan.FromDays(31);
                     });

            services.AddHttpClient("default")
                    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                     {
                         PooledConnectionLifetime    = TimeSpan.FromSeconds(AppSettings.Network.PooledConnectionLifetimeInSeconds),
                         PooledConnectionIdleTimeout = TimeSpan.Zero,
                         UseProxy                    = false, //关键，不然回收后请求很慢
                     })
                    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(AppSettings.Network.TimeoutInSeconds));

            services.AddTransient<INotificationService, MailNotificationService>();
            services.AddHostedService<NotifyJob>();

            Vars.EnabledWebs.ForEach(web =>
                //不能用AddHostedService，有BUG，相同类型只会添加一个实例
                services.AddSingleton<IHostedService>(sp => new CheckJob(web,
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("default"),
                    sp.GetService<ILogger<CheckJob>>())));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseStaticFiles();
            app.UseSerilogRequestLogging();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapRazorPages());
        }
    }
}