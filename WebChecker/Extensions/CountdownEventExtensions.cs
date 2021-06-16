using System.Threading;

namespace AhDung.Extensions
{
    public static class CountdownEventExtensions
    {
        public static void ForceAddCount(this CountdownEvent cde, int signalCount = 1)
        {
            lock (cde)
            {
                if (!cde.TryAddCount(signalCount))
                {
                    cde.Reset(signalCount);
                }
            }
        }
    }
}