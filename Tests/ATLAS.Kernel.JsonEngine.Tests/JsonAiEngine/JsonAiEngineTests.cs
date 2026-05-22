using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.AI;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

namespace ATLAS.Kernel.JsonEngine.Tests.JsonAiEngine;

public class JsonAiEngineTests
{
    private readonly JsonNode _root;
    private readonly JsonGraph _graph;

    public JsonAiEngineTests()
    {
        const string json = """
                            {
                              "items": [
                                { "id": "A", "nombre": "Motor",   "precio": { "valor": 120 }, "dependencias": ["B", "C"] },
                                { "id": "B", "nombre": "Tornillo","precio": { "valor": 5   }, "dependencias": [] },
                                { "id": "C", "nombre": "Eje",     "precio": { "valor": 40  }, "dependencias": ["B"] },
                                { "id": "D", "nombre": "Filtro",  "precio": { "valor": 80  }, "dependencias": ["C"] }
                              ]
                            }
                            """;

        _root = JsonCore.Parse(json)!;

        _graph = new JsonGraph();
        _graph.Build(_root, "$.items", "id", "dependencias");
    }

    [Fact]
    public void Execute_ShouldReturnRankedItems()
    {
        const string nl = "Muéstrame los items caros y sus dependencias ordenadas por importancia";
        string aiQuery = JsonAiNl.ToAiQuery(nl);

        var sqlEngine = new JsonSqlEngine();
        List<Dictionary<string, JsonNode?>> result = AI.JsonAiEngine.Execute(_root, _graph, aiQuery, sqlEngine);

        Assert.NotEmpty(result);

        // Debe contener A y D (caros)
        List<string> ids = result.Select(r => r["id"]!.ToString().Trim('"')).ToList();

        Assert.Contains("A", ids);
        Assert.Contains("D", ids);
    }
}
