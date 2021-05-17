using AhDung.WebChecker.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace AhDung.WebChecker
{
    public static class AppSettings
    {
        public static IConfiguration Configuration { get; set; }

        public static List<User> Users => Configuration.GetSection("Users").Get<List<User>>() ?? new();

        public static int IntervalSeconds => Configuration.GetValue("GlobalIntervalSeconds", 300);

        public static string TimeFormat => Configuration["TimeFormat"] ?? "yyyy-MM-dd HH:mm:ss";

        public static string OwnUrl => Configuration["OwnUrl"];

        public static class Network
        {
            public static int PooledConnectionLifetimeInSeconds => Configuration.GetValue("Network:PooledConnectionLifetimeInSeconds", 1);

            public static int TimeoutInSeconds => Configuration.GetValue("Network:TimeoutInSeconds", 10);
        }

        public static class Notify
        {
            public static class Email
            {
                public static bool Enabled => Configuration.GetValue("Notify:Email:Enabled", true);

                public static int RetryTimes => Configuration.GetValue("Notify:Email:RetryTimes", 10);

                public static int RetryIntervalInMinutes => Configuration.GetValue("Notify:Email:RetryIntervalInMinutes", 2);
            }
        }
    }
}