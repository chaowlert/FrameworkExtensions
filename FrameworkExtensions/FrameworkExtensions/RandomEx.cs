using System;

namespace FrameworkExtensions
{
    public static class RandomEx
    {
        private static readonly Random global = new Random();
        [ThreadStatic]
        private static Random local;

        public static int Next(int maxValue)
        {
            var inst = local;
            if (inst == null)
            {
                int seed;
                lock (global)
                {
                    seed = global.Next();
                }
                local = inst = new Random(seed);
            }
            return inst.Next(maxValue);
        }
    }
}