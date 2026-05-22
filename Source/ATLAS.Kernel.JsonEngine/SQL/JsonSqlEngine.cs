using System.Text.Json.Nodes;
using ATLAS.Kernel.JsonEngine.SQL.Interfaces;

namespace ATLAS.Kernel.JsonEngine.SQL;

/// <summary>
/// Executes a compact SQL-like query language over arrays inside <see cref="JsonNode"/> documents.
/// </summary>
/// <remarks>
/// Supported clauses are <c>SELECT</c>, <c>FROM</c>, optional <c>WHERE</c>, optional <c>ORDER BY</c>, and optional <c>LIMIT</c>.
/// </remarks>
/// <example>
/// Query JSON items by nested price:
/// <code>
/// var root = JsonCore.Parse("""
/// {
///   "items": [
///     { "id": "A", "name": "Motor", "price": { "value": 120 } },
///     { "id": "B", "name": "Bolt", "price": { "value": 5 } }
///   ]
/// }
/// """)!;
///
/// var sql = new JsonSqlEngine();
/// var rows = sql.Execute(root, """
/// SELECT id, name, price.value
/// FROM $.items[*]
/// WHERE price.value &gt; 20
/// """);
/// </code>
/// </example>
public class JsonSqlEngine : IJsonSqlEngine
{
    /// <summary>
    /// Executes a SQL-like query against a JSON document.
    /// </summary>
    /// <param name="root">The JSON document root.</param>
    /// <param name="sql">The SQL-like query text to execute.</param>
    /// <returns>A list of rows containing either projected fields or the full matching node under the <c>value</c> key when <c>SELECT *</c> is used.</returns>
    /// <example>
    /// <code>
    /// var rows = new JsonSqlEngine().Execute(root, """
    /// SELECT id, nombre
    /// FROM $.items[*]
    /// WHERE precio.valor &gt; 50
    /// ORDER BY nombre ASC
    /// LIMIT 10
    /// """);
    /// </code>
    /// </example>
    public List<JsonSqlRow> Execute(JsonNode root, string sql)
    {
        return ExecuteInternal(root, sql);
    }

    private static List<JsonSqlRow> ExecuteInternal(JsonNode root, string sql)
    {
        JsonSqlStatement stmt = JsonSqlStatement.Parse(sql);
        // FROM
        JsonNode? fromNode = JsonCore.JsonPath(root, stmt.From);
        JsonArray items = fromNode as JsonArray ?? new JsonArray();
        IEnumerable<JsonNode?> query = items;

        // WHERE
        if (!string.IsNullOrWhiteSpace(stmt.Where))
        {
            query = query.Where(n => n is not null && JsonCore.EvalCondition(n!, stmt.Where));
        }

        // ORDER BY
        if (!string.IsNullOrWhiteSpace(stmt.OrderByField))
        {
            query = stmt.OrderByDir.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(n => JsonCore.Get(n, stmt.OrderByField)?.ToString())
                : query.OrderBy(n => JsonCore.Get(n, stmt.OrderByField)?.ToString());
        }

        // LIMIT
        if (stmt.Limit.HasValue)
        {
            query = query.Take(stmt.Limit.Value);
        }

        // SELECT
        var result = new List<JsonSqlRow>();
        string[]? fields = stmt.Select == "*"
            ? null
            : stmt.Select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (JsonNode? item in query)
        {
            if (item is null)
                continue;
            var row = new JsonSqlRow();

            if (fields is null)
            {
                row["value"] = item;
            }
            else
            {
                foreach (string f in fields)
                {
                    row[f] = JsonCore.Get(item, f);
                }
            }
            result.Add(row);
        }

        return result;
    }
}

/// <summary>
/// Represents one row returned by <see cref="JsonSqlEngine"/>, keyed by selected field name.
/// </summary>
/// <example>
/// Read projected fields from a result row:
/// <code>
/// JsonSqlRow row = rows[0];
/// var id = row["id"]?.ToString();
/// var price = row["precio.valor"]?.ToString();
/// </code>
/// </example>
public class JsonSqlRow : Dictionary<string, JsonNode?> { }

internal sealed class JsonSqlStatement
{
    public string Select { get; set; } = "*";
    public string From { get; set; } = "$";
    public string Where { get; set; } = "";
    public string OrderByField { get; set; } = "";
    public string OrderByDir { get; set; } = "ASC";
    public int? Limit { get; set; }

    public static JsonSqlStatement Parse(string sql)
    {
        var stmt = new JsonSqlStatement();
        string text = sql.Trim();

        stmt.Select = Between(text, "SELECT", "FROM").Trim();
        stmt.From   = Between(text, "FROM", new[] { "WHERE", "ORDER BY", "LIMIT" }).Trim();

        string where = BetweenOptional(text, "WHERE", new[] { "ORDER BY", "LIMIT" });
        if (!string.IsNullOrWhiteSpace(where))
            stmt.Where = where.Trim();

        string order = BetweenOptional(text, "ORDER BY", new[] { "LIMIT" });
        if (!string.IsNullOrWhiteSpace(order))
        {
            string[] parts = order.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            stmt.OrderByField = parts[0];
            if (parts.Length > 1)
                stmt.OrderByDir = parts[1].ToUpperInvariant();
        }

        int limitIdx = text.IndexOf("LIMIT", StringComparison.OrdinalIgnoreCase);

        if (limitIdx < 0)
            return stmt;

        string limStr = text[(limitIdx + 5)..].Trim();
        if (int.TryParse(limStr, out int lim))
            stmt.Limit = lim;

        return stmt;
    }

    private static string Between(string text, string start, string end)
    {
        int a = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (a < 0)
            return "";
        a += start.Length;

        int b = text.IndexOf(end, a, StringComparison.OrdinalIgnoreCase);

        if (b < 0)
            b = text.Length;

        return text[a..b];
    }

    private static string Between(string text, string start, string[] ends)
    {
        int a = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);

        if (a < 0)
            return "";
        a += start.Length;

        int b = text.Length;

        foreach (string e in ends)
        {
            int idx = text.IndexOf(e, a, StringComparison.OrdinalIgnoreCase);

            if (idx >= 0 && idx < b)
                b = idx;
        }

        return text[a..b];
    }

    private static string BetweenOptional(string text, string start, string[] ends)
        => text.Contains(start, StringComparison.OrdinalIgnoreCase)
            ? Between(text, start, ends)
            : "";
}
