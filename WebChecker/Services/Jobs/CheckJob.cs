using AhDung.WebChecker.Models;
using AhDung.WebChecker.Services.Jobs;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;

namespace AhDung.WebChecker.Jobs
{
    public class CheckJob : TimerJobBase
    {
        private readonly Web _web;
        private readonly HttpClient _client;

        public CheckJob(Web web, HttpClient client, ILogger logger)
            : base(TimeSpan.FromSeconds(web.IntervalSeconds ?? AppSettings.IntervalSeconds), autoLog: false, logger: logger, name: $"Check \"{web.Name}\" Job")
        {
            _web    = web;
            _client = client;
        }

        protected override void Worker(object state)
        {
            var sw = new Stopwatch();
            var startTime = DateTimeOffset.Now;
            CheckResult result = new();

            try
            {
                _web.InChecking = true;
                sw.Start();
                using var response = _client.Send(new HttpRequestMessage(HttpMethod.Get, _web.Url));
                sw.Stop();
                result.State = ((int)response.StatusCode).ToString();
                if (response.IsSuccessStatusCode)
                {
                    result.Succeeded = true;
                    result.Speed     = sw.ElapsedMilliseconds;
                }
                else
                {
                    result.Detail = response.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                result.State  = "Fault";
                result.Detail = ex.Message;
            }
            finally
            {
                sw.Stop();
            }

            if (result.Succeeded)
            {
                Logger.LogInformation("Check \"{Name}\" {State}. {Speed}ms.", _web.Name, "OK", result.Speed);
            }
            else
            {
                Logger.LogWarning("Check \"{Name}\" {State}. {Detail:l}", _web.Name, "Fault", result.Detail);
            }
            //Logger.Log(result.Succeeded ? LogLevel.Information : LogLevel.Warning,
            //    $"Check {{name}} {{state}} {(result.Succeeded ? result.Speed + "ms" : result.Detail)}.",
            //    _web.Name,
            //    result.Succeeded ? "OK." : "Fault!");

            if ((_web.Result?.Succeeded ?? true) != result.Succeeded)
            {
                var web = _web.Clone();
                web.LastCheck = startTime;
                web.Result    = result.Clone();
                _             = NotifyJob.AddToQueueAsync(web);
            }

            _web.Result     = result;
            _web.LastCheck  = startTime;
            _web.InChecking = false;
        }
    }
}