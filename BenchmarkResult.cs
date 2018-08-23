using System;

namespace Automa.Benchmarks
{
    public struct BenchmarkResult
    {
        public readonly string Name;
        public readonly TimeSpan Duration;
        public readonly long MemoryAllocated;

        public BenchmarkResult(string name, TimeSpan duration, long memoryAllocated)
        {
            Name = name;
            Duration = duration;
            MemoryAllocated = memoryAllocated;
        }
    }
}