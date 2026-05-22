using System.Text.Json.Nodes;

namespace ATLAS.Kernel.JsonEngine.Graph.Interfaces;

/// <summary>
/// Defines the public contract for a JSON-backed directed graph.
/// </summary>
/// <example>
/// Use the interface to depend on graph behavior without coupling callers to <c>JsonGraph</c>:
/// <code>
/// void RankItems(IJsonGraph graph)
/// {
///     var scores = graph.PageRank();
/// }
/// </code>
/// </example>
public interface IJsonGraph
{
    /// <summary>
    /// Gets the graph nodes keyed by their node identifier.
    /// </summary>
    /// <example>
    /// <code>
    /// JsonNode node = graph.Nodes["A"];
    /// </code>
    /// </example>
    Dictionary<string, JsonNode> Nodes { get; }

    /// <summary>
    /// Gets the directed adjacency list where each key contains the identifiers of its outgoing neighbors.
    /// </summary>
    /// <example>
    /// <code>
    /// List&lt;string&gt; dependencies = graph.Edges["A"];
    /// </code>
    /// </example>
    Dictionary<string, List<string>> Edges { get; }

    /// <summary>
    /// Builds the graph nodes and edges from a JSON document.
    /// </summary>
    /// <param name="root">The JSON document root.</param>
    /// <param name="pathNodes">A simplified JSONPath expression that resolves to the node array, such as <c>$.items</c>.</param>
    /// <param name="fieldId">The dot path to the field that contains each node identifier.</param>
    /// <param name="pathDeps">The path, relative to each node, that resolves to the dependency array.</param>
    /// <example>
    /// <code>
    /// IJsonGraph graph = new JsonGraph();
    /// graph.Build(root, "$.items", "id", "dependencias");
    /// </code>
    /// </example>
    void Build(JsonNode root, string pathNodes, string fieldId, string pathDeps);

    /// <summary>
    /// Calculates PageRank scores for the current graph.
    /// </summary>
    /// <param name="iterations">The number of PageRank iterations to run.</param>
    /// <param name="damping">The damping factor used to distribute rank through outgoing edges.</param>
    /// <returns>A dictionary of PageRank scores keyed by node identifier.</returns>
    /// <example>
    /// <code>
    /// var scores = graph.PageRank(iterations: 30, damping: 0.85);
    /// var scoreForA = scores["A"];
    /// </code>
    /// </example>
    Dictionary<string, double> PageRank(int iterations = 20, double damping = 0.85);
}
