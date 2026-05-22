using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine;

namespace ATLAS.Kernel.JsonEngine.Tests;

public class JsonCoreTests
{
    private readonly JsonNode _root;

    public JsonCoreTests()
    {
        var json = """
                   {
                     "item": {
                       "id": "A",
                       "precio": { "valor": 120 },
                       "tags": ["uno", "dos"]
                     }
                   }
                   """;

        _root = JsonCore.Parse(json)!;
    }

    [Fact]
    public void Parse_ShouldLoadJson()
    {
        Assert.NotNull(_root);
        Assert.Equal("A", JsonCore.GetString(_root, "item.id"));
    }

    [Fact]
    public void JsonPath_ShouldNavigateCorrectly()
    {
        var node = JsonCore.JsonPath(_root, "$.item.precio.valor");
        Assert.Equal("120", node!.ToString());
    }

    [Fact]
    public void GetDouble_ShouldReturnNumericValue()
    {
        var val = JsonCore.GetDouble(_root, "item.precio.valor");
        Assert.Equal(120, val);
    }

    [Fact]
    public void EvalCondition_ShouldEvaluateNumericExpression()
    {
        var ok = JsonCore.EvalCondition(_root["item"]!, "precio.valor > 100");
        Assert.True(ok);
    }
}