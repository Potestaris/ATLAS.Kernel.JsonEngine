using System.Text.Json.Nodes;

namespace ATLAS.Kernel.JsonEngine.SQL.Interfaces;

/// <summary>
/// Defines the contract for executing SQL-like queries over JSON documents.
/// </summary>
/// <example>
/// Depend on this interface when composing services or tests:
/// <code>
/// List&lt;JsonSqlRow&gt; FindExpensiveItems(IJsonSqlEngine sqlEngine, JsonNode root)
/// {
///     return sqlEngine.Execute(root, "SELECT id FROM $.items[*] WHERE precio.valor &gt; 50");
/// }
/// </code>
/// </example>
public interface IJsonSqlEngine
{
    /// <summary>
    /// Executes a SQL-like query against a JSON document.
    /// </summary>
    /// <param name="root">The JSON document root.</param>
    /// <param name="sql">The SQL-like query text to execute.</param>
    /// <returns>A list of rows containing either projected fields or the full matching node under the <c>value</c> key when <c>SELECT *</c> is used.</returns>
    /// <example>
    /// <code>
    /// var rows = sqlEngine.Execute(root, "SELECT id, nombre FROM $.items[*] LIMIT 5");
    /// </code>
    /// </example>
    List<JsonSqlRow> Execute(JsonNode root, string sql);
}
