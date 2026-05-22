using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ATLAS.Kernel.JsonEngine;

/// <summary>
/// Provides lightweight helpers for parsing, navigating, reading, and evaluating values in <see cref="JsonNode"/> documents.
/// </summary>
/// <remarks>
/// The helper methods intentionally support a compact subset of JSON navigation that is useful for in-memory querying.
/// </remarks>
/// <example>
/// Parse a document and read nested values:
/// <code>
/// var root = JsonCore.Parse("""
/// {
///   "item": {
///     "id": "A",
///     "price": { "value": 120 }
///   }
/// }
/// """)!;
///
/// var id = JsonCore.GetString(root, "item.id");
/// var price = JsonCore.GetDouble(root, "item.price.value");
/// </code>
/// </example>
public static class JsonCore
{
    /// <summary>
    /// Parses a JSON string into a <see cref="JsonNode"/> tree.
    /// </summary>
    /// <param name="json">The JSON text to parse.</param>
    /// <returns>The parsed JSON root node, or <see langword="null"/> when the parsed JSON literal is null.</returns>
    /// <example>
    /// <code>
    /// var root = JsonCore.Parse("""{ "name": "Motor" }""");
    /// var name = JsonCore.GetString(root, "name");
    /// </code>
    /// </example>
    public static JsonNode? Parse(string json)
    {
        return JsonNode.Parse(json);
    }

    /// <summary>
    /// Navigates a JSON node with a simplified JSONPath expression.
    /// </summary>
    /// <param name="root">The root node to navigate from.</param>
    /// <param name="path">A simplified path such as <c>$.items</c>, <c>$.items[0]</c>, or <c>$.items[0].id</c>.</param>
    /// <returns>The node found at the path, or <see langword="null"/> when the path cannot be resolved.</returns>
    /// <example>
    /// <code>
    /// var root = JsonCore.Parse("""{ "items": [{ "id": "A" }] }""")!;
    /// var firstId = JsonCore.JsonPath(root, "$.items[0].id");
    /// </code>
    /// </example>
    public static JsonNode? JsonPath(JsonNode? root, string path)
    {
        if (root is null)
            return null;
        if (string.IsNullOrWhiteSpace(path))
            return root;

        string p = path.Trim();

        if (p.StartsWith("$."))
            p = p[2..];

        string[] parts = p.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? current = root;

        foreach (string part in parts)
        {
            if (current is null) return null;

            // Array index: items[0] or items[*]
            string name = part;
            int? index = null;
            int bracketIdx = part.IndexOf('[');

            if (bracketIdx >= 0)
            {
                name = part[..bracketIdx].Trim();
                int endBracket = part.LastIndexOf(']');
                if (endBracket > bracketIdx)
                {
                    string idxStr = part[(bracketIdx + 1)..endBracket].Trim();
                    if (idxStr == "*" || string.IsNullOrEmpty(idxStr))
                    {
                        index = null;
                    }
                    else if (int.TryParse(idxStr, out var idx))
                    {
                        index = idx;
                    }
                }
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (current is JsonObject obj)
                    current = obj[name];
                else
                    return null;
            }

            if (!index.HasValue)
                continue;

            if (current is not JsonArray arr)
                return null;

            if (index.Value < 0 || index.Value >= arr.Count)
                return null;

            current = arr[index.Value];
        }

        return current;
    }

    /// <summary>
    /// Reads a value from a node by following a simple dot-separated path.
    /// </summary>
    /// <param name="node">The node to navigate from.</param>
    /// <param name="path">A dot-separated path such as <c>price.value</c>.</param>
    /// <returns>The node found at the path, or <see langword="null"/> when the path cannot be resolved.</returns>
    /// <example>
    /// <code>
    /// var root = JsonCore.Parse("""{ "item": { "price": { "value": 120 } } }""")!;
    /// var priceNode = JsonCore.Get(root, "item.price.value");
    /// </code>
    /// </example>
    public static JsonNode? Get(JsonNode? node, string path)
    {
        if (node is null)
            return null;
        if (string.IsNullOrWhiteSpace(path))
            return node;

        string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? current = node;

        foreach (string part in parts)
        {
            if (current is null)
                return null;
            current = current[part];
        }

        return current;
    }

    /// <summary>
    /// Reads a numeric value from a node by following a simple dot-separated path.
    /// </summary>
    /// <param name="node">The node to navigate from.</param>
    /// <param name="path">A dot-separated path such as <c>price.value</c>.</param>
    /// <returns>The numeric value, or <see langword="null"/> when the path is missing or the value cannot be converted to <see cref="double"/>.</returns>
    /// <example>
    /// <code>
    /// var root = JsonCore.Parse("""{ "item": { "price": { "value": 120 } } }""")!;
    /// double? price = JsonCore.GetDouble(root, "item.price.value");
    /// </code>
    /// </example>
    public static double? GetDouble(JsonNode? node, string path)
    {
        JsonNode? n = Get(node, path);

        if (n is null)
            return null;
        if (n is JsonValue jv && jv.TryGetValue(out double d))
            return d;

        string s = n.ToString();
        if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
            s = s[1..^1];
        if (double.TryParse(s, out d))
            return d;
        return null;
    }

    /// <summary>
    /// Reads a string representation of a value from a node by following a simple dot-separated path.
    /// </summary>
    /// <param name="node">The node to navigate from.</param>
    /// <param name="path">A dot-separated path such as <c>name</c> or <c>item.name</c>.</param>
    /// <returns>The value converted to a string, or <see langword="null"/> when the path cannot be resolved.</returns>
    /// <example>
    /// <code>
    /// var root = JsonCore.Parse("""{ "item": { "name": "Motor" } }""")!;
    /// var name = JsonCore.GetString(root, "item.name");
    /// </code>
    /// </example>
    public static string? GetString(JsonNode? node, string path)
    {
        JsonNode? n = Get(node, path);

        if (n is null) return null;
        if (n is JsonValue jv && jv.TryGetValue<string>(out var s))
            return s;
        string s2 = n.ToString();
        if (s2.StartsWith("\"") && s2.EndsWith("\"") && s2.Length >= 2)
            s2 = s2[1..^1];
        return s2;
    }

    /// <summary>
    /// Evaluates a simple comparison expression against a JSON object.
    /// </summary>
    /// <param name="obj">The JSON object that provides the values used by the expression.</param>
    /// <param name="expr">A comparison such as <c>price.value &gt; 20</c>, <c>name = Motor</c>, or <c>status &lt;&gt; Closed</c>.</param>
    /// <returns><see langword="true"/> when the expression matches; otherwise, <see langword="false"/>.</returns>
    /// <example>
    /// <code>
    /// var root = JsonCore.Parse("""{ "item": { "price": { "value": 120 } } }""")!;
    /// var expensive = JsonCore.EvalCondition(root["item"]!, "price.value &gt; 50");
    /// </code>
    /// </example>
    public static bool EvalCondition(JsonNode obj, string expr)
    {
        // Support simple boolean connectors AND / OR at top-level (no parentheses)
        if (Regex.IsMatch(expr, "\\bOR\\b", RegexOptions.IgnoreCase))
        {
            var parts = Regex.Split(expr, "\\bOR\\b", RegexOptions.IgnoreCase).Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p));
            foreach (var p in parts)
                if (EvalCondition(obj, p))
                    return true;
            return false;
        }

        if (Regex.IsMatch(expr, "\\bAND\\b", RegexOptions.IgnoreCase))
        {
            var parts = Regex.Split(expr, "\\bAND\\b", RegexOptions.IgnoreCase).Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p));
            foreach (var p in parts)
                if (!EvalCondition(obj, p))
                    return false;
            return true;
        }

        string? op = FindOperator(expr);

        if (op is null)
            return false;

        int idx = expr.IndexOf(op, StringComparison.Ordinal);
        string left = expr[..idx].Trim();
        string right = expr[(idx + op.Length)..].Trim();

        JsonNode? leftNode = Get(obj, left);
        if (leftNode is null) return false;

        // Try numeric comparison first
        double? leftNum = GetDouble(obj, left);
        if (leftNum.HasValue && double.TryParse(right, out double rightNum))
        {
            return op switch
            {
                ">=" => leftNum.Value >= rightNum,
                "<=" => leftNum.Value <= rightNum,
                "<>" => Math.Abs(leftNum.Value - rightNum) > double.Epsilon,
                ">"  => leftNum.Value > rightNum,
                "<"  => leftNum.Value < rightNum,
                "="  => Math.Abs(leftNum.Value - rightNum) < double.Epsilon,
                _    => false
            };
        }

        // Fallback to string comparison
        string leftStr;
        if (leftNode is JsonValue jv && jv.TryGetValue<string>(out var s))
            leftStr = s;
        else
        {
            leftStr = leftNode.ToString();
            if (leftStr.StartsWith("\"") && leftStr.EndsWith("\"") && leftStr.Length >= 2)
                leftStr = leftStr[1..^1];
        }

        string rightStr = right;
        if (rightStr.StartsWith("\"") && rightStr.EndsWith("\"") && rightStr.Length >= 2)
            rightStr = rightStr[1..^1];

        return op switch
        {
            "=" => string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
            "<>" => !string.Equals(leftStr, rightStr, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string? FindOperator(string expr)
    {
        string[] ops = new[] { ">=", "<=", "<>", ">", "<", "=" };

        return ops.FirstOrDefault(op => expr.Contains(op, StringComparison.Ordinal));
    }
}
