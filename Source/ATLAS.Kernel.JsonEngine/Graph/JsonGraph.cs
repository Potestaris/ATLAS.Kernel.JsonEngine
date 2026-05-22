using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.Graph.Interfaces;

namespace ATLAS.Kernel.JsonEngine.Graph;

/// <summary>
/// Builds and queries an in-memory directed graph whose nodes and edges are read from JSON data.
/// </summary>
/// <example>
/// Build a dependency graph and find a path:
/// <code>
/// var root = JsonCore.Parse("""
/// {
///   "items": [
///     { "id": "A", "dependencias": ["B"] },
///     { "id": "B", "dependencias": [] }
///   ]
/// }
/// """)!;
///
/// var graph = new JsonGraph();
/// graph.Build(root, "$.items", "id", "dependencias");
///
/// var path = graph.FindPath("A", "B");
/// </code>
/// </example>
public class JsonGraph : IJsonGraph
{
    /// <summary>
    /// Gets the graph nodes keyed by their node identifier.
    /// </summary>
    /// <example>
    /// <code>
    /// var nodeA = graph.Nodes["A"];
    /// var name = JsonCore.GetString(nodeA, "nombre");
    /// </code>
    /// </example>
    public Dictionary<string, JsonNode> Nodes { get; } = new();

    /// <summary>
    /// Gets the directed adjacency list where each key contains the identifiers of its outgoing neighbors.
    /// </summary>
    /// <example>
    /// <code>
    /// var dependencies = graph.Edges["A"];
    /// </code>
    /// </example>
    public Dictionary<string, List<string>> Edges { get; } = new();

    /// <summary>
    /// Builds the graph nodes and edges from a JSON document.
    /// </summary>
    /// <param name="root">The JSON document root.</param>
    /// <param name="pathNodes">A simplified JSONPath expression that resolves to the node array, such as <c>$.items</c>.</param>
    /// <param name="fieldId">The dot path to the field that contains each node identifier.</param>
    /// <param name="pathDeps">The path, relative to each node, that resolves to the dependency array.</param>
    /// <example>
    /// <code>
    /// var graph = new JsonGraph();
    /// graph.Build(root, "$.items", "id", "dependencias");
    /// </code>
    /// </example>
    public void Build(JsonNode root, string pathNodes, string fieldId, string pathDeps)
    {
        var nodesArray = JsonCore.JsonPath(root, pathNodes) as JsonArray;
        if (nodesArray is null) return;

        foreach (JsonNode? item in nodesArray)
        {
            if (item is null)
                continue;
            string? id = JsonCore.Get(item, fieldId)?.ToString();
            if (string.IsNullOrEmpty(id))
                continue;

            Nodes[id] = item;
            Edges[id] = new List<string>();

            JsonNode? depsNode = JsonCore.JsonPath(item, pathDeps);
            if (depsNode is not JsonArray depsArr)
                continue;
            foreach (JsonNode? d in depsArr)
            {
                if (d is null)
                    continue;
                string depId = d.ToString();
                Edges[id].Add(depId);
            }
        }
    }

    /// <summary>
    /// Traverses the graph breadth-first from a starting node.
    /// </summary>
    /// <param name="startId">The identifier of the node where traversal starts.</param>
    /// <returns>The visited node identifiers in breadth-first order, or an empty list when <paramref name="startId"/> is not in the graph.</returns>
    /// <example>
    /// <code>
    /// var visited = graph.Bfs("A");
    /// </code>
    /// </example>
    public List<string> Bfs(string startId)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var result = new List<string>();

        if (!Nodes.ContainsKey(startId))
            return result;

        queue.Enqueue(startId);
        visited.Add(startId);

        while (queue.Count > 0)
        {
            string id = queue.Dequeue();
            result.Add(id);

            if (!Edges.TryGetValue(id, out List<string>? children))
                continue;
            foreach (string c in children.Where(c => visited.Add(c)))
            {
                queue.Enqueue(c);
            }
        }

        return result;
    }

    /// <summary>
    /// Traverses the graph depth-first from a starting node.
    /// </summary>
    /// <param name="startId">The identifier of the node where traversal starts.</param>
    /// <returns>The visited node identifiers in depth-first order.</returns>
    /// <example>
    /// <code>
    /// var visited = graph.Dfs("A");
    /// </code>
    /// </example>
    public List<string> Dfs(string startId)
    {
        var visited = new HashSet<string>();
        var result = new List<string>();
        DfsVisit(startId, visited, result);

        return result;
    }

    private void DfsVisit(string id, HashSet<string> visited, List<string> result)
    {
        if (!visited.Add(id))
            return;
        result.Add(id);

        if (!Edges.TryGetValue(id, out List<string>? children))
            return;
        foreach (string c in children)
            DfsVisit(c, visited, result);
    }

    /// <summary>
    /// Finds a shortest directed path between two nodes by breadth-first parent tracking.
    /// </summary>
    /// <param name="startId">The identifier of the starting node.</param>
    /// <param name="endId">The identifier of the target node.</param>
    /// <returns>The node identifiers in the path from <paramref name="startId"/> to <paramref name="endId"/>, or an empty list when no path exists.</returns>
    /// <example>
    /// <code>
    /// var path = graph.FindPath("A", "B");
    /// </code>
    /// </example>
    public List<string> FindPath(string startId, string endId)
    {
        var parents = new Dictionary<string, string?>();
        var queue = new Queue<string>();

        if (!Nodes.ContainsKey(startId) || !Nodes.ContainsKey(endId))
            return new List<string>();
        queue.Enqueue(startId);
        parents[startId] = null;
        while (queue.Count > 0)
        {
            string id = queue.Dequeue();
            if (id == endId)
                break;
            if (!Edges.TryGetValue(id, out var children))
                continue;
            foreach (string c in children.Where(c => !parents.ContainsKey(c)))
            {
                parents[c] = id;
                queue.Enqueue(c);
            }
        }

        if (!parents.ContainsKey(endId)) return new List<string>();

        var path = new List<string>();
        string? cur = endId;
        while (cur != null)
        {
            path.Insert(0, cur);
            cur = parents[cur];
        }

        return path;
    }

    /// <summary>
    /// Calculates PageRank scores for the current graph.
    /// </summary>
    /// <param name="iterations">The number of PageRank iterations to run.</param>
    /// <param name="damping">The damping factor used to distribute rank through outgoing edges.</param>
    /// <returns>A dictionary of PageRank scores keyed by node identifier.</returns>
    /// <example>
    /// <code>
    /// var scores = graph.PageRank(iterations: 30, damping: 0.85);
    /// var mostImportant = scores.OrderByDescending(score => score.Value).First();
    /// </code>
    /// </example>
    public Dictionary<string, double> PageRank(int iterations = 20, double damping = 0.85)
    {
        Dictionary<string, double> rank = Nodes.Keys.ToDictionary(k => k, _ => 1.0 / Nodes.Count);
        Dictionary<string, double> newRank = Nodes.Keys.ToDictionary(k => k, _ => 0.0);

        for (int it = 0; it < iterations; it++)
        {
            foreach (string k in Nodes.Keys)
                newRank[k] = (1 - damping) / Nodes.Count;
            foreach ((string id, List<string> children) in Edges)
            {
                if (children.Count == 0)
                    continue;
                double share = rank[id] / children.Count;
                foreach (string c in children.Where(c => newRank.ContainsKey(c)))
                {
                    newRank[c] += damping * share;
                }
            }
            foreach (string k in Nodes.Keys)
                rank[k] = newRank[k];
        }

        return rank;
    }
}
