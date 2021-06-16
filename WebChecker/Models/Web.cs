using System;

namespace AhDung.WebChecker.Models
{
    public class Web : ICloneable
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public int? IntervalSeconds { get; set; }

        public int? FaultIntervalSeconds { get; set; }

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 是否正在检测
        /// </summary>
        public bool InChecking { get; set; }

        public DateTimeOffset? LastCheck { get; set; }

        public string FriendlyLastCheck => LastCheck?.ToRelative();

        public CheckResult Result { get; set; }

        public Web Clone() => (Web)((ICloneable)this).Clone();

        object ICloneable.Clone() => new Web
        {
            Name                 = Name,
            Url                  = Url,
            IntervalSeconds      = IntervalSeconds,
            FaultIntervalSeconds = FaultIntervalSeconds,
            Enabled              = Enabled,
            InChecking           = InChecking,
            LastCheck            = LastCheck,
            Result               = Result?.Clone(),
        };
    }
}