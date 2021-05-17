using System;

namespace AhDung
{
    public static class DateTimeUtil
    {
        public static string ToRelative(this DateTimeOffset time)
        {
            var delta = DateTimeOffset.Now - time;
            var result = "";
            if ((int)delta.TotalMinutes is { } mins && mins != 0)
            {
                result = $"{Math.Abs(mins)}m";
            }

            if (delta.Seconds is { } secs && secs != 0)
            {
                result += $"{Math.Abs(secs)}s";
            }

            if (result.Length == 0)
            {
                result = "Now";
            }
            else
            {
                result += delta.TotalSeconds > 0 ? " ago" : " later";
            }

            return result;
        }
    }
}