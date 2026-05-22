using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.AI;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

namespace ATLAS.Kernel.JsonEngine.Integration.Tests;

public class JsonGraphIntegrationTests
{
    [Fact]
    public void JsonAI_GraphExpand_WithLevels_ShouldExpandCorrectly()
    {
        JsonNode root = JsonNode.Parse("""
                                       {
                                         "items": [
                                           { "id": "A", "dependencias": ["B", "C"] },
                                           { "id": "B", "dependencias": ["D"] },
                                           { "id": "C", "dependencias": [] },
                                           { "id": "D", "dependencias": [] }
                                         ]
                                       }
                                       """)!;

        var graph = new JsonGraph();
        graph.Build(root, "$.items", "id", "dependencias");

        var sql = new JsonSqlEngine();

        const string aiQuery = """
                               AI QUERY:
                                 FIND items WHERE id = "A"
                                 THEN GRAPH EXPAND dependencias[*] UP TO 2 LEVELS
                                 RETURN id
                               """;

        List<Dictionary<string, JsonNode?>> result = JsonAiEngine.Execute(root, graph, aiQuery, sql);

        List<string> ids = result.Select(r => r["id"]!.ToString().Trim('\"')).ToList();

        Assert.Contains("A", ids);
        Assert.Contains("B", ids);
        Assert.Contains("C", ids);
        Assert.Contains("D", ids); // B → D (2 niveles)
    }
    [Fact]
    public void GraphExpand_WithLevels_ShouldExpandCorrectly()
    {
        JsonNode root = JsonNode.Parse("""
                                       {
                                         "items": [
                                           { "id": "A", "dependencias": ["B", "C"] },
                                           { "id": "B", "dependencias": ["D"] },
                                           { "id": "C", "dependencias": [] },
                                           { "id": "D", "dependencias": [] }
                                         ]
                                       }
                                       """)!;

        var graph = new JsonGraph();
        graph.Build(root, "$.items", "id", "dependencias");

        var sql = new JsonSqlEngine();

        const string aiQuery = """
                               AI QUERY:
                                 FIND items WHERE id = "A"
                                 THEN GRAPH EXPAND dependencias[*] UP TO 2 LEVELS
                                 RETURN id
                               """;

        List<Dictionary<string, JsonNode?>> result = JsonAiEngine.Execute(root, graph, aiQuery, sql);

        List<string> ids = result.Select(r => r["id"]!.ToString().Trim('\"')).ToList();

        Assert.Contains("A", ids);
        Assert.Contains("B", ids);
        Assert.Contains("C", ids);
        Assert.Contains("D", ids);
    }
}
