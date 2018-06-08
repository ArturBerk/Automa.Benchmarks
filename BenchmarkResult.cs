using System;

namespace Automa.Benchmarks
{
    public struct BenchmarkResult
    {
        public readonly string Name;
        public readonly TimeSpan Duration;

        public BenchmarkResult(string name, TimeSpan duration)
        {
            Name = name;
            Duration = duration;
        }
    }
}