using System;

namespace ImageViewerDemo
{
    public static class MathEx
    {
        private static Random rand = new System.Random();

        public static T Max<T>(T a, T b) where T : IComparable
        {
            return a.CompareTo(b) > 0 ? a : b;
        }

        public static T Min<T>(T a, T b) where T : IComparable
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        public static int Random(int min = int.MinValue, int max = int.MaxValue) => rand.Next(min, max);

        public static double QuadIn(double x) => x * x;
    }
}