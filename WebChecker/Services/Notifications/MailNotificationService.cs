using AhDung.AspNet.Razor;
using AhDung.Extensions;
using AhDung.WebChecker.Models;
using AhDung.WebChecker.Notifications.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AhDung.WebChecker.Services
{
    public class MailNotificationService : INotificationService
    {
        readonly IOptionsMonitor<MailNotificationOptions> _optionsMonitor;
        readonly ILogger<MailNotificationService>         _logger;
        readonly IServiceScopeFactory                     _scopeFactory;
        readonly AppSettings                              _settings;
        readonly Channel<Web>                             _messageQueue = Channel.CreateUnbounded<Web>(new() { SingleReader = true });
        readonly CountdownEvent                           _cde          = new(0);

        public MailNotificationService(
            IOptionsMonitor<MailNotificationOptions> optionsMonitor,
            AppSettings                              settings,
            IServiceScopeFactory                     scopeFactory,
            ILogger<MailNotificationService>         logger
        )
        {
            _optionsMonitor = optionsMonitor;
            _settings       = settings;
            _scopeFactory   = scopeFactory;
            _logger         = logger;
            _               = StartProcessQueueAsync();
        }

        async Task StartProcessQueueAsync()
        {
            while (await _messageQueue.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                _cde.Wait();
                var webs = _messageQueue.Reader.ReadAll().ToList();

                _logger.LogInformation("Picked {count} webs from notification queue: {webs}.", webs.Count, string.Join(", ", webs.Select(x => x.Name)));

                var info = await MakeMailInfoAsync(webs).ConfigureAwait(false);
                _logger.LogInformation("Sending mail...");
                await SendMailAsync(info.HtmlBody, info.Title).ConfigureAwait(false);
            }
        }

        async Task<(string Title, string HtmlBody)> MakeMailInfoAsync(IEnumerable<Web> webs)
        {
            var webList = webs.ToList();
            var model = new MailNotificationTemplateModel
            {
                Webs       = webList,
                TimeFormat = _settings.TimeFormat,
                ToolUrl    = _settings.OwnUrl,
            };

            string html;

            using (var scope = _scopeFactory.CreateScope())
            {
                var render = scope.ServiceProvider.GetRequiredService<IViewRenderService>();
                html = await render.RenderToStringAsync("~/Pages/MailNotificationTemplate.cshtml", model);
            }

            string title;
            if (webList.Count == 1)
            {
                var web = webList[0];
                title = $"{web.Name}访问{(web.Result.Succeeded ? "恢复" : "故障")}";
            }
            else
            {
                title = string.Join("", webList.GroupBy(x => x.Result.Succeeded)
                                               .OrderBy(x => x.Key) //故障在前
                                               .Select(x => $"{x.Count()}网站{(x.Key ? "恢复" : "故障")}"));
            }

            return (title, html);
        }

        async Task SendMailAsync(string htmlBody, string title = null)
        {
            if (_optionsMonitor.CurrentValue is not { Sender: { } sender, Receivers: { Count: > 0 } receivers } options)
            {
                return;
            }

            var retryTimes             = options.RetryTimes;
            var retryIntervalInMinutes = options.RetryIntervalMinutes;
            if (retryIntervalInMinutes < 0)
                retryIntervalInMinutes = 0;

            do
            {
                try
                {
                    using var msg = new MailMessage
                    {
                        From         = new(sender.Address),
                        Subject      = title ?? "WebChecker通知",
                        IsBodyHtml   = true,
                        Body         = htmlBody,
                        BodyEncoding = Encoding.UTF8,
                    };

                    receivers.ForEach(msg.To.Add);

                    using var client = new SmtpClient(sender.SmtpServer, sender.Port)
                    {
                        EnableSsl   = sender.UseSsl,
                        Credentials = new NetworkCredential(sender.User, sender.Password),
                    };

                    await client.SendMailAsync(msg).ConfigureAwait(false);
                    _logger.LogInformation("Mail sent.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mail send error: {msg}", ex.Message);
                    _logger.LogWarning("Mail sending retry times: {times}", retryTimes);
                    if (retryTimes <= 0)
                    {
                        _logger.LogWarning("Retry times reached 0, won't retry.");
                        break;
                    }

                    retryTimes--;
                    await Task.Delay(TimeSpan.FromMinutes(retryIntervalInMinutes)).ConfigureAwait(false);
                }
            } while (true);
        }

        public async Task NotifyAsync(Web web)
        {
            if (_optionsMonitor.CurrentValue is not { Enabled: true } options)
            {
                _logger.LogTrace("{way} on disabled, won't send.", "Mail notification");
                return;
            }

            _cde.ForceAddCount();

            try
            {
                _logger.LogInformation("Adding \"{name}\" to notification queue.", web.Name);
                await _messageQueue.Writer.WriteAsync(web).ConfigureAwait(false);
                _logger.LogTrace("Added \"{name}\" to notification queue.", web.Name);
            }
            finally
            {
                _ = Task.Delay(TimeSpan.FromSeconds(options.SendingDelaySeconds))
                        .ContinueWith(_ =>
                         {
                             _logger.LogTrace("CountdownEvent signal...");
                             _cde.Signal();
                             _logger.LogTrace("CountdownEvent signaled.");
                         });
            }
        }
    }

    public class MailNotificationOptions
    {
        public bool Enabled { get; set; } = true;

        public SenderOptions Sender { get; set; }

        public List<string> Receivers { get; set; }

        public int RetryTimes { get; set; } = 10;

        public int RetryIntervalMinutes { get; set; } = 2;

        public int SendingDelaySeconds { get; set; } = 8;

        public class SenderOptions
        {
            public string SmtpServer { get; set; }

            public int Port { get; set; }

            public bool UseSsl { get; set; }

            public string Address { get; set; }

            public string User { get; set; }

            public string Password { get; set; }
        }
    }

    public static class MailNotificationServiceExtensions
    {
        public static IServiceCollection AddMailNotification(this IServiceCollection services, Action<MailNotificationOptions> configureOptions) =>
            services.TryAddViewRender()
                    .Configure(configureOptions)
                    .AddSingleton<INotificationService, MailNotificationService>();


        public static IServiceCollection AddMailNotification(this IServiceCollection services, IConfiguration section) =>
            services.TryAddViewRender()
                    .Configure<MailNotificationOptions>(section)
                    .AddSingleton<INotificationService, MailNotificationService>();
    }
}