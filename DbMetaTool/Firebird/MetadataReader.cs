using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Models;

namespace DbMetaTool.Firebird;

public class MetadataReader(FirebirdConnectionFactory connectionFactory)
{
    public List<DomainMetadata> GetDomains()
    {
        var domains = new List<DomainMetadata>();

        using var connection = connectionFactory.CreateAndOpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT 
                TRIM(f.RDB$FIELD_NAME) AS FIELD_NAME,
                f.RDB$FIELD_TYPE AS FIELD_TYPE,
                f.RDB$FIELD_LENGTH AS FIELD_LENGTH,
                f.RDB$FIELD_PRECISION AS FIELD_PRECISION,
                f.RDB$FIELD_SCALE AS FIELD_SCALE,
                f.RDB$NULL_FLAG AS NULL_FLAG,
                f.RDB$DEFAULT_SOURCE AS DEFAULT_SOURCE,
                f.RDB$VALIDATION_SOURCE AS VALIDATION_SOURCE
            FROM RDB$FIELDS f
            WHERE f.RDB$FIELD_NAME NOT STARTING WITH 'RDB$'
              AND f.RDB$SYSTEM_FLAG = 0
            ORDER BY f.RDB$FIELD_NAME";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader["FIELD_NAME"]?.ToString()?.Trim() ?? string.Empty;
            var fieldType = reader["FIELD_TYPE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_TYPE"]) : 0;
            var length = reader["FIELD_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_LENGTH"]) : (int?)null;
            var precision = reader["FIELD_PRECISION"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_PRECISION"]) : (int?)null;
            var scale = reader["FIELD_SCALE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_SCALE"]) : (int?)null;
            var nullFlag = reader["NULL_FLAG"] != DBNull.Value ? Convert.ToInt32(reader["NULL_FLAG"]) : 0;
            var defaultSource = reader["DEFAULT_SOURCE"]?.ToString()?.Trim();
            var validationSource = reader["VALIDATION_SOURCE"]?.ToString()?.Trim();

            var dataType = MapFirebirdTypeToSql(fieldType, length, precision, scale);
            var isNullable = nullFlag == 0;

            domains.Add(new DomainMetadata(
                Name: name,
                DataType: dataType,
                Length: length,
                Precision: precision,
                Scale: scale,
                IsNullable: isNullable,
                DefaultValue: defaultSource,
                CheckConstraint: validationSource
            ));
        }

        return domains;
    }

    public List<TableMetadata> GetTables()
    {
        var tables = new List<TableMetadata>();

        using var connection = connectionFactory.CreateAndOpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT TRIM(r.RDB$RELATION_NAME) AS RELATION_NAME
            FROM RDB$RELATIONS r
            WHERE (r.RDB$VIEW_BLR IS NULL)
              AND (r.RDB$SYSTEM_FLAG IS NULL OR r.RDB$SYSTEM_FLAG = 0)
            ORDER BY r.RDB$RELATION_NAME";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var tableName = reader["RELATION_NAME"]?.ToString()?.Trim() ?? string.Empty;
            var columns = GetTableColumns(tableName);

            tables.Add(new TableMetadata(
                Name: tableName,
                Columns: columns
            ));
        }

        return tables;
    }

    public List<ColumnMetadata> GetTableColumns(string tableName)
    {
        var columns = new List<ColumnMetadata>();

        using var connection = connectionFactory.CreateAndOpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT 
                rf.RDB$FIELD_POSITION AS FIELD_POSITION,
                TRIM(rf.RDB$FIELD_NAME) AS FIELD_NAME,
                TRIM(rf.RDB$FIELD_SOURCE) AS FIELD_SOURCE,
                f.RDB$FIELD_TYPE AS FIELD_TYPE,
                f.RDB$FIELD_LENGTH AS FIELD_LENGTH,
                f.RDB$FIELD_PRECISION AS FIELD_PRECISION,
                f.RDB$FIELD_SCALE AS FIELD_SCALE,
                rf.RDB$NULL_FLAG AS NULL_FLAG,
                rf.RDB$DEFAULT_SOURCE AS DEFAULT_SOURCE
            FROM RDB$RELATION_FIELDS rf
            JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
            WHERE rf.RDB$RELATION_NAME = @TableName
            ORDER BY rf.RDB$FIELD_POSITION";

        command.Parameters.AddWithValue("@TableName", tableName);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var position = reader["FIELD_POSITION"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_POSITION"]) : 0;
            var fieldName = reader["FIELD_NAME"]?.ToString()?.Trim() ?? string.Empty;
            var fieldSource = reader["FIELD_SOURCE"]?.ToString()?.Trim();
            var fieldType = reader["FIELD_TYPE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_TYPE"]) : 0;
            var length = reader["FIELD_LENGTH"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_LENGTH"]) : (int?)null;
            var precision = reader["FIELD_PRECISION"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_PRECISION"]) : (int?)null;
            var scale = reader["FIELD_SCALE"] != DBNull.Value ? Convert.ToInt32(reader["FIELD_SCALE"]) : (int?)null;
            var nullFlag = reader["NULL_FLAG"] != DBNull.Value ? Convert.ToInt32(reader["NULL_FLAG"]) : 0;
            var defaultSource = reader["DEFAULT_SOURCE"]?.ToString()?.Trim();

            var dataType = MapFirebirdTypeToSql(fieldType, length, precision, scale);
            var isNullable = nullFlag == 0;
            var isDomain = fieldSource != null && !fieldSource.StartsWith("RDB$");

            columns.Add(new ColumnMetadata(
                Position: position,
                Name: fieldName,
                DataType: dataType,
                Length: length,
                Precision: precision,
                Scale: scale,
                IsNullable: isNullable,
                DefaultValue: defaultSource,
                DomainName: isDomain ? fieldSource : null
            ));
        }

        return columns;
    }

    public List<ProcedureMetadata> GetProcedures()
    {
        var procedures = new List<ProcedureMetadata>();

        using var connection = connectionFactory.CreateAndOpenConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT 
                TRIM(p.RDB$PROCEDURE_NAME) AS PROC_NAME,
                p.RDB$PROCEDURE_SOURCE AS PROC_SOURCE
            FROM RDB$PROCEDURES p
            WHERE (p.RDB$SYSTEM_FLAG IS NULL OR p.RDB$SYSTEM_FLAG = 0)
            ORDER BY p.RDB$PROCEDURE_NAME";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader["PROC_NAME"]?.ToString()?.Trim() ?? string.Empty;
            var source = reader["PROC_SOURCE"]?.ToString() ?? string.Empty;

            procedures.Add(new ProcedureMetadata(
                Name: name,
                Source: source
            ));
        }

        return procedures;
    }

    private static string MapFirebirdTypeToSql(int firebirdType, int? length, int? precision, int? scale)
    {
        return firebirdType switch
        {
            7 => scale switch
            {
                null or 0 => "SMALLINT",
                _ => $"NUMERIC({precision ?? 4},{Math.Abs(scale.Value)})"
            },
            8 => scale switch
            {
                null or 0 => "INTEGER",
                _ => $"NUMERIC({precision ?? 9},{Math.Abs(scale.Value)})"
            },
            10 => "FLOAT",
            12 => "DATE",
            13 => "TIME",
            14 => length.HasValue ? $"CHAR({length.Value})" : "CHAR",
            16 => scale switch
            {
                null or 0 => "BIGINT",
                _ => $"NUMERIC({precision ?? 18},{Math.Abs(scale.Value)})"
            },
            27 => "DOUBLE PRECISION",
            35 => "TIMESTAMP",
            37 => length.HasValue ? $"VARCHAR({length.Value})" : "VARCHAR",
            261 => "BLOB",
            _ => $"UNKNOWN_TYPE_{firebirdType}"
        };
    }
}

