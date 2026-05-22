using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.AI;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

namespace ATLAS.Kernel.JsonEngine.Client;

public static class Program
{
    private static void Main()
    {
        Console.WriteLine("=== JSON AI ENGINE DEMO ===");

        // ---------------------------------------------------------
        // 1. Cargar JSON
        // ---------------------------------------------------------
        const string jsonText = @"
            {
              ""items"": [
                { ""id"": ""A"", ""nombre"": ""Motor"", ""precio"": { ""valor"": 120 }, ""dependencias"": [""B"", ""C""] },
                { ""id"": ""B"", ""nombre"": ""Tornillo"", ""precio"": { ""valor"": 5 },   ""dependencias"": [] },
                { ""id"": ""C"", ""nombre"": ""Eje"",     ""precio"": { ""valor"": 40 },  ""dependencias"": [""B""] },
                { ""id"": ""D"", ""nombre"": ""Filtro"",  ""precio"": { ""valor"": 80 },  ""dependencias"": [""C""] }
              ]
            }";

        JsonNode root = JsonCore.Parse(jsonText)!;
        Console.WriteLine("JSON cargado correctamente.");

        // ---------------------------------------------------------
        // 2. Construir el grafo
        // ---------------------------------------------------------
        var graph = new JsonGraph();
        graph.Build(
            root,
            "$.items",          // ruta a los nodos
            "id",               // campo ID
            "dependencias"      // campo dependencias
        );

        Console.WriteLine("Grafo construido:");
        Console.WriteLine($"Nodos: {graph.Nodes.Count}");
        Console.WriteLine($"Aristas: {graph.Edges.Count}");

        // ---------------------------------------------------------
        // 3. Frase en lenguaje natural
        // ---------------------------------------------------------
        const string frase = "Muéstrame los items caros y sus dependencias ordenadas por importancia";

        Console.WriteLine();
        Console.WriteLine("Frase NL:");
        Console.WriteLine(frase);

        // Convertir NL → AI QUERY
        string aiQuery = JsonAiNl.ToAiQuery(frase);

        Console.WriteLine();
        Console.WriteLine("AI QUERY generada:");
        Console.WriteLine(aiQuery);

        // ---------------------------------------------------------
        // 4. Ejecutar JsonAiEngine
        // ---------------------------------------------------------
        var sqlEngine = new JsonSqlEngine();
        List<Dictionary<string, JsonNode?>> resultado = JsonAiEngine.Execute(root, graph, aiQuery, sqlEngine);

        // ---------------------------------------------------------
        // 5. Imprimir resultados
        // ---------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("=== RESULTADO FINAL ===");

        foreach (Dictionary<string, JsonNode?> row in resultado)
        {
            Console.WriteLine("----");
            foreach (KeyValuePair<string, JsonNode?> kv in row)
            {
                Console.WriteLine($"{kv.Key}: {kv.Value}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Fin de la demo.");
    }
}
