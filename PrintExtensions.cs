using System;

namespace Automa.Benchmarks
{
    public static class PrintExtensions
    {
        public static void Print(this BenchmarkResult[] results)
        {
            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                Console.WriteLine($"{result.Name,-20} {result.Duration,16:c}");
            }
        }
    }
}