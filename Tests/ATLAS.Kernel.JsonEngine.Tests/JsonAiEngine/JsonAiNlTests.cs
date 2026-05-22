using ATLAS.Kernel.JsonEngine.AI;

namespace ATLAS.Kernel.JsonEngine.Tests.JsonAiEngine;

public class JsonAiNlTests
{
    [Fact]
    public void ToAiQuery_ShouldGeneratePipeline()
    {
        const string nl = "Muéstrame los items caros y sus dependencias ordenadas por importancia";
        var ai = JsonAiNl.ToAiQuery(nl);

        Assert.Contains("FIND items WHERE precio.valor > 50", ai);
        Assert.Contains("GRAPH EXPAND", ai);
        Assert.Contains("RANK BY PAGERANK", ai);
        Assert.Contains("RETURN id, nombre", ai);
    }
}
