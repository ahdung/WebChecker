using AhDung.WebChecker.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AhDung.WebChecker.Services.Jobs
{
    public class CheckService : BackgroundService
    {
        readonly AppSettings _settings;
        readonly IEnumerable<INotificationService> _notifications;
        readonly ILogger<CheckService> _logger;
        readonly Dictionary<Guid, Timer> _timers = new();
        readonly HttpClient _client;

        public CheckService(AppSettings settings,
            IHttpClientFactory httpClientFactory,
            IEnumerable<INotificationService> notifications,
            ILogger<CheckService> logger)
        {
            _settings      = settings;
            _client        = httpClientFactory.CreateClient("default");
            _notifications = notifications;
            _logger        = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ReStartChecking();
            _settings.Changed += (_, _) =>
            {
                _logger.LogWarning("settings changed.");
                ReStartChecking();
            };
            return Task.CompletedTask;
        }

        int _runningFlag;

        void ReStartChecking()
        {
            if (Interlocked.CompareExchange(ref _runningFlag, 1, 0) == 1)
                return;

            try
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Dispose();
                }

                _timers.Clear();

                var webs = _settings.Webs;
                foreach (var web in webs)
                {
                    var period = web.IntervalSeconds is { } interval
                        ? TimeSpan.FromSeconds(interval)
                        : _settings.Interval;

                    var id = Guid.NewGuid();
                    var timer = new Timer(TimerRun, new object[] { id, web }, TimeSpan.Zero, period);

                    _timers.Add(id, timer);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _runningFlag, 0);
            }
        }

        void TimerRun(object state)
        {
            var id = (Guid)(state as object[])[0];
            var web = (Web)(state as object[])[1];

            if (!_timers.ContainsKey(id)
                || !Monitor.TryEnter(web)) //不能让上锁在前
                return;

            try
            {
                var startTime = DateTimeOffset.Now;
                CheckResult result = new();
                try
                {
                    web.InChecking = true;
                    //_logger.LogInformation("Checking {name}...", web.Name);

                    var start = Stopwatch.GetTimestamp();
                    using var response = _client.Send(new HttpRequestMessage(HttpMethod.Get, web.Url));
                    var end = Stopwatch.GetTimestamp();

                    result.State = ((int)response.StatusCode).ToString();
                    if (response.IsSuccessStatusCode)
                    {
                        result.Succeeded = true;
                        result.Speed     = (long)GetElapsedMilliseconds(start, end);
                    }
                    else
                    {
                        result.Detail = response.StatusCode.ToString();
                    }

                    result.ServerTime = response.Headers.Date?.LocalDateTime;
                }
                catch (Exception ex)
                {
                    result.State  = "Fault";
                    result.Detail = ex.Message;
                }

                if (result.Succeeded)
                    _logger.LogInformation("Check {Name} {State}. {Speed}ms.", web.Name, "OK", result.Speed);
                else
                    _logger.LogWarning("Check {Name} {State}. {Detail:l}", web.Name, "Fault", result.Detail);

                if ((web.Result?.Succeeded ?? true) != result.Succeeded)
                {
                    var newInterval = result.Succeeded
                        ? web.IntervalSeconds is { } i ? TimeSpan.FromSeconds(i) : _settings.Interval
                        : web.FaultIntervalSeconds is { } fi
                            ? TimeSpan.FromSeconds(fi)
                            : _settings.FaultInterval;

                    _logger.LogInformation("Change timer {web} interval to {new}s", web.Name, newInterval.TotalSeconds);
                    _timers[id].Change(newInterval, newInterval);

                    var copy = web.Clone();
                    copy.LastCheck = startTime;
                    copy.Result    = result.Clone();

                    foreach (var n in _notifications)
                    {
                        n.NotifyAsync(copy);
                    }
                }

                web.Result     = result;
                web.LastCheck  = startTime;
                web.InChecking = false;
            }
            finally
            {
                Monitor.Exit(web);
            }
        }

        private static double GetElapsedMilliseconds(long start, long stop) =>
            (double)((stop - start) * 1000) / Stopwatch.Frequency;
    }
}