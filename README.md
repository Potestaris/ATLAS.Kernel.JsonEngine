# ATLAS.Kernel.JsonEngine - JSON Query, Graph & AI Engine

**ATLAS** platform JSON processing library. Provides:

- **JSON Core**: parse JSON, navigate with simplified JSONPath/dot paths, and evaluate basic conditions.
- **SQL over JSON**: run a compact `SELECT / FROM / WHERE / ORDER BY / LIMIT` dialect over `JsonNode` arrays.
- **Graph operations**: build dependency graphs from JSON, traverse with BFS/DFS, find paths, and calculate PageRank.
- **Indexes**: simple, composite, and lightweight full-text in-memory indexes.
- **AI Query pipeline**: translate simple natural language into an executable `AI QUERY` pipeline.

> Current project files and namespaces are named `ATLAS.Kernel.JsonEngine`.
> The README keeps `ATLAS.Kernel.JsonEngine` as the public library name, but code examples use the namespace that exists in source.

---

## Architecture

```
ATLAS.Kernel.JsonEngine/
+-- Source/
|   +-- ATLAS.Kernel.JsonEngine/                <- Core library
|   |   +-- JsonCore.cs                          <- JSON parsing, path navigation, typed getters, conditions
|   |   +-- SQL/
|   |   |   +-- JsonSqlEngine.cs                 <- SQL-like query executor over JsonNode arrays
|   |   |   +-- Interfaces/
|   |   |       +-- IJsonSqlEngine.cs            <- Injectable SQL engine contract
|   |   +-- Graph/
|   |   |   +-- JsonGraph.cs                     <- Graph builder, BFS, DFS, path finding, PageRank
|   |   |   +-- Interfaces/
|   |   |       +-- IJsonGraph.cs                <- Injectable graph contract
|   |   +-- Index/
|   |   |   +-- JsonIndexEngine.cs               <- Simple, composite, and full-text indexes
|   |   +-- AI/
|   |       +-- JsonAiNl.cs                      <- Rule-based natural language to AI QUERY translator
|   |       +-- JsonAiEngine.cs                  <- AI QUERY execution pipeline
|   |
|   +-- ATLAS.Kernel.JsonEngine.Benchmark/       <- BenchmarkDotNet performance harness
|       +-- Benchmark.cs
|       +-- Program.cs
|
+-- SourceClient/
|   +-- ATLAS.Kernel.JsonEngine.Client.csproj   <- Console demo project
|   +-- Program.cs                               <- End-to-end demo: JSON -> graph -> NL -> AI QUERY
|
+-- Tests/
    +-- ATLAS.Kernel.JsonEngine.Tests/           <- xUnit + Moq tests
        +-- JsonCoreTests.cs
        +-- JsonGraphTests.cs
        +-- JsonAiNlTests.cs
        +-- JsonAiEngineTests.cs
        +-- JsonAiEngineMoqTests.cs
```

### Applied Design Principles

| Concept | Implementation |
|---|---|
| **Small core API** | `JsonCore` exposes parsing, navigation, typed getters, and simple condition evaluation |
| **Query abstraction** | `JsonSqlEngine` wraps a compact SQL-like language for JSON arrays |
| **Graph model** | `JsonGraph` stores nodes and edges in memory and supports traversal/ranking algorithms |
| **Pipeline execution** | `JsonAiEngine` executes ordered `FIND`, `GRAPH EXPAND`, `RANK BY`, and `RETURN` steps |
| **Testability** | `IJsonSqlEngine` and `IJsonGraph` allow mocking query and graph behavior |
| **No infrastructure dependency** | The library works in memory over `System.Text.Json.Nodes` |

---

## Requirements

| Tool | Minimum Version |
|---|---|
| .NET SDK | 10.0 |
| xUnit | 2.9.3 (tests only) |
| Moq | 4.20.72 (tests only) |
| BenchmarkDotNet | 0.15.8 (benchmarks only) |

No database, Redis, Docker, or external service is required.

---

## Quick Start

```bash
# 1. Restore dependencies
dotnet restore ATLAS.Kernel.JsonEngine.slnx

# 2. Build the solution
dotnet build ATLAS.Kernel.JsonEngine.slnx

# 3. Run tests
dotnet test ATLAS.Kernel.JsonEngine.slnx

# 4. Run the console demo
dotnet run --project SourceClient/ATLAS.Kernel.JsonEngine.Client.csproj
```

### Add as a Project Reference

```bash
dotnet add <your-project>.csproj reference Source/ATLAS.Kernel.JsonEngine/ATLAS.Kernel.JsonEngine.csproj
```

---

## Basic Usage

```csharp
using ATLAS.Kernel.JsonEngine;

var root = JsonCore.Parse("""
{
  "item": {
    "id": "A",
    "name": "Motor",
    "price": { "value": 120 }
  }
}
""")!;

var id = JsonCore.GetString(root, "item.id");
var price = JsonCore.GetDouble(root, "item.price.value");
var expensive = JsonCore.EvalCondition(root["item"]!, "price.value > 50");
```

### JSONPath Navigation

```csharp
var node = JsonCore.JsonPath(root, "$.item.price.value");
```

Supported path style:

| Feature | Example |
|---|---|
| Root marker | `$.items` |
| Dot navigation | `$.item.price.value` |
| Array index | `$.items[0]` |
| Empty path | returns the current/root node |

`JsonCore.Get(...)`, `GetString(...)`, and `GetDouble(...)` use simple dot paths such as `item.price.value`.

---

## SQL over JSON

`JsonSqlEngine` executes a small SQL-like dialect over JSON arrays.

```csharp
using ATLAS.Kernel.JsonEngine;
using ATLAS.Kernel.JsonEngine.SQL;

var root = JsonCore.Parse("""
{
  "items": [
    { "id": "A", "name": "Motor", "price": { "value": 120 } },
    { "id": "B", "name": "Bolt", "price": { "value": 5 } },
    { "id": "C", "name": "Shaft", "price": { "value": 40 } }
  ]
}
""")!;

var sql = new JsonSqlEngine();

var rows = sql.Execute(root, """
SELECT id, name, price.value
FROM $.items[*]
WHERE price.value > 20
ORDER BY name ASC
LIMIT 10
""");
```

### Supported SQL Clauses

| Clause | Description | Example |
|---|---|---|
| `SELECT` | Field projection or `*` | `SELECT id, name` |
| `FROM` | JSONPath to an array | `FROM $.items[*]` |
| `WHERE` | Single simple condition | `WHERE price.value > 20` |
| `ORDER BY` | Sort by one field | `ORDER BY name DESC` |
| `LIMIT` | Restrict result count | `LIMIT 10` |

### Supported Condition Operators

| Operator | Numeric | String |
|---|---:|---:|
| `=` | yes | yes |
| `<>` | yes | yes |
| `>` | yes | no |
| `<` | yes | no |
| `>=` | yes | no |
| `<=` | yes | no |

---

## Graph Engine

`JsonGraph` builds a directed graph from JSON nodes. Each node is stored by an ID field, and edges are read from a dependency array.

```csharp
using ATLAS.Kernel.JsonEngine;
using ATLAS.Kernel.JsonEngine.Graph;

var root = JsonCore.Parse("""
{
  "items": [
    { "id": "A", "dependencies": ["B", "C"] },
    { "id": "B", "dependencies": [] },
    { "id": "C", "dependencies": ["B"] }
  ]
}
""")!;

var graph = new JsonGraph();
graph.Build(root, "$.items", "id", "dependencies");

var bfs = graph.Bfs("A");
var dfs = graph.Dfs("A");
var path = graph.FindPath("A", "B");
var pageRank = graph.PageRank();
```

### Graph Methods

| Method | Description |
|---|---|
| `Build(root, pathNodes, fieldId, pathDeps)` | Builds nodes and edges from JSON |
| `Bfs(startId)` | Breadth-first traversal |
| `Dfs(startId)` | Depth-first traversal |
| `FindPath(startId, endId)` | Shortest path by BFS parent tracking |
| `PageRank(iterations, damping)` | PageRank score by node ID |

---

## Index Engine

`JsonIndexEngine` creates in-memory indexes over a `JsonArray`.

```csharp
using ATLAS.Kernel.JsonEngine;
using ATLAS.Kernel.JsonEngine.Index;

var items = JsonCore.JsonPath(root, "$.items")!.AsArray();

var byName = JsonIndexEngine.BuildSimpleIndex(items, "name");
var motors = JsonIndexEngine.Find(byName, "Motor");

var byNameAndPrice = JsonIndexEngine.BuildCompositeIndex(items, "name", "price.value");
var matches = JsonIndexEngine.FindComposite(byNameAndPrice, "Motor", "120");

var fullText = JsonIndexEngine.BuildFullTextIndex(items, "name");
var search = JsonIndexEngine.FullTextSearch(fullText, "motor");
```

| Index | Build Method | Search Method |
|---|---|---|
| Simple | `BuildSimpleIndex(items, field)` | `Find(index, value)` |
| Composite | `BuildCompositeIndex(items, fields...)` | `FindComposite(index, values...)` |
| Full-text | `BuildFullTextIndex(items, field)` | `FullTextSearch(index, query)` |

---

## AI QUERY Pipeline

`JsonAiEngine` executes a small pipeline language intended for JSON search, graph expansion, ranking, and projection.

```text
AI QUERY:
  FIND items WHERE price.value > 50
  THEN GRAPH EXPAND dependencies[*] UP TO 3 LEVELS
  THEN RANK BY PAGERANK
  RETURN id, name, price.value
```

### Pipeline Steps

| Step | Description |
|---|---|
| `FIND <entity>` | Reads `$.<entity>[*]` with the SQL engine |
| `FIND <entity> WHERE <condition>` | Adds a SQL `WHERE` condition |
| `THEN GRAPH EXPAND ... UP TO N LEVELS` | Expands related graph nodes up to `N` levels |
| `THEN RANK BY PAGERANK` | Sorts current rows by graph PageRank |
| `RETURN <fields>` | Projects fields from each selected JSON node |

### Natural Language Helper

`JsonAiNl.ToAiQuery(...)` is a rule-based helper that converts simple supported user text into an `AI QUERY`.

```csharp
using ATLAS.Kernel.JsonEngine.AI;

var aiQuery = JsonAiNl.ToAiQuery(
    "Muestrame los items caros y sus dependencias ordenadas por importancia"
);
```

The current rule set is Spanish-oriented and recognizes these patterns:

| Input signal | Output |
|---|---|
| `item` | `FIND items` |
| `nodo` | `FIND nodes` |
| `servicio` | `FIND servicios` |
| `caro` | `WHERE precio.valor > 50` |
| `muy caro` | `WHERE precio.valor > 200` |
| `barato` | `WHERE precio.valor < 20` |
| `dependenc`, `dependency`, `relacion` | `THEN GRAPH EXPAND ...` |
| `importancia`, `importantes`, `relevantes` | `THEN RANK BY PAGERANK` |
| `id`, `nombre`, `name`, `precio`, `coste`, `costo` | `RETURN ...` fields |

### End-to-End Execution

```csharp
using ATLAS.Kernel.JsonEngine;
using ATLAS.Kernel.JsonEngine.AI;
using ATLAS.Kernel.JsonEngine.Graph;
using ATLAS.Kernel.JsonEngine.SQL;

var root = JsonCore.Parse(jsonText)!;

var graph = new JsonGraph();
graph.Build(root, "$.items", "id", "dependencias");

var sqlEngine = new JsonSqlEngine();
var aiQuery = JsonAiNl.ToAiQuery(
    "Muestrame los items caros y sus dependencias ordenadas por importancia"
);

var result = JsonAiEngine.Execute(root, graph, aiQuery, sqlEngine);
```

---

## Testing and Mocking

The AI pipeline accepts `IJsonGraph` and `IJsonSqlEngine`, so tests can isolate behavior with mocks.

```csharp
var result = JsonAiEngine.Execute(
    root,
    mockGraph.Object,
    aiQuery,
    mockSql.Object
);
```

Run the full test suite:

```bash
dotnet test ATLAS.Kernel.JsonEngine.slnx
```

Run only the test project:

```bash
dotnet test Tests/ATLAS.Kernel.JsonEngine.Tests/ATLAS.Kernel.JsonEngine.Tests.csproj
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Benchmarks

Run BenchmarkDotNet benchmarks:

```bash
dotnet run -c Release --project Source/ATLAS.Kernel.JsonEngine.Benchmark/ATLAS.Kernel.JsonEngine.Benchmark.csproj
```

Current benchmark coverage:

| Benchmark | Scenario |
|---|---|
| `Benchmark_JsonGet` | Reads a nested JSON numeric value |
| `Benchmark_PageRank` | Calculates PageRank over a small dependency graph |
| `Benchmark_JsonAI` | Executes the full AI QUERY pipeline |

---

## Limitations

This library intentionally implements compact in-memory engines, not full standards-compliant parsers.

| Area | Current Scope |
|---|---|
| JSONPath | Dot navigation plus basic array indexes |
| SQL | Single-source `SELECT`, optional `WHERE`, `ORDER BY`, and `LIMIT` |
| Conditions | One comparison at a time; no `AND`, `OR`, joins, groups, or aggregates |
| AI NL | Rule-based keyword detection, not an LLM integration |
| Storage | In-memory only; caller owns persistence and cache strategy |
| Graph | Mutable dictionaries; build one graph per dataset/scope when mutating |

---

## Versioning

`Directory.Build.props` currently defines:

| Property | Value |
|---|---|
| `VersionPrefix` | `0.0.1` |
| `NuGetAudit` | `true` |

---

## License

This project is distributed under the GNU General Public License v3.0 (GPL-3.0).

This means the software is free to use, modify, and redistribute, provided that:

- The original copyright notice is preserved.
- Proper attribution is given to the original creators.
- Any derivative work distributed must remain under the same GPL license terms.

For full license terms, see the `LICENSE` file included in this repository.
