using System.Text.Json.Nodes;

namespace ATLAS.Kernel.JsonEngine.Index;

/// <summary>
/// Builds and searches in-memory indexes over JSON arrays.
/// </summary>
/// <remarks>
/// The index dictionaries use case-insensitive string keys and store the matching <see cref="JsonNode"/> items.
/// </remarks>
/// <example>
/// Create and query simple, composite, and full-text indexes:
/// <code>
/// var items = JsonCore.JsonPath(root, "$.items")!.AsArray();
///
/// var byName = JsonIndexEngine.BuildSimpleIndex(items, "name");
/// var motors = JsonIndexEngine.Find(byName, "Motor");
///
/// var byNameAndPrice = JsonIndexEngine.BuildCompositeIndex(items, "name", "price.value");
/// var matches = JsonIndexEngine.FindComposite(byNameAndPrice, "Motor", "120");
///
/// var fullText = JsonIndexEngine.BuildFullTextIndex(items, "description");
/// var searchResults = JsonIndexEngine.FullTextSearch(fullText, "electric motor");
/// </code>
/// </example>
public static class JsonIndexEngine
{
    /// <summary>
    /// Builds a simple index from one field value to the JSON nodes that contain it.
    /// </summary>
    /// <param name="items">The JSON array to index.</param>
    /// <param name="field">The dot path of the field used as the index key.</param>
    /// <returns>A case-insensitive index keyed by field value.</returns>
    /// <example>
    /// <code>
    /// var index = JsonIndexEngine.BuildSimpleIndex(items, "name");
    /// var motors = JsonIndexEngine.Find(index, "Motor");
    /// </code>
    /// </example>
    public static Dictionary<string, List<JsonNode>> BuildSimpleIndex(JsonArray items, string field)
    {
        var idx = new Dictionary<string, List<JsonNode>>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonNode? item in items)
        {
            if (item is null)
                continue;
            string val = JsonCore.Get(item, field)?.ToString() ?? "";
            if (!idx.TryGetValue(val, out List<JsonNode>? list))
            {
                list = new List<JsonNode>();
                idx[val] = list;
            }
            list.Add(item);
        }

        return idx;
    }

    /// <summary>
    /// Finds nodes in a simple index by key value.
    /// </summary>
    /// <param name="index">The index created by <see cref="BuildSimpleIndex"/>.</param>
    /// <param name="value">The key value to search for.</param>
    /// <returns>The nodes stored for the value, or an empty list when the value is not indexed.</returns>
    /// <example>
    /// <code>
    /// var matches = JsonIndexEngine.Find(index, "Motor");
    /// </code>
    /// </example>
    public static List<JsonNode> Find(Dictionary<string, List<JsonNode>> index, string value)
    {
        return index.TryGetValue(value, out List<JsonNode>? list)
            ? list
            : new List<JsonNode>();
    }

    /// <summary>
    /// Builds a composite index from several field values to the JSON nodes that contain them.
    /// </summary>
    /// <param name="items">The JSON array to index.</param>
    /// <param name="fields">The ordered dot paths used to build the composite key.</param>
    /// <returns>A case-insensitive composite index keyed by values joined in the same order as <paramref name="fields"/>.</returns>
    /// <example>
    /// <code>
    /// var index = JsonIndexEngine.BuildCompositeIndex(items, "name", "price.value");
    /// var matches = JsonIndexEngine.FindComposite(index, "Motor", "120");
    /// </code>
    /// </example>
    public static Dictionary<string, List<JsonNode>> BuildCompositeIndex(JsonArray items, params string[] fields)
    {
        var idx = new Dictionary<string, List<JsonNode>>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonNode? item in items)
        {
            if (item is null)
                continue;
            string key = string.Join("|", fields.Select(f => JsonCore.Get(item, f)?.ToString() ?? ""));
            if (!idx.TryGetValue(key, out List<JsonNode>? list))
            {
                list = new List<JsonNode>();
                idx[key] = list;
            }
            list.Add(item);
        }

        return idx;
    }

    /// <summary>
    /// Finds nodes in a composite index by the ordered key values.
    /// </summary>
    /// <param name="index">The index created by <see cref="BuildCompositeIndex"/>.</param>
    /// <param name="values">The ordered values that make up the composite key.</param>
    /// <returns>The nodes stored for the composite value, or an empty list when the value is not indexed.</returns>
    /// <example>
    /// <code>
    /// var matches = JsonIndexEngine.FindComposite(index, "Motor", "120");
    /// </code>
    /// </example>
    public static List<JsonNode> FindComposite(Dictionary<string, List<JsonNode>> index, params string[] values)
    {
        string key = string.Join("|", values);

        return index.TryGetValue(key, out List<JsonNode>? list) ? list : new List<JsonNode>();
    }

    /// <summary>
    /// Builds a lightweight full-text index by tokenizing one string field.
    /// </summary>
    /// <param name="items">The JSON array to index.</param>
    /// <param name="field">The dot path of the text field to tokenize.</param>
    /// <returns>A case-insensitive index keyed by normalized token.</returns>
    /// <example>
    /// <code>
    /// var index = JsonIndexEngine.BuildFullTextIndex(items, "description");
    /// var results = JsonIndexEngine.FullTextSearch(index, "electric motor");
    /// </code>
    /// </example>
    public static Dictionary<string, List<JsonNode>> BuildFullTextIndex(JsonArray items, string field)
    {
        var idx = new Dictionary<string, List<JsonNode>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (item is null)
                continue;

            string text = JsonCore.Get(item, field)?.ToString() ?? "";
            IEnumerable<string> tokens = Tokenize(text);

            foreach (string t in tokens)
            {
                if (!idx.TryGetValue(t, out var list))
                {
                    list = new List<JsonNode>();
                    idx[t] = list;
                }
                list.Add(item);
            }
        }

        return idx;
    }

    /// <summary>
    /// Searches a full-text index and orders results by token match count.
    /// </summary>
    /// <param name="index">The index created by <see cref="BuildFullTextIndex"/>.</param>
    /// <param name="query">The search text to tokenize and match.</param>
    /// <returns>The matching nodes ordered by descending number of matched tokens.</returns>
    /// <example>
    /// <code>
    /// var results = JsonIndexEngine.FullTextSearch(index, "electric motor");
    /// </code>
    /// </example>
    public static List<JsonNode> FullTextSearch(Dictionary<string, List<JsonNode>> index, string query)
    {
        IEnumerable<string> tokens = Tokenize(query);
        var scores = new Dictionary<JsonNode, int>();

        foreach (string t in tokens)
        {
            if (!index.TryGetValue(t, out List<JsonNode>? list))
                continue;
            foreach (JsonNode item in list)
            {
                scores.TryAdd(item, 0);
                scores[item]++;
            }
        }

        return scores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        char[] chars = text.ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray();

        return new string(chars)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
