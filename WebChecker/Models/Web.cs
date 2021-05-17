using System;

namespace AhDung.WebChecker.Models
{
    public class Web : ICloneable
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public int? IntervalSeconds { get; set; }

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 是否正在检测
        /// </summary>
        public bool InChecking { get; set; }

        public DateTimeOffset? LastCheck { get; set; }

        public string FriendlyLastCheck => LastCheck?.ToRelative();

        public CheckResult Result { get; set; }

        ///// <summary>
        ///// 响应码。异常时显示Fault
        ///// </summary>
        //public string State { get; set; }

        ///// <summary>
        ///// 访问速度。毫秒
        ///// </summary>
        //public long? Speed { get; set; }

        //public bool? IsOk { get; set; }

        //public string ErrorMessage { get; set; }

        public Web Clone() => (Web)((ICloneable)this).Clone();

        object ICloneable.Clone() => new Web
        {
            Name            = Name,
            Url             = Url,
            IntervalSeconds = IntervalSeconds,
            Enabled         = Enabled,
            InChecking      = InChecking,
            LastCheck       = LastCheck,
            Result          = Result?.Clone(),
        };
    }
}