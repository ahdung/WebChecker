using System;

namespace AhDung.WebChecker.Models
{
    public class CheckResult : ICloneable
    {
        public bool Succeeded { get; set; }

        /// <summary>
        /// 访问速度（毫秒）
        /// </summary>
        public long Speed { get; set; }

        /// <summary>
        /// 有响应码显示响应码，否则显示Fault
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// 有响应码显示响应码状态，否则显示异常消息
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// 服务器时间
        /// </summary>
        public DateTime? ServerTime { get; set; }

        public CheckResult Clone() => (CheckResult)((ICloneable)this).Clone();

        object ICloneable.Clone() => new CheckResult
        {
            Detail     = Detail,
            Speed      = Speed,
            State      = State,
            Succeeded  = Succeeded,
            ServerTime = ServerTime,
        };
    }
}