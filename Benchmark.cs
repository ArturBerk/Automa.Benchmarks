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

        public int IterationCount = 10;

        protected Benchmark()
        {
            cases = SelectCases(GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)).ToArray();
        }

        protected virtual void Prepare() { }

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

        public BenchmarkResult[] Execute()
        {
            Prepare();
            var iterationCount = IterationCount;
            Stopwatch stopwatch = new Stopwatch();
            BenchmarkResult[] results = new BenchmarkResult[cases.Length];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (var index = 0; index < cases.Length; index++)
            {
                stopwatch.Restart();
                var benchmarkCase = cases[index];
                for (int i = 0; i < iterationCount; i++)
                {
                    benchmarkCase.Execute();
                }
                stopwatch.Stop();
                results[index] = new BenchmarkResult(benchmarkCase.Name, stopwatch.Elapsed);
            }
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
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PrepareAttribute : Attribute
    {

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
