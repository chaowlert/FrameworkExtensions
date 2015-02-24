using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.Caching
{
    public class CacheObject<T> where T : class
    {
        volatile T _item;
        public T Item
        {
            get { return _item; }
            set { _item = value; }
        }
        long _expiredTicks;
        public long ExpiredTicks
        {
            get { return Thread.VolatileRead(ref _expiredTicks); }
            set { Thread.VolatileWrite(ref _expiredTicks, value); }
        }
    }
    public static class MemoryCacheExtensions
    {
        public static T GetOrCreate<T>(this MemoryCache cache, string key, Func<T> func, int minExpire = 8) where T : class
        {
            var obj = (CacheObject<T>)cache.Get(key);
            var now = DateTime.UtcNow;
            if (obj == null)
            {
                obj = new CacheObject<T>
                {
                    Item = func(),
                    ExpiredTicks = now.AddMinutes(minExpire).Ticks,
                };
                cache.Set(key, obj, new DateTimeOffset(now.AddMinutes(minExpire * 2)));
            }
            else if (obj.ExpiredTicks < now.Ticks)
            {
                obj.ExpiredTicks = now.AddMinutes(minExpire).Ticks;
                Task.Run(() =>
                {
                    obj.Item = func();
                    cache.Set(key, obj, new DateTimeOffset(now.AddMinutes(minExpire * 2)));
                });
            }
            return obj.Item;
        }
    }
}