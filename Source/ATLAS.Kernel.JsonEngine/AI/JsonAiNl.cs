using System.Text;

namespace ATLAS.Kernel.JsonEngine.AI;

/// <summary>
/// Converts simple natural-language search requests into the compact AI QUERY pipeline syntax used by <see cref="JsonAiEngine"/>.
/// </summary>
/// <remarks>
/// This translator is rule-based and recognizes a small set of entity, price, dependency, ranking, and return-field terms.
/// </remarks>
/// <example>
/// Convert user text into an executable AI QUERY:
/// <code>
/// var aiQuery = JsonAiNl.ToAiQuery(
///     "Muestrame los items caros y sus dependencias ordenadas por importancia"
/// );
///
/// // AI QUERY:
/// //   FIND items WHERE precio.valor &gt; 50
/// //   THEN GRAPH EXPAND dependencias[*] UP TO 3 LEVELS
/// //   THEN RANK BY PAGERANK
/// //   RETURN id, nombre
/// </code>
/// </example>
public static class JsonAiNl
{
    /// <summary>
    /// Translates a supported natural-language request into an AI QUERY pipeline.
    /// </summary>
    /// <param name="text">The user request to translate.</param>
    /// <returns>An AI QUERY string containing <c>FIND</c>, optional graph or ranking steps, and a <c>RETURN</c> projection.</returns>
    /// <example>
    /// <code>
    /// var aiQuery = JsonAiNl.ToAiQuery("items caros con id, nombre y precio");
    /// </code>
    /// </example>
    public static string ToAiQuery(string text)
    {
        string lower = text.ToLowerInvariant();
        var sb = new StringBuilder();
        sb.AppendLine("AI QUERY:");

        string entity = DetectEntity(lower) ?? "items";
        string? where = DetectPriceFilter(lower);

        if (!string.IsNullOrWhiteSpace(where))
            sb.AppendLine($"  FIND {entity} WHERE {where}");
        else
            sb.AppendLine($"  FIND {entity}");

        if (lower.Contains("dependenc") || lower.Contains("dependency") || lower.Contains("relacion") || lower.Contains("relación"))
            sb.AppendLine("  THEN GRAPH EXPAND dependencias[*] UP TO 3 LEVELS");

        if (lower.Contains("importancia") || lower.Contains("importantes") || lower.Contains("relevantes"))
            sb.AppendLine("  THEN RANK BY PAGERANK");

        string fields = DetectReturnFields(lower);

        if (string.IsNullOrWhiteSpace(fields))
            fields = "id, nombre";
        sb.Append("  RETURN ").Append(fields);

        return sb.ToString();
    }

    private static string? DetectEntity(string lower)
    {
        if (lower.Contains("item"))
            return "items";
        if (lower.Contains("nodo"))
            return "nodes";

        return lower.Contains("servicio") ? "servicios" : null;
    }

    private static string? DetectPriceFilter(string lower)
    {
        if (lower.Contains("muy caro"))
            return "precio.valor > 200";
        if (lower.Contains("caro"))
            return "precio.valor > 50";

        return lower.Contains("barato") ? "precio.valor < 20" : null;
    }

    private static string DetectReturnFields(string lower)
    {
        var fields = new StringBuilder();

        if (lower.Contains("id"))
            fields.Append("id, ");
        if (lower.Contains("nombre") || lower.Contains("name"))
            fields.Append("nombre, ");
        if (lower.Contains("precio") || lower.Contains("coste") || lower.Contains("costo"))
            fields.Append("precio.valor, ");

        return fields.Length == 0 ? "" : fields.ToString(0, fields.Length - 2);
    }
}
