using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

namespace ATLAS.Kernel.JsonEngine.Tests.JsonAiEngine;

public class JsonAiQueryTests
{
    [Fact]
    public void JsonAI_AIQueryParser_ShouldRemainStable()
    {
        JsonNode root = JsonNode.Parse("""
                                       {
                                         "items": [
                                           { "id": "A", "precio": { "valor": 120 } },
                                           { "id": "B", "precio": { "valor": 5   } },
                                           { "id": "C", "precio": { "valor": 40  } },
                                           { "id": "D", "precio": { "valor": 80  } }
                                         ]
                                       }
                                       """)!;

        var graph = new JsonGraph();
        graph.Build(root, "$.items", "id", "precio.valor");

        var sql = new JsonSqlEngine();

        // -----------------------------
        // 3 variantes de la misma AI QUERY
        // -----------------------------
        string[] queries =
        [
            """
            AI QUERY:
              FIND items WHERE precio.valor > 20
              THEN ORDER BY precio.valor DESC
              THEN LIMIT 2
              RETURN id
            """,

            """
            AI QUERY:
            FIND   items   WHERE   precio.valor   >   20
            THEN   ORDER BY   precio.valor   DESC
            THEN   LIMIT   2
            RETURN   id
            """,

            """
            AI QUERY:
            FIND items
            WHERE precio.valor > 20
            THEN ORDER BY precio.valor DESC
            THEN LIMIT 2
            RETURN id
            """
        ];

        List<List<string>> results = queries
            .Select(q => AI.JsonAiEngine.Execute(root, graph, q, sql))
            .Select(r => r.Select(x => x["id"]!.ToString().Trim('"')).ToList())
            .ToList();

        // -----------------------------
        // Todas las variantes deben producir el mismo resultado
        // -----------------------------
        Assert.Equal(results[0], results[1]);
        Assert.Equal(results[1], results[2]);

        // Validación del contenido
        Assert.Equal("A", results[0][0]); // 120
        Assert.Equal("D", results[0][1]); // 80
    }
}
