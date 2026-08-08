namespace Application.Features.CentralDbSync.Mapping;

/// <summary>
/// Describes the structure and shape of a source table (or logical entity)
/// in SQL Server that is being synced to PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// <c>SourceSpec</c> is the "left side" of a <c>TableMappingRule</c> —
/// it defines how the SQL builder reads data from the ERP SQL Server database.
/// Every field directly influences the generated SQL SELECT statement.
/// </para>
/// <para>
/// <b>Usage flow:</b>
/// <list type="number">
/// <item><c>SqlServerSqlBuilder</c> reads <c>SourceSpec</c> to generate
/// bootstrap SELECT and ChangeTracking CHANGETABLE queries.</item>
/// <item>PostgresGenericApplier reads <c>ActivePredicate</c> to
/// classify each row as active/inactive before upserting to the target.</item>
/// </list>
/// </para>
/// </remarks>
public sealed record SourceSpec
{
    /// <summary>
    /// The SQL Server table name (schema-qualified logical name, e.g. "MerchStyleTrimSwatch").
    /// The SQL builder maps this to <c>[dbo].[MerchStyleTrimSwatch]</c>.
    /// This is the table that appears in the <c>FROM</c> clause of bootstrap SELECT
    /// and in the <c>CHANGETABLE(CHANGES ...)</c> function for incremental sync.
    /// </summary>
    public required string PrimaryTable { get; init; }

    /// <summary>
    /// SQL alias for the primary table in generated queries.
    /// Defaults to <c>"t0"</c>. Used as the table prefix in column references
    /// (e.g. <c>[t0].[Id]</c>) and as the correlation name in predicate WHERE clauses.
    /// </summary>
    public string PrimaryAlias { get; init; } = "t0";

    /// <summary>
    /// Optional JOIN clauses to attach related lookup tables.
    /// Each <c>JoinSpec</c> describes a table, its alias, join kind
    /// (INNER or LEFT), and the ON condition. When populated, the SQL builder
    /// appends these after the FROM clause to enrich the row set with related data.
    /// </summary>
    public IReadOnlyList<JoinSpec> Joins { get; init; } = [];

    /// <summary>
    /// Static row-level filter applied to every read query (both bootstrap and
    /// incremental). Each <see cref="ColumnPredicate"/> is turned into a
    /// parameterized WHERE condition (e.g. <c>CompanyId = @readFilter_P0</c>).
    /// Used to scope data to a specific tenant/company or exclude logically
    /// deleted rows.
    /// </summary>
    public IReadOnlyList<ColumnPredicate> ReadFilter { get; init; } = [];

    /// <summary>
    /// Optional developer-authored SQL predicate appended to the generated WHERE clause.
    /// Use only for fixed sync rules that cannot be represented by ColumnPredicate,
    /// such as an EXISTS filter against another source table.
    /// </summary>
    public string? ReadFilterSql { get; init; }

    /// <summary>
    /// Predicates that determine whether a source row should be considered
    /// "active" on the target side. Evaluated by the applier during upsert:
    /// <list type="bullet">
    /// <item>All predicates must be satisfied (AND semantics).</item>
    /// <item>An empty list means every row is active by default.</item>
    /// <item>When the rule has an ActiveFlag column, a row that fails this check
    /// is soft-deactivated on the target.</item>
    /// <item>When the rule has no ActiveFlag column, a row that fails this check
    /// is hard-deleted from the target by primary key.</item>
    /// </list>
    /// </summary>
    public IReadOnlyList<ColumnPredicate> ActivePredicate { get; init; } = [];

    /// <summary>
    /// The set of source columns that uniquely identify a row.
    /// <list type="bullet">
    /// <item>Used as the join key between CHANGETABLE and the base table
    /// in incremental SELECT queries.</item>
    /// <item>Used in the ORDER BY clause (appended after <c>SYS_CHANGE_VERSION</c>)
    /// to guarantee deterministic ordering across sync runs.</item>
    /// <item>Must match the target-side primary key columns (via <see cref="ColumnMapping.IsPrimaryKey"/>).</item>
    /// </list>
    /// </summary>
    public required IReadOnlyList<string> PrimaryKey { get; init; }
}
