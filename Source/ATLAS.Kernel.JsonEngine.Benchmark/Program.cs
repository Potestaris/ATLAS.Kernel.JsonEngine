using BenchmarkDotNet.Running;

namespace ATLAS.Kernel.JsonEngine.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<JsonEngineBenchmarks>();
    }
}