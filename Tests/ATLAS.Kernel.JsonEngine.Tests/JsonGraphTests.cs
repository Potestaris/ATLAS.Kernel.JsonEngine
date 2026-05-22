using ATLAS.Kernel.JsonEngine;
using ATLAS.Kernel.JsonEngine.Graph;

namespace ATLAS.Kernel.JsonEngine.Tests;

public class JsonGraphTests
{
    private readonly JsonGraph _graph;
    private static readonly string[] sourceArray = new[] { "A", "B", "C" };
    private static readonly string[] expected = new[] { "A", "B" };

    public JsonGraphTests()
    {
        const string json = """
                            {
                              "items": [
                                { "id": "A", "dependencias": ["B", "C"] },
                                { "id": "B", "dependencias": [] },
                                { "id": "C", "dependencias": ["B"] }
                              ]
                            }
                            """;

        var root = JsonCore.Parse(json)!;

        _graph = new JsonGraph();
        _graph.Build(root, "$.items", "id", "dependencias");
    }

    [Fact]
    public void Graph_ShouldBuildNodesAndEdges()
    {
        Assert.Equal(3, _graph.Nodes.Count);
        Assert.Equal(3, _graph.Edges.Count);
    }

    [Fact]
    public void BFS_ShouldReturnTraversal()
    {
        var bfs = _graph.Bfs("A");
        Assert.Equal(sourceArray.OrderBy(x => x), bfs.OrderBy(x => x));
    }

    [Fact]
    public void FindPath_ShouldReturnCorrectPath()
    {
        var path = _graph.FindPath("A", "B");
        Assert.Equal(expected, path);
    }

    [Fact]
    public void PageRank_ShouldReturnScores()
    {
        var pr = _graph.PageRank();
        Assert.Equal(3, pr.Count);
    }
}