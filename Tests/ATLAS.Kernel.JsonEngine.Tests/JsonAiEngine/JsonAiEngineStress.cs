using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

namespace ATLAS.Kernel.JsonEngine.Tests.JsonAiEngine;

public class JsonAiEngineStress
{
    [Fact]
    public void JsonAI_StressTest_10000Nodes_50000Edges()
    {
        const int nodeCount = 10000;
        const int edgeCount = 50000;

        // -----------------------------
        // Generar JSON masivo
        // -----------------------------
        var items = new JsonArray();

        for (int i = 0; i < nodeCount; i++)
        {
            items.Add(new JsonObject
            {
                ["id"] = $"N{i}",
                ["valor"] = i,
                ["dependencias"] = new JsonArray()
            });
        }

        // Generar 50.000 aristas aleatorias
        var rnd = new Random(42);
        for (int i = 0; i < edgeCount; i++)
        {
            int from = rnd.Next(nodeCount);
            int to = rnd.Next(nodeCount);

            if (from != to)
            {
                ((JsonArray)items[from]!["dependencias"]!).Add($"N{to}");
            }
        }

        var root = new JsonObject { ["items"] = items };

        // -----------------------------
        // Construir grafo real
        // -----------------------------
        var graph = new JsonGraph();
        graph.Build(root, "$.items", "id", "dependencias");

        // -----------------------------
        // Motor SQL real
        // -----------------------------
        var sql = new JsonSqlEngine();

        // -----------------------------
        // AI QUERY real
        // -----------------------------
        const string aiQuery = """
                               AI QUERY:
                                 FIND items WHERE valor > 5000
                                 THEN GRAPH EXPAND dependencias[*] UP TO 2 LEVELS
                                 THEN RANK BY PAGERANK
                                 RETURN id, valor
                               """;

        // -----------------------------
        // Ejecutar pipeline completo
        // -----------------------------
        var sw = System.Diagnostics.Stopwatch.StartNew();

        List<Dictionary<string, JsonNode?>> result = AI.JsonAiEngine.Execute(root, graph, aiQuery, sql);

        sw.Stop();

        // -----------------------------
        // Validaciones
        // -----------------------------
        Assert.NotEmpty(result);
        Assert.True(sw.ElapsedMilliseconds < 2000, $"El test tardó demasiado: {sw.ElapsedMilliseconds} ms");
    }
}
