using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.AI;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;
using BenchmarkDotNet.Attributes;

namespace ATLAS.Kernel.JsonEngine.Benchmark;

public class JsonEngineBenchmarks
{
    private JsonNode _root = null!;
    private JsonGraph _graph = null!;
    private string _aiQuery = null!;
    private JsonSqlEngine _sqlEngine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _root = JsonCore.Parse("""
                               {
                                 "items": [
                                   { "id": "A", "precio": { "valor": 120 }, "dependencias": ["B","C"] },
                                   { "id": "B", "precio": { "valor": 5 },   "dependencias": [] },
                                   { "id": "C", "precio": { "valor": 40 },  "dependencias": ["B"] }
                                 ]
                               }
                               """)!;

        _graph = new JsonGraph();
        _graph.Build(_root, "$.items", "id", "dependencias");
        _sqlEngine = new JsonSqlEngine();

        _aiQuery = """
                   AI QUERY:
                     FIND items WHERE precio.valor > 20
                     THEN GRAPH EXPAND dependencias[*] UP TO 3 LEVELS
                     THEN RANK BY PAGERANK
                     RETURN id, precio.valor
                   """;
    }

    [Benchmark]
    public void Benchmark_JsonGet()
    {
        JsonCore.GetDouble(_root, "items[0].precio.valor");
    }

    [Benchmark]
    public void Benchmark_PageRank()
    {
        _graph.PageRank();
    }

    [Benchmark]
    public void Benchmark_JsonAI()
    {
        JsonAiEngine.Execute(_root, _graph, _aiQuery, _sqlEngine);
    }
}

