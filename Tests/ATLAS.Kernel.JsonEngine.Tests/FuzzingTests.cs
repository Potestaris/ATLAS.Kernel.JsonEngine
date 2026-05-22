using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ATLAS.Kernel.JsonEngine.Tests
{
    public class FuzzingTests
    {
        private static readonly string[] _jsonSeeds =
        [
            "{ \"item\": { \"id\": \"A\", \"price\": { \"value\": 120 } } }",
            "[1,2,3]",
            "null",
            "{ malformed",
            "{ \"items\": [{ \"id\": \"X\" }, { \"id\": \"Y\" }] }",
            "{ \"deep\": " + "\"" + string.Concat(Enumerable.Repeat("x", 2000)) + "\" }"
        ];

        private static readonly string[] _pathSeeds =
        [
            "",
            "$.item.id",
            "items[0].id",
            "items[*].id",
            "deep",
            "nonexistent.path"
        ];

        private static readonly string[] _exprSeeds =
        [
            "price.value > 50",
            "item.id = A",
            "name = Motor",
            "status <> Closed",
            "price.value >= 100 AND item.id = A",
            "bad expr >>>"
        ];

        private static string MutateRandom(string seed, Random rnd)
        {
            if (string.IsNullOrEmpty(seed)) seed = "{}";
            int op = rnd.Next(0, 5);
            return op switch
            {
                0 => seed + " ",
                1 => seed.Replace("\"", "\\\"") + "", // escape quotes badly
                2 => seed + "\n//comment",
                3 => seed.Insert(Math.Min(seed.Length, 3), "\"mut\":\"v\","),
                _ => seed
            };
        }

        [Fact]
        public void CombinatorialFuzzing_AllMutations_NoUnhandledExceptions()
        {
            var rnd = new Random(12345);
            foreach (string j in _jsonSeeds)
            foreach (string p in _pathSeeds)
            foreach (string e in _exprSeeds)
            {
                string mj = MutateRandom(j, rnd);
                string mp = MutateRandom(p, rnd);
                string me = MutateRandom(e, rnd);

                JsonNode? root;
                try
                {
                    root = JsonCore.Parse(mj);
                }
                catch (Exception ex) when (ex is JsonException || ex.GetType().Name.Contains("JsonReaderException"))
                {
                    // invalid JSON is expected for fuzzing inputs; skip this input
                    continue;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception while parsing json='{mj}' path='{mp}' expr='{me}': {ex}");
                    return; // unreachable but keeps compiler happy
                }

                // JsonPath and Get should not crash for valid parsed inputs
                try
                {
                    JsonCore.JsonPath(root, mp);

                    if (root is JsonObject && !string.IsNullOrWhiteSpace(me))
                    {
                        // EvalCondition may return false but should not throw on well-formed inputs
                        try
                        {
                            if (root is JsonObject obj)
                                _ = JsonCore.EvalCondition(obj, me);
                        }
                        catch (Exception ex)
                        {
                            Assert.Fail($"EvalCondition threw for json='{mj}' path='{mp}' expr='{me}': {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception for json='{mj}' path='{mp}' expr='{me}': {ex}");
                }
            }
        }

        [Fact]
        public void ParallelFuzzing_Multithread_NoRaceConditionsOrCrashes()
        {
            var exceptions = new ConcurrentBag<Exception>();
            ParallelOptions opts = new() { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 };
            Parallel.For(0, 1000, opts, i =>
            {
                try
                {
                    var rnd = new Random(i ^ Environment.TickCount);
                    string j = MutateRandom(_jsonSeeds[rnd.Next(_jsonSeeds.Length)], rnd);
                    string p = MutateRandom(_pathSeeds[rnd.Next(_pathSeeds.Length)], rnd);
                    string e = MutateRandom(_exprSeeds[rnd.Next(_exprSeeds.Length)], rnd);

                    JsonNode? root = JsonCore.Parse(j);
                    _ = JsonCore.JsonPath(root, p);
                    if (root is JsonObject obj && !string.IsNullOrWhiteSpace(e))
                        _ = JsonCore.EvalCondition(obj, e);
                }
                catch (Exception ex)
                {
                    if (ex is JsonException || ex.GetType().Name.Contains("JsonReaderException"))
                        return; // ignore parse errors
                    exceptions.Add(ex);
                }
            });

            if (exceptions.IsEmpty)
            {
                return;
            }

            Exception first = exceptions.First();
            Assert.Fail("Exceptions occurred during parallel fuzzing: " + first);
        }

        [Fact]
        public void MemoryLeakFuzzing_DetectsSignificantGrowth()
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long before = GC.GetTotalMemory(true);

            for (int iter = 0; iter < 50; iter++)
            {
                // Create many reasonably large JSON documents and parse them
                for (int k = 0; k < 200; k++)
                {
                    string big = "{ \"items\": [" + string.Join(",", Enumerable.Range(0, 50).Select(_ => "{\"v\":\"" + Guid.NewGuid() + "\"}")) + "] }";
                    JsonNode? root = JsonCore.Parse(big);
                    // Do some cheap access
                    _ = JsonCore.JsonPath(root, "$.items[0]");
                }

                // Encourage collection between batches
                GC.Collect(); GC.WaitForPendingFinalizers();
            }

            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long after = GC.GetTotalMemory(true);

            long delta = after - before;
            // Allow some buffer for allocations; fail if growth > 20MB
            const long threshold = 20 * 1024 * 1024;
            Assert.True(delta < threshold, $"Memory grew by {delta} bytes which exceeds threshold {threshold}");
        }

        [Fact]
        public void PerformanceFuzzing_DetectsDegradation()
        {
            // Warmup
            for (int w = 0; w < 50; w++)
            {
                JsonNode? r = JsonCore.Parse("{ \"a\": { \"b\": 1 } }");
                _ = JsonCore.JsonPath(r, "$.a.b");
            }

            const int n = 200;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
            {
                string json = "{ \"items\": [" + string.Join(",", Enumerable.Range(0, 20).Select(_ => "{\"v\":\"" + Guid.NewGuid() + "\"}")) + "] }";
                JsonNode? root = JsonCore.Parse(json);
                _ = JsonCore.JsonPath(root, "$.items[5]");
            }
            sw.Stop();
            double avgMs = sw.Elapsed.TotalMilliseconds / n;
            // If average per operation grows too large, consider it a regression. 50ms is conservative.
            const double thresholdMs = 50.0;
            Assert.True(avgMs < thresholdMs, $"Average operation time {avgMs:0.00}ms exceeds threshold {thresholdMs}ms");
        }
    }
}
