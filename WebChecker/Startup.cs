using AhDung.WebChecker.Services;
using AhDung.WebChecker.Services.Jobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Unicode;

namespace AhDung.WebChecker
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
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
                    .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
                     {
                         PooledConnectionLifetime    = TimeSpan.FromSeconds(sp.GetRequiredService<AppSettings>().Network.PooledConnectionLifetimeInSeconds),
                         PooledConnectionIdleTimeout = TimeSpan.Zero,
                         UseProxy                    = false, //关键，不然回收后请求很慢
                     })
                    .ConfigureHttpClient((sp,client)=> client.Timeout = TimeSpan.FromSeconds(sp.GetRequiredService<AppSettings>().Network.TimeoutInSeconds));

            services.AddMailNotification(_configuration.GetSection("Notify:Email"));
            services.AddSingleton<AppSettings>();
            services.AddHostedService<CheckService>();
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
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                //endpoints.MapGet("/test", async _ =>
                //{
                //    var services = _.RequestServices.GetServices<INotificationService>().ToList();
                //    //await NotifyJob.AddToQueueAsync(new Web { Name = "百度", Enabled = true, LastCheck = DateTimeOffset.Now, Url = "http://baidu.com", Result = new() { Speed = 30, State = "200", Succeeded = true } });
                //    //await NotifyJob.AddToQueueAsync(new Web { Name = "腾讯", Enabled = true, LastCheck = DateTimeOffset.Now, Url = "http://qq.com", Result = new() { Speed = 30, State = "200", Succeeded = true } });
                //    //await NotifyJob.AddToQueueAsync(new Web { Name = "Google", Enabled = true, LastCheck = DateTimeOffset.Now, Url = "http://google.com", Result = new() { Speed = 0, State = "Fault", Succeeded = false, Detail = "访问超时" } });
                //});
            });
        }
    }
}