using AhDung.WebChecker.Models;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AhDung.WebChecker.Services.Jobs
{
    public class NotifyJob : IHostedService
    {
        static readonly Channel<Web> _messageQueue = Channel.CreateUnbounded<Web>();
        private readonly IEnumerable<INotificationService> _notifications;
        bool _stopped;

        public NotifyJob(IEnumerable<INotificationService> notifications)
        {
            _notifications = notifications;
        }

        public static async Task AddToQueueAsync(Web web)
        {
            await _messageQueue.Writer.WriteAsync(web);
            Log.Information("Added \"{name}\" to notification queue.", web.Name);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopped = false;

            Task.Run(async () =>
            {
                while (!_stopped)
                {
                    var web = await _messageQueue.Reader.ReadAsync();
                    Log.Information("Picked \"{name}\" from notification queue.", web.Name);
                    foreach (var n in _notifications)
                    {
                        _ = n.NotifyAsync(web);
                    }
                }
            }, CancellationToken.None);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stopped = true;
            return Task.CompletedTask;
        }
    }
}