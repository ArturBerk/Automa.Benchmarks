using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Automa.Benchmarks
{
    public abstract class Benchmark : IBenchmark
    {
        private readonly BenchmarkCase[] cases;
        private readonly Dictionary<string, BenchmarkPrepare> prepares;

        public int IterationCount = 10;

        protected Benchmark()
        {
            var methods = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToArray();
            cases = SelectCases(methods).ToArray();
            prepares = new Dictionary<string, BenchmarkPrepare>();
            foreach (var benchmarkPrepare in SelectPrepares(methods))
            {
                prepares[benchmarkPrepare.Name] = benchmarkPrepare;
            }
        }

        protected virtual void Prepare() { }

        protected virtual void Free() { }

        private IEnumerable<BenchmarkCase> SelectCases(IEnumerable<MethodInfo> methods)
        {
            foreach (var methodInfo in methods)
            {
                if (methodInfo.GetParameters().Length > 0) continue;
                if (methodInfo.ReturnType != typeof(void)) continue;
                var caseAttribute = methodInfo.GetCustomAttribute<CaseAttribute>();
                if (caseAttribute == null) continue;
                yield return new BenchmarkCase(
                    caseAttribute.Name,
                    (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo));
            }
        }

        private IEnumerable<BenchmarkPrepare> SelectPrepares(IEnumerable<MethodInfo> methods)
        {
            foreach (var methodInfo in methods)
            {
                if (methodInfo.GetParameters().Length > 0) continue;
                if (methodInfo.ReturnType != typeof(void)) continue;
                var caseAttribute = methodInfo.GetCustomAttribute<CaseAttribute>();
                if (caseAttribute == null) continue;
                yield return new BenchmarkPrepare(
                    caseAttribute.Name,
                    (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo));
            }
        }

        public BenchmarkResult[] Execute()
        {
            Prepare();
            var iterationCount = IterationCount;
            Stopwatch stopwatch = new Stopwatch();
            BenchmarkResult[] results = new BenchmarkResult[cases.Length];
            for (var index = 0; index < cases.Length; index++)
            {
                var benchmarkCase = cases[index];
                if (prepares.TryGetValue(benchmarkCase.Name, out var prepare))
                {
                    prepare.Execute();
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var totalMemory = GC.GetTotalMemory(true);
                stopwatch.Restart();
                for (int i = 0; i < iterationCount; i++)
                {
                    benchmarkCase.Execute();
                }
                stopwatch.Stop();
                var totalMemoryAfterTest = GC.GetTotalMemory(false);
                results[index] = new BenchmarkResult(benchmarkCase.Name, stopwatch.Elapsed, totalMemoryAfterTest - totalMemory);
            }
            Free();
            return results;
        }

        public static void ExecuteAll()
        {
            Thread.Sleep(1000);
            foreach (var benchmarkType in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>!type.IsAbstract && typeof(IBenchmark).IsAssignableFrom(type)))
            {
                var benchmark = (IBenchmark) Activator.CreateInstance(benchmarkType);
                var results = benchmark.Execute();
                Console.WriteLine(benchmark);
                results.Print();
                Console.WriteLine();
            }
        }

        public static void Execute(params IBenchmark[] benchmarks)
        {
            Thread.Sleep(1000);
            foreach (var benchmark in benchmarks)
            {
                var results = benchmark.Execute();
                Console.WriteLine(benchmark);
                results.Print();
                Console.WriteLine();
            }
        }

        public static void Execute(params Type[] benchmarkTypes)
        {
            Thread.Sleep(1000);
            foreach (var benchmarkType in benchmarkTypes
                .Where(type => !type.IsAbstract && typeof(IBenchmark).IsAssignableFrom(type)))
            {
                var benchmark = (IBenchmark)Activator.CreateInstance(benchmarkType);
                var results = benchmark.Execute();
                Console.WriteLine(benchmark);
                results.Print();
                Console.WriteLine();
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name} ({IterationCount})";
        }

        private struct BenchmarkCase
        {
            public readonly string Name;
            public readonly Action Execute;

            public BenchmarkCase(string name, Action execute)
            {
                Name = name;
                Execute = execute;
            }
        }

        private struct BenchmarkPrepare
        {
            public readonly string Name;
            public readonly Action Execute;

            public BenchmarkPrepare(string name, Action execute)
            {
                Name = name;
                Execute = execute;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PrepareAttribute : Attribute
    {
        public readonly string Name;

        public PrepareAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CaseAttribute : Attribute
    {
        public readonly string Name;

        public CaseAttribute(string name)
        {
            Name = name;
        }
    }
}
