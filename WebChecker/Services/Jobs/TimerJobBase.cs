using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhDung
{
    public abstract class TimerJobBase : IHostedService, IDisposable
    {
        private readonly TimeSpan _interval;
        private readonly TimeSpan _delay;
        private readonly bool _allowReentrant;
        private readonly bool _autoLog;
        readonly Timer _timer;
        int _runningFlag;

        protected ILogger Logger { get; }
        public string Name { get; }

        protected TimerJobBase(TimeSpan interval, object state=null, TimeSpan delay = default, bool allowReentrant = false, ILogger logger = null, bool autoLog = true, string name = null)
        {
            _interval       = interval;
            _delay          = delay;
            _allowReentrant = allowReentrant;
            Logger          = logger;
            _autoLog        = autoLog;
            Name            = name;
            _timer          = new(RunWorker, state, Timeout.Infinite, Timeout.Infinite);
        }

        protected abstract void Worker(object state);

        void RunWorker(object state)
        {
            if (!_allowReentrant && Interlocked.CompareExchange(ref _runningFlag, 1, 0) == 1)
            {
                return;
            }

            try
            {
                TryLog($"{Name} running...");
                Worker(state);
            }
            finally
            {
                TryLog($"{Name} finished.");
                if (!_allowReentrant)
                {
                    Interlocked.Exchange(ref _runningFlag, 0);
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(_delay, _interval);
            TryLog($"{Name} started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            TryLog($"{Name} stopped.");
            return Task.CompletedTask;
        }

        void TryLog(string message)
        {
            if (!string.IsNullOrEmpty(Name) && _autoLog)
            {
                Logger?.LogInformation(message);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}