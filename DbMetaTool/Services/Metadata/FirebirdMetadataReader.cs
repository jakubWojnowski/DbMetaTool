using System.Text;
using DbMetaTool.Models;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Services.Metadata;

public static class FirebirdMetadataReader
{
    private const string SystemPrefixRdb = "RDB$";

    public static List<DomainMetadata> ReadDomains(ISqlExecutor executor)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT");
        sql.AppendLine("    RDB$FIELD_NAME AS DOMAIN_NAME,");
        sql.AppendLine("    RDB$FIELD_TYPE AS FIELD_TYPE,");
        sql.AppendLine("    RDB$FIELD_SUB_TYPE AS FIELD_SUBTYPE,");
        sql.AppendLine("    RDB$FIELD_LENGTH AS FIELD_LENGTH,");
        sql.AppendLine("    RDB$FIELD_PRECISION AS FIELD_PRECISION,");
        sql.AppendLine("    RDB$FIELD_SCALE AS FIELD_SCALE,");
        sql.AppendLine("    RDB$CHARACTER_SET_ID AS CHARACTER_SET_ID,");
        sql.AppendLine("    RDB$NULL_FLAG AS NULL_FLAG,");
        sql.AppendLine("    RDB$DEFAULT_SOURCE AS DEFAULT_SOURCE,");
        sql.AppendLine("    RDB$VALIDATION_SOURCE AS CHECK_CONSTRAINT");
        sql.AppendLine("FROM RDB$FIELDS");
        sql.AppendLine("WHERE RDB$FIELD_NAME NOT STARTING WITH 'RDB$'");
        sql.AppendLine("  AND RDB$FIELD_NAME NOT STARTING WITH 'MON$'");
        sql.AppendLine("  AND RDB$FIELD_NAME NOT STARTING WITH 'SEC$'");
        sql.AppendLine("ORDER BY RDB$FIELD_NAME");

        return executor.ExecuteRead(sql.ToString(), reader =>
        {
            var fieldType = (FirebirdFieldType)Convert.ToInt32(reader["FIELD_TYPE"]);
            
            var fieldSubType = reader["FIELD_SUBTYPE"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_SUBTYPE"]);
            
            var fieldLength = reader["FIELD_LENGTH"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_LENGTH"]);
            
            var fieldPrecision = reader["FIELD_PRECISION"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_PRECISION"]);
            
            var fieldScale = reader["FIELD_SCALE"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_SCALE"]);
            
            var characterSetId = reader["CHARACTER_SET_ID"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["CHARACTER_SET_ID"]);

            var charLength = CalculateCharacterLength(fieldType, fieldLength, characterSetId);
            
            var dataType = MapFirebirdTypeToString(
                fieldType,
                fieldSubType,
                fieldPrecision,
                fieldScale,
                charLength);

            return new DomainMetadata(
                Name: reader["DOMAIN_NAME"].ToString()!.Trim(),
                DataType: dataType,
                Length: charLength ?? fieldLength,
                Precision: fieldPrecision,
                Scale: CalculateAbsoluteScale(fieldScale),
                IsNullable: IsFieldNullable(reader["NULL_FLAG"]),
                DefaultValue: GetTrimmedStringOrNull(reader["DEFAULT_SOURCE"]),
                CheckConstraint: GetTrimmedStringOrNull(reader["CHECK_CONSTRAINT"])
            );
        });
    }

    public static List<TableMetadata> ReadTables(ISqlExecutor executor)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT");
        sql.AppendLine("    RDB$RELATION_NAME AS TABLE_NAME");
        sql.AppendLine("FROM RDB$RELATIONS");
        sql.AppendLine("WHERE RDB$VIEW_BLR IS NULL");
        sql.AppendLine("  AND RDB$SYSTEM_FLAG = 0");
        sql.AppendLine("  AND RDB$RELATION_NAME NOT STARTING WITH 'MON$'");
        sql.AppendLine("  AND RDB$RELATION_NAME NOT STARTING WITH 'SEC$'");
        sql.AppendLine("ORDER BY RDB$RELATION_NAME");

        var tables = executor.ExecuteRead(sql.ToString(), reader => reader["TABLE_NAME"].ToString()!.Trim());

        var result = new List<TableMetadata>();

        foreach (var tableName in tables)
        {
            var columns = ReadTableColumns(executor, tableName);
            result.Add(new TableMetadata(
                Name: tableName,
                Columns: columns
            ));
        }

        return result;
    }

    private static List<ColumnMetadata> ReadTableColumns(ISqlExecutor executor, string tableName)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT");
        sql.AppendLine("    rf.RDB$FIELD_NAME AS COLUMN_NAME,");
        sql.AppendLine("    rf.RDB$FIELD_SOURCE AS FIELD_SOURCE,");
        sql.AppendLine("    f.RDB$FIELD_TYPE AS FIELD_TYPE,");
        sql.AppendLine("    f.RDB$FIELD_SUB_TYPE AS FIELD_SUBTYPE,");
        sql.AppendLine("    f.RDB$FIELD_LENGTH AS FIELD_LENGTH,");
        sql.AppendLine("    f.RDB$FIELD_PRECISION AS FIELD_PRECISION,");
        sql.AppendLine("    f.RDB$FIELD_SCALE AS FIELD_SCALE,");
        sql.AppendLine("    f.RDB$CHARACTER_SET_ID AS CHARACTER_SET_ID,");
        sql.AppendLine("    rf.RDB$NULL_FLAG AS NULL_FLAG,");
        sql.AppendLine("    rf.RDB$DEFAULT_SOURCE AS DEFAULT_SOURCE,");
        sql.AppendLine("    rf.RDB$FIELD_POSITION AS FIELD_POSITION");
        sql.AppendLine("FROM RDB$RELATION_FIELDS rf");
        sql.AppendLine("JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME");
        sql.AppendLine($"WHERE rf.RDB$RELATION_NAME = '{tableName}'");
        sql.AppendLine("ORDER BY rf.RDB$FIELD_POSITION");

        return executor.ExecuteRead(sql.ToString(), reader =>
        {
            var fieldSource = reader["FIELD_SOURCE"].ToString()!.Trim();
            
            var fieldType = (FirebirdFieldType)Convert.ToInt32(reader["FIELD_TYPE"]);
            
            var fieldSubType = reader["FIELD_SUBTYPE"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_SUBTYPE"]);
            
            var fieldLength = reader["FIELD_LENGTH"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_LENGTH"]);
            
            var fieldPrecision = reader["FIELD_PRECISION"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_PRECISION"]);
            
            var fieldScale = reader["FIELD_SCALE"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_SCALE"]);
            
            var characterSetId = reader["CHARACTER_SET_ID"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["CHARACTER_SET_ID"]);

            var charLength = CalculateCharacterLength(fieldType, fieldLength, characterSetId);

            string dataType;
            
            string? domainName = null;

            if (fieldSource.StartsWith(SystemPrefixRdb))
            {
                dataType = MapFirebirdTypeToString(
                    fieldType,
                    fieldSubType,
                    fieldPrecision,
                    fieldScale,
                    charLength);
            }
            else
            {
                dataType = fieldSource;
                domainName = fieldSource;
            }

            return new ColumnMetadata(
                Position: Convert.ToInt32(reader["FIELD_POSITION"]),
                Name: reader["COLUMN_NAME"].ToString()!.Trim(),
                DataType: dataType,
                Length: charLength ?? fieldLength,
                Precision: fieldPrecision,
                Scale: CalculateAbsoluteScale(fieldScale),
                IsNullable: IsFieldNullable(reader["NULL_FLAG"]),
                DefaultValue: GetTrimmedStringOrNull(reader["DEFAULT_SOURCE"]),
                DomainName: domainName
            );
        });
    }

    public static List<ProcedureMetadata> ReadProcedures(ISqlExecutor executor)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT");
        sql.AppendLine("    RDB$PROCEDURE_NAME AS PROCEDURE_NAME,");
        sql.AppendLine("    RDB$PROCEDURE_SOURCE AS PROCEDURE_SOURCE");
        sql.AppendLine("FROM RDB$PROCEDURES");
        sql.AppendLine("WHERE RDB$SYSTEM_FLAG = 0");
        sql.AppendLine("  AND RDB$PROCEDURE_NAME NOT STARTING WITH 'MON$'");
        sql.AppendLine("  AND RDB$PROCEDURE_NAME NOT STARTING WITH 'SEC$'");
        sql.AppendLine("ORDER BY RDB$PROCEDURE_NAME");

        return executor.ExecuteRead(sql.ToString(), reader =>
        {
            var procedureName = reader["PROCEDURE_NAME"].ToString()!.Trim();
            var source = reader["PROCEDURE_SOURCE"] == DBNull.Value
                ? null
                : reader["PROCEDURE_SOURCE"].ToString();

            var fullSource = BuildFullProcedureDefinition(executor, procedureName, source);

            return new ProcedureMetadata(
                Name: procedureName,
                Source: fullSource);
        });
    }

    private static string? BuildFullProcedureDefinition(ISqlExecutor executor, string procedureName, string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var inputParams = ReadProcedureParameters(executor, procedureName, isInput: true);
        
        var outputParams = ReadProcedureParameters(executor, procedureName, isInput: false);

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE PROCEDURE {procedureName}");

        if (inputParams.Count > 0)
        {
            sb.AppendLine("(");
            
            var paramLines = inputParams.Select(p => $"    {p.Name} {p.DataType}");
            
            sb.AppendLine(string.Join("," + Environment.NewLine, paramLines));
            
            sb.AppendLine(")");
        }

        if (outputParams.Count > 0)
        {
            sb.AppendLine("RETURNS");
            
            sb.AppendLine("(");
            
            var paramLines = outputParams.Select(p => $"    {p.Name} {p.DataType}");
            
            sb.AppendLine(string.Join("," + Environment.NewLine, paramLines));
            
            sb.AppendLine(")");
        }

        sb.AppendLine("AS");
        
        sb.AppendLine(body.Trim());

        return sb.ToString();
    }

    private static List<(string Name, string DataType)> ReadProcedureParameters(
        ISqlExecutor executor,
        string procedureName,
        bool isInput)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT");
        sql.AppendLine("    pp.RDB$PARAMETER_NAME AS PARAM_NAME,");
        sql.AppendLine("    f.RDB$FIELD_TYPE AS FIELD_TYPE,");
        sql.AppendLine("    f.RDB$FIELD_SUB_TYPE AS FIELD_SUBTYPE,");
        sql.AppendLine("    f.RDB$FIELD_LENGTH AS FIELD_LENGTH,");
        sql.AppendLine("    f.RDB$FIELD_PRECISION AS FIELD_PRECISION,");
        sql.AppendLine("    f.RDB$FIELD_SCALE AS FIELD_SCALE,");
        sql.AppendLine("    f.RDB$CHARACTER_SET_ID AS CHARACTER_SET_ID,");
        sql.AppendLine("    pp.RDB$PARAMETER_NUMBER AS PARAM_NUMBER");
        sql.AppendLine("FROM RDB$PROCEDURE_PARAMETERS pp");
        sql.AppendLine("JOIN RDB$FIELDS f ON pp.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME");
        sql.AppendLine($"WHERE pp.RDB$PROCEDURE_NAME = '{procedureName}'");
        sql.AppendLine($"  AND pp.RDB$PARAMETER_TYPE = {(isInput ? 0 : 1)}");
        sql.AppendLine("ORDER BY pp.RDB$PARAMETER_NUMBER");

        return executor.ExecuteRead(sql.ToString(), reader =>
        {
            var fieldType = (FirebirdFieldType)Convert.ToInt32(reader["FIELD_TYPE"]);
            var fieldSubType = reader["FIELD_SUBTYPE"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_SUBTYPE"]);
            
            var fieldLength = reader["FIELD_LENGTH"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_LENGTH"]);
            
            var fieldPrecision = reader["FIELD_PRECISION"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_PRECISION"]);
            
            var fieldScale = reader["FIELD_SCALE"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["FIELD_SCALE"]);
            
            var characterSetId = reader["CHARACTER_SET_ID"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(reader["CHARACTER_SET_ID"]);

            var charLength = CalculateCharacterLength(fieldType, fieldLength, characterSetId);
            var dataType = MapFirebirdTypeToString(
                fieldType,
                fieldSubType,
                fieldPrecision,
                fieldScale,
                charLength);

            return (
                Name: reader["PARAM_NAME"].ToString()!.Trim(),
                DataType: dataType
            );
        });
    }

    private static int? CalculateCharacterLength(FirebirdFieldType fieldType, int? fieldLength, int? characterSetId)
    {
        if (fieldType is FirebirdFieldType.Char or FirebirdFieldType.VarChar)
        {
            if (!fieldLength.HasValue)
            {
                return null;
            }
            
            if (characterSetId == 4)
            {

                if (fieldLength.Value % 4 == 0 && fieldLength.Value > 0)
                {
                    return fieldLength.Value / 4;
                }
                
                return fieldLength;
            }
            
            if (characterSetId == 3)
            {
                if (fieldLength.Value % 3 == 0 && fieldLength.Value > 0)
                {
                    return fieldLength.Value / 3;
                }
                return fieldLength;
            }
            
            return fieldLength;
        }

        return null;
    }

    private static int? CalculateAbsoluteScale(int? fieldScale)
    {
        if (fieldScale is < 0)
        {
            return Math.Abs(fieldScale.Value);
        }

        return null;
    }

    private static bool IsFieldNullable(object nullFlagValue)
    {
        if (nullFlagValue == DBNull.Value)
        {
            return true;
        }

        return Convert.ToInt32(nullFlagValue) == 0;
    }

    private static string? GetTrimmedStringOrNull(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }

        return value.ToString()!.Trim();
    }

    private static string MapFirebirdTypeToString(
        FirebirdFieldType fieldType,
        int? fieldSubType,
        int? fieldPrecision,
        int? fieldScale,
        int? charLength)
    {
        var hasNegativeScale = fieldScale is < 0;
        var absoluteScale = hasNegativeScale ? Math.Abs(fieldScale!.Value) : 0;

        return fieldType switch
        {
            FirebirdFieldType.SmallInt => BuildNumericType(
                "SMALLINT",
                "DECIMAL",
                hasNegativeScale,
                fieldPrecision ?? 9,
                absoluteScale),
            FirebirdFieldType.Integer => BuildNumericType(
                "INTEGER",
                "DECIMAL",
                hasNegativeScale,
                fieldPrecision ?? 18,
                absoluteScale),
            FirebirdFieldType.BigInt => BuildNumericType(
                "BIGINT",
                "NUMERIC",
                hasNegativeScale,
                fieldPrecision ?? 18,
                absoluteScale),
            FirebirdFieldType.Float => "FLOAT",
            FirebirdFieldType.DoublePrecision => "DOUBLE PRECISION",
            FirebirdFieldType.Date => "DATE",
            FirebirdFieldType.Time => "TIME",
            FirebirdFieldType.Timestamp => "TIMESTAMP",
            FirebirdFieldType.Char => BuildCharType("CHAR", charLength),
            FirebirdFieldType.VarChar => BuildCharType("VARCHAR", charLength),
            FirebirdFieldType.Blob => BuildBlobType(fieldSubType),
            _ => $"UNKNOWN_TYPE_{(int)fieldType}"
        };
    }

    private static string BuildNumericType(string baseType, string decimalType, bool hasScale, int precision, int scale)
    {
        if (hasScale)
        {
            var sb = new StringBuilder();
            sb.Append(decimalType);
            sb.Append('(');
            sb.Append(precision);
            sb.Append(", ");
            sb.Append(scale);
            sb.Append(')');
            return sb.ToString();
        }

        return baseType;
    }

    private static string BuildCharType(string baseType, int? length)
    {
        if (length.HasValue)
        {
            var sb = new StringBuilder();
            
            sb.Append(baseType);
            sb.Append('(');
            sb.Append(length.Value);
            sb.Append(')');
            
            return sb.ToString();
        }

        return baseType;
    }

    private static string BuildBlobType(int? subType)
    {
        if (subType == (int)FirebirdBlobSubType.Text)
        {
            return "BLOB SUB_TYPE TEXT";
        }

        return "BLOB";
    }
}
