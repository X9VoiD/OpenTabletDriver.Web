using BenchmarkDotNet.Running;

namespace OpenTabletDriver.Web.Benchmarks;

public static class EntryPoint
{
    public static void Main(string[] _)
    {
        BenchmarkRunner.Run(typeof(EntryPoint).Assembly);
    }
}
