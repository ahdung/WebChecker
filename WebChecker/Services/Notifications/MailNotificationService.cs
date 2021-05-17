using AhDung.WebChecker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AhDung.WebChecker.Services
{
    public class MailNotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailNotificationService> _logger;

        public MailNotificationService(IConfiguration configuration, ILogger<MailNotificationService> logger)
        {
            _configuration = configuration;
            _logger        = logger;
        }

        public async Task NotifyAsync(Web web)
        {
            if (!AppSettings.Notify.Email.Enabled
                || _configuration.GetSection("Notify:Email:Sender")?.Get<MailSender>() is not { } sender
                || _configuration.GetSection("Notify:Email:Receivers")?.Get<List<string>>() is not { Count: > 0 } receivers)
            {
                return;
            }

            var retryTimes = AppSettings.Notify.Email.RetryTimes;
            var retryIntervalInMinutes = AppSettings.Notify.Email.RetryIntervalInMinutes;
            if (retryIntervalInMinutes < 0)
                retryIntervalInMinutes = 0;

            do
            {
                try
                {
                    using var msg = new MailMessage
                    {
                        From       = new(sender.Address),
                        Subject    = $"{web.Name}访问{(web.Result.Succeeded ? "恢复" : "故障")}",
                        IsBodyHtml = true,
                        Body = $@"Url: <a href=""{web.Url}"" target=""_blank"">{web.Url}</a><br/>
State: {(web.Result.Succeeded
    ? @"<span style=""color:#4caf50"">正常</span>"
    : @"<span style=""color:#ff5722"">故障</span>")}<br/>
{(web.Result.Succeeded
    ? $"Speed: {web.Result.Speed}ms"
    : $"Error: {WebUtility.HtmlEncode(web.Result.Detail)}")}<br/>
LastCheck: {WebUtility.HtmlEncode(web.LastCheck?.ToString(AppSettings.TimeFormat))}<br/>
{(AppSettings.OwnUrl is { Length: >0 } ownUlr
    ? $"<br/>To visit WebChecker: <a href=\"{ownUlr}\" target=\"_blank\">{ownUlr}</a>"
    : "")}",
                    };

                    receivers.ForEach(msg.To.Add);

                    using var client = new SmtpClient(sender.SmtpServer, sender.Port)
                    {
                        EnableSsl   = sender.UseSsl,
                        Credentials = new NetworkCredential(sender.User, sender.Password),
                    };

                    await client.SendMailAsync(msg).ConfigureAwait(false);
                    _logger.LogInformation("\"{name}\" mail sent.", web.Name);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mail send error: {msg}", ex.Message);
                    _logger.LogWarning("Mail sending retry times: {times}", retryTimes);
                    if (retryTimes <= 0)
                    {
                        break;
                    }

                    retryTimes--;
                    await Task.Delay(TimeSpan.FromMinutes(retryIntervalInMinutes)).ConfigureAwait(false);
                }
            } while (true);
        }
    }
}