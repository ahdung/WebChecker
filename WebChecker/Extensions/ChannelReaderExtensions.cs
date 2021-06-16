using System.Collections.Generic;
using System.Threading.Channels;

namespace AhDung.Extensions
{
    public static class ChannelReaderExtensions
    {
        /// <summary>
        /// 以不等待的方式读取当前管道中的所有项
        /// </summary>
        public static IEnumerable<T> ReadAll<T>(this ChannelReader<T> reader)
        {
            while (reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }
}