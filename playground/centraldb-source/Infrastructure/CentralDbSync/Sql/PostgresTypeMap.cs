using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace Infrastructure.CentralDbSync.Sql;

/// <summary>
/// Single source of truth for the PostgreSQL target types the sync engine supports.
/// Owns both the DDL rendering and the binary COPY write of each type so the two
/// can never describe the same target type differently.
/// </summary>
public static class PostgresTypeMap
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <param name="DdlType">Column type rendered into CREATE TABLE.</param>
    /// <param name="WriteAsync">
    /// Writes one non-null value into a binary COPY stream. The COPY protocol carries no
    /// column type information, so Npgsql infers the wire format from the CLR type it is
    /// handed: a SQL Server <c>float</c> reaching a <c>numeric</c> column would emit float8
    /// bytes that the server rejects with 22P03. Every entry therefore converts to the CLR
    /// type the column expects and states the PostgreSQL type explicitly.
    /// </param>
    private sealed record PostgresType(
        string DdlType,
        Func<NpgsqlBinaryImporter, object, CancellationToken, Task> WriteAsync);

    private static readonly Dictionary<string, PostgresType> Types = new(StringComparer.Ordinal)
    {
        ["text"] = new("TEXT",
            (writer, value, ct) => writer.WriteAsync(
                value as string ?? Convert.ToString(value, Culture)!, NpgsqlDbType.Text, ct)),

        ["integer"] = new("INTEGER",
            (writer, value, ct) => writer.WriteAsync(
                Convert.ToInt32(value, Culture), NpgsqlDbType.Integer, ct)),

        ["bigint"] = new("BIGINT",
            (writer, value, ct) => writer.WriteAsync(
                Convert.ToInt64(value, Culture), NpgsqlDbType.Bigint, ct)),

        ["boolean"] = new("BOOLEAN",
            (writer, value, ct) => writer.WriteAsync(
                Convert.ToBoolean(value, Culture), NpgsqlDbType.Boolean, ct)),

        ["numeric"] = new("NUMERIC",
            (writer, value, ct) => writer.WriteAsync(
                Convert.ToDecimal(value, Culture), NpgsqlDbType.Numeric, ct)),

        ["timestamp"] = new("TIMESTAMP WITHOUT TIME ZONE",
            (writer, value, ct) => writer.WriteAsync(
                DateTime.SpecifyKind(Convert.ToDateTime(value, Culture), DateTimeKind.Unspecified),
                NpgsqlDbType.Timestamp, ct)),

        ["timestamptz"] = new("TIMESTAMPTZ",
            (writer, value, ct) => writer.WriteAsync(
                DateTime.SpecifyKind(Convert.ToDateTime(value, Culture), DateTimeKind.Utc),
                NpgsqlDbType.TimestampTz, ct)),

        ["date"] = new("DATE",
            (writer, value, ct) => writer.WriteAsync(
                DateOnly.FromDateTime(Convert.ToDateTime(value, Culture)), NpgsqlDbType.Date, ct))
    };

    public static void EnsureSupported(string targetType) => Resolve(targetType);

    /// <summary>Returns the column type to render in CREATE TABLE for the given target type.</summary>
    public static string ToDdlType(string targetType) => Resolve(targetType).DdlType;

    /// <summary>Writes a non-null value into a binary COPY stream using the declared target type.</summary>
    public static Task WriteCopyValueAsync(
        NpgsqlBinaryImporter writer, string targetType, object value, CancellationToken ct)
        => Resolve(targetType).WriteAsync(writer, value, ct);

    private static PostgresType Resolve(string targetType)
        => Types.TryGetValue(targetType, out var type)
            ? type
            : throw new InvalidOperationException(
                $"PostgreSQL target type '{targetType}' is not supported by the sync engine.");
}
