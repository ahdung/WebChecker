using System;

namespace AhDung.WebChecker.Models
{
    public class CheckResult : ICloneable
    {
        public bool Succeeded { get; set; }

        public long Speed { get; set; }

        public string State { get; set; }

        public string Detail { get; set; }

        public CheckResult Clone() => (CheckResult)((ICloneable)this).Clone();

        object ICloneable.Clone() => new CheckResult
        {
            Detail    = Detail,
            Speed     = Speed,
            State     = State,
            Succeeded = Succeeded,
        };
    }
}