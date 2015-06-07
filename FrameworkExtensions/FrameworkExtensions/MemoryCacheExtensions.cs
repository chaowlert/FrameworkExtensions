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
        public static T GetOrCreate<T>(this MemoryCache cache, string key, Func<T> func, int maxExpire = 16) where T : class
        {
            var obj = (CacheObject<T>) cache.Get(key);
            if (obj == null)
            {
                obj = new CacheObject<T>
                {
                    Item = func()
                };
                var now = DateTime.UtcNow;
                obj.ExpiredTicks = now.AddMinutes(maxExpire >> 1).Ticks;
                cache.Set(key, obj, new DateTimeOffset(now.AddMinutes(maxExpire)));
            }
            else
            {
                var now = DateTime.UtcNow;
                if (obj.ExpiredTicks < now.Ticks)
                {
                    obj.ExpiredTicks = now.AddMinutes(maxExpire >> 1).Ticks;
                    Task.Run(() =>
                    {
                        obj.Item = func();
                        var now2 = DateTime.UtcNow;
                        obj.ExpiredTicks = now2.AddMinutes(maxExpire >> 1).Ticks;
                        cache.Set(key, obj, new DateTimeOffset(now2.AddMinutes(maxExpire)));
                    });
                }
            }
            return obj.Item;
        }
    }
}