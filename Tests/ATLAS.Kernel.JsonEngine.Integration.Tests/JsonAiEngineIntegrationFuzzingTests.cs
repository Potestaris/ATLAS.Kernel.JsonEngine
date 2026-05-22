using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.AI;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

namespace ATLAS.Kernel.JsonEngine.Integration.Tests;

public class JsonAiEngineIntegrationFuzzingTests
{
    private readonly JsonNode _root;
    private readonly JsonGraph _graph;
    private readonly JsonGraph _graphDependencies;
    private readonly JsonGraph _graphInterdependencies;
    private readonly JsonGraph _graphLimits;
    private readonly JsonGraph _graphNaturalLanguage;
    private readonly JsonSqlEngine _sql;

    public JsonAiEngineIntegrationFuzzingTests()
    {
        _root = JsonNode.Parse("""
        {
          "items": [
            { "id": "A", "nombre": "Motor", "precio": { "valor": 120 }, "dependencias": ["B"], "interdependencies": ["B", "C"] , "stock": 10  },
            { "id": "B", "nombre": "Tornillo", "precio": { "valor": 5   }, "dependencias": ["D"], "interdependencies": ["D"], "stock": 0  },
            { "id": "C", "nombre": "Eje", "precio": { "valor": 40  }, "dependencias": ["B"], "interdependencies": [], "stock": 3  },
            { "id": "D", "nombre": "Tuerca", "precio": { "valor": 80  }, "dependencias": [], "interdependencies": [], "stock": 7  }
          ]
        }
        """)!;

        _graph = new JsonGraph();
        _graph.Build(_root, "$.items", "id", "precio.valor");
        _graphDependencies = new JsonGraph();
        _graphDependencies.Build(_root, "$.items", "id", "dependencias");
        _graphInterdependencies = new JsonGraph();
        _graphInterdependencies.Build(_root, "$.items", "id", "interdependencies");
        _graphLimits = new JsonGraph();
        _graphLimits.Build(_root, "$.items", "id", "id");
        _graphNaturalLanguage = new JsonGraph();
        _graphNaturalLanguage.Build(_root, "$.items", "id", "precio.valor");

        _sql = new JsonSqlEngine();
    }

    [Fact]
    public void AIQuery_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items WHERE precio.valor > 20
                                   THEN ORDER BY precio.valor DESC
                                   THEN LIMIT 2
                                   RETURN id
                                 """;

        var random = new Random(42);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzQuery(baseQuery, random);

            Exception? ex = Record.Exception(() =>
            {
                List<Dictionary<string, JsonNode?>> result = JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex); // El parser NO debe romperse nunca
        }
    }
    [Fact]
    public void FullQuery_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items WHERE precio.valor > 10
                                   THEN GRAPH EXPAND dependencias[*] UP TO 1 LEVELS
                                   THEN ORDER BY precio.valor DESC
                                   THEN LIMIT 2
                                   RETURN id, precio.valor
                                 """;

        var rnd = new Random(888);

        for (int i = 0; i < 3000; i++)
        {
            string fuzzed = FuzzEverything(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                JsonAiEngine.Execute(_root, _graphDependencies, fuzzed, _sql);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void GraphExpand_Fuzzing_ShouldNotThrowExceptions()
    {
        string baseQuery = """
                           AI QUERY:
                             FIND items WHERE id = "A"
                             THEN GRAPH EXPAND dependencias[*] UP TO 2 LEVELS
                             RETURN id
                           """;

        var rnd = new Random(123);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzQuery(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                List<Dictionary<string, JsonNode?>> result = JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex); // El parser y GRAPH EXPAND no deben romperse nunca
        }
    }
    [Fact]
    public void JsonPath_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items
                                   RETURN precio.valor, dependencias[*]
                                 """;

        var rnd = new Random(5050);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzJsonPath(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void Limit_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items
                                   THEN LIMIT 2
                                   RETURN id
                                 """;

        var rnd = new Random(3030);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzLimit(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void NaturalLanguage_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseNl = "Muéstrame los items caros ordenados por importancia";

        var rnd = new Random(999);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzedNl = FuzzNaturalLanguage(baseNl, rnd);

            string aiQuery = JsonAiNl.ToAiQuery(fuzzedNl);

            Exception? ex = Record.Exception(() =>
            {
                JsonAiEngine.Execute(_root, _graph, aiQuery, _sql);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void OrderBy_Fuzzing_ShouldNotThrowExceptions()
    {
        string baseQuery = """
                           AI QUERY:
                             FIND items
                             THEN ORDER BY precio.valor DESC
                             RETURN id
                           """;

        var rnd = new Random(2024);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzOrderBy(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void PageRank_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items
                                   THEN RANK BY PAGERANK
                                   RETURN id
                                 """;

        var rnd = new Random(4040);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzPageRank(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void Return_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items
                                   RETURN id, nombre, precio.valor
                                 """;

        var rnd = new Random(999);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzReturn(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                List<Dictionary<string, JsonNode?>> result = JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex); // RETURN debe ser robusto
        }
    }
    [Fact]
    public void SqlEngine_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseSql = "SELECT * FROM $.items[*] WHERE precio.valor > 20";

        var rnd = new Random(111);

        for (int i = 0; i < 3000; i++)
        {
            string fuzzed = FuzzSql(baseSql, rnd);

            Exception? ex = Record.Exception(() =>
            {
                _sql.Execute(_root, fuzzed);
            });

            Assert.Null(ex);
        }
    }
    [Fact]
    public void Where_Fuzzing_ShouldNotThrowExceptions()
    {
        const string baseQuery = """
                                 AI QUERY:
                                   FIND items WHERE precio.valor > 20 AND stock >= 3
                                   RETURN id
                                 """;

        var rnd = new Random(777);

        for (int i = 0; i < 2000; i++)
        {
            string fuzzed = FuzzWhere(baseQuery, rnd);

            Exception? ex = Record.Exception(() =>
            {
                var result = JsonAiEngine.Execute(_root, _graph, fuzzed, _sql);
            });

            Assert.Null(ex); // El parser WHERE debe ser robusto
        }
    }

    private static string FuzzQuery(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Replace("\n", "\n\n"),
            q => q.ToUpper(),
            q => q.ToLower(),
            q => "   " + q + "   ",
            q => q.Replace("FIND", " FIND "),
            q => q.Replace("RETURN", "  RETURN  "),
            q => q.Replace("GRAPH", "  GRAPH  "),
            q => q.Replace("EXPAND", " EXPAND "),
            q => q.Replace("LEVELS", "  LEVELS  "),
            q => q.Replace("dependencias", " dependencias "),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n")
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(6).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzEverything(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Replace("\n", "\n\n"),
            q => q.ToUpper(),
            q => q.ToLower(),
            q => "   " + q + "   ",
            q => q.Replace("FIND", " FIND "),
            q => q.Replace("WHERE", " WHERE "),
            q => q.Replace("ORDER BY", " ORDER   BY "),
            q => q.Replace("LIMIT", " LIMIT "),
            q => q.Replace("GRAPH", " GRAPH "),
            q => q.Replace("EXPAND", " EXPAND "),
            q => q.Replace("RETURN", " RETURN "),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n")
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(7).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzJsonPath(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace(".", " . "),
            q => q.Replace("[*]", " [ * ] "),
            q => q.Replace("precio", " precio "),
            q => q.Replace("valor", " valor "),
            q => q.Replace("dependencias", " dependencias "),
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n"),
            q => q.ToUpper(),
            q => q.ToLower()
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzLimit(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace("LIMIT", "  LIMIT  "),
            q => q.Replace("2", rnd.Next(1, 4).ToString()),
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n"),
            q => q.ToUpper(),
            q => q.ToLower()
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzNaturalLanguage(string text, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            t => t.Replace(" ", "  "),
            t => t.Replace(" ", "\t"),
            t => t.Replace("items", " ítems "),
            t => t.Replace("caros", " caros   "),
            t => t.Replace("importancia", " importancia "),
            t => t.Insert(rnd.Next(t.Length), " "),
            t => t.Insert(rnd.Next(t.Length), "\n"),
            t => t.ToUpper(),
            t => t.ToLower()
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(text, (current, mutate) => mutate(current));
    }
    private static string FuzzOrderBy(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace("ORDER BY", " ORDER   BY "),
            q => q.Replace("DESC", "  DESC "),
            q => q.Replace("ASC", "   ASC "),
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n"),
            q => q.ToUpper(),
            q => q.ToLower()
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzPageRank(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace("PAGERANK", " PAGE   RANK "),
            q => q.Replace("RANK BY", "  RANK   BY  "),
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n"),
            q => q.ToUpper(),
            q => q.ToLower()
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzReturn(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Replace("\n", "\n\n"),
            q => q.ToUpper(),
            q => q.ToLower(),
            q => "   " + q + "   ",
            q => q.Replace("RETURN", "  RETURN  "),
            q => q.Replace(",", " , "),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n")
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(query, (current, mutate) => mutate(current));
    }
    private static string FuzzSql(string sql, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            s => s.Replace(" ", "  "),
            s => s.Replace(" ", "\t"),
            s => s.Replace("SELECT", " SELECT "),
            s => s.Replace("FROM", " FROM "),
            s => s.Replace("WHERE", " WHERE "),
            s => s.Replace(">", " > "),
            s => s.Replace(".", " . "),
            s => s.Replace("[*]", " [ * ] "),
            s => s.Insert(rnd.Next(s.Length), " "),
            s => s.Insert(rnd.Next(s.Length), "\n"),
            s => s.ToUpper(),
            s => s.ToLower()
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(6).Aggregate(sql, (current, mutate) => mutate(current));
    }
    private static string FuzzWhere(string query, Random rnd)
    {
        var mutations = new Func<string, string>[]
        {
            q => q.Replace(" ", "  "),
            q => q.Replace(" ", "\t"),
            q => q.Replace("\n", "\n\n"),
            q => q.ToUpper(),
            q => q.ToLower(),
            q => "   " + q + "   ",
            q => q.Replace("WHERE", "  WHERE  "),
            q => q.Replace("AND", "   AND   "),
            q => q.Replace(">", " > "),
            q => q.Replace("<", " < "),
            q => q.Insert(rnd.Next(q.Length), " "),
            q => q.Insert(rnd.Next(q.Length), "\n")
        };

        return mutations.OrderBy(_ => rnd.Next()).Take(5).Aggregate(query, (current, mutate) => mutate(current));
    }

}
