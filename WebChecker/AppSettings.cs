using AhDung.WebChecker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AhDung.WebChecker
{
    public class AppSettings
    {
        readonly IConfiguration _configuration;
        readonly byte[] _settingsFileHash = new byte[16];
        readonly byte[] _envSettingsFileHash = new byte[16];
        int _count;

        public event EventHandler Changed;

        public AppSettings(IConfiguration configuration, IHostEnvironment env, ILogger<AppSettings> logger)
        {
            _configuration = configuration;

            ChangeToken.OnChange(configuration.GetReloadToken, async () =>
            {
                Interlocked.Increment(ref _count);
                await Task.Delay(500);
                if (Interlocked.Decrement(ref _count) > 0)
                    return;

                var result = await Task.WhenAll(
                    HasChangedAsync(Path.Combine(env.ContentRootPath, "appsettings.json"), _settingsFileHash),
                    HasChangedAsync(Path.Combine(env.ContentRootPath, $"appsettings.{env.EnvironmentName}.json"), _envSettingsFileHash)
                ).ConfigureAwait(false);

                if (result.Any(x => x))
                {
                    logger.LogInformation("fire Changed event...");
                    OnChanged(EventArgs.Empty);
                }
            });
        }

        void OnChanged(EventArgs e) => Changed?.Invoke(this, e);

        static async Task<bool> HasChangedAsync(string file, byte[] lastHash)
        {
            if (!File.Exists(file))
            {
                return false;
            }

            var retry = 3;

            do
            {
                try
                {
                    var hash = await ComputeHash(file).ConfigureAwait(false);
                    if (!hash.SequenceEqual(lastHash))
                    {
                        hash.CopyTo(lastHash, 0);
                        return true;
                    }

                    return false;
                }
                catch (IOException ex) when (ex.HResult == -2147024864 && retry > 0)
                {
                    retry--;
                    Thread.Sleep(1000);
                }
            } while (true);

            static async Task<byte[]> ComputeHash(string file)
            {
                using var md5 = MD5.Create();
                await using var fs = File.OpenRead(file);
                return await md5.ComputeHashAsync(fs).ConfigureAwait(false);
            }
        }

        public string OwnUrl => _configuration["OwnUrl"];

        public TimeSpan Interval => TimeSpan.FromSeconds(_configuration.GetValue("GlobalIntervalSeconds", 300));

        public TimeSpan FaultInterval => TimeSpan.FromSeconds(_configuration.GetValue("GlobalFaultIntervalSeconds", 60));

        public string TimeFormat => _configuration["TimeFormat"] ?? "yyyy-MM-dd HH:mm:ss";

        public List<User> Users => _configuration.GetSection("Users").Get<List<User>>() ?? new();

        List<Web> _webs;

        /// <summary>
        /// 已筛选为已启用的
        /// </summary>
        public List<Web> Webs
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                var webs = _configuration.GetSection("Webs").Get<List<Web>>()
                                        ?.Where(x => x.Enabled)
                                         .ToList() ?? new();

                for (var i = 0; i < webs.Count; i++)
                {
                    var item = webs[i];
                    var key = item.Name;
                    if (_webs?.Find(x => x.Name == key) is { } oldItem)
                    {
                        oldItem.Enabled              = item.Enabled;
                        oldItem.FaultIntervalSeconds = item.FaultIntervalSeconds;
                        oldItem.IntervalSeconds      = item.IntervalSeconds;
                        oldItem.Name                 = item.Name;
                        oldItem.Url                  = item.Url;
                        webs[i]                      = oldItem;
                    }
                }

                return _webs = webs;
            }
        }

        public NetworkOptions Network => _configuration.GetSection("Network").Get<NetworkOptions>() ?? new();

        public class NetworkOptions
        {
            public int PooledConnectionLifetimeInSeconds { get; set; } = 1;

            public int TimeoutInSeconds { get; set; } = 10;
        }
    }
}