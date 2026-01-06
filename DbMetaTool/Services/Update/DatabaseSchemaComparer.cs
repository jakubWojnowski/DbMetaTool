using System.Text;
using DbMetaTool.Models;

namespace DbMetaTool.Services.Update;

public static class DatabaseSchemaComparer
{
    public static List<string> GenerateAlterStatements(
        TableMetadata existingTable,
        TableMetadata desiredTable,
        List<DomainMetadata>? existingDomains = null)
    {
        var statements = new List<string>();

        foreach (var desiredColumn in desiredTable.Columns)
        {
            var existingColumn = existingTable.Columns
                .FirstOrDefault(c => c.Name.Equals(desiredColumn.Name, StringComparison.OrdinalIgnoreCase));

            if (existingColumn == null)
            {
                var addColumnStatement = GenerateAddColumnStatement(existingTable.Name, desiredColumn);
                statements.Add(addColumnStatement);
            }
            else if (!AreColumnsEqual(existingColumn, desiredColumn, existingDomains))
            {
                var comment = $"-- MANUAL REVIEW REQUIRED: Column {desiredColumn.Name} exists but has different definition";
                statements.Add(comment);
            }
        }

        return statements;
    }

    private static string GenerateAddColumnStatement(string tableName, ColumnMetadata column)
    {
        var sb = new StringBuilder();
        sb.Append($"ALTER TABLE {tableName} ADD {column.Name} {column.DataType}");

        if (!column.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrWhiteSpace(column.DefaultValue))
        {
            sb.Append($" {column.DefaultValue.Trim()}");
        }

        return sb.ToString();
    }

    private static bool AreColumnsEqual(
        ColumnMetadata col1, 
        ColumnMetadata col2, 
        List<DomainMetadata>? existingDomains = null)
    {
        var isCol2Domain = IsDomainName(col2.DataType, existingDomains);
        
        if (!string.IsNullOrWhiteSpace(col1.DomainName) && isCol2Domain)
        {
            return col1.DomainName.Equals(col2.DataType, StringComparison.OrdinalIgnoreCase) &&
                   col1.IsNullable == col2.IsNullable;
        }
        
        var dataType1 = GetEffectiveDataType(col1, existingDomains);
        var dataType2 = isCol2Domain 
            ? GetEffectiveDataTypeFromDomain(col2.DataType, existingDomains)
            : NormalizeDataType(col2.DataType);
        
        if (!dataType1.Equals(dataType2, StringComparison.OrdinalIgnoreCase) ||
            col1.IsNullable != col2.IsNullable)
        {
            return false;
        }
        
        if (IsNumericTypeWithParameters(dataType1) && IsNumericTypeWithParameters(dataType2))
        {
            var precision1 = GetEffectivePrecision(col1, existingDomains);
            var precision2 = isCol2Domain 
                ? GetEffectivePrecisionFromDomain(col2.DataType, existingDomains)
                : col2.Precision;
            var scale1 = GetEffectiveScale(col1, existingDomains);
            var scale2 = isCol2Domain 
                ? GetEffectiveScaleFromDomain(col2.DataType, existingDomains)
                : col2.Scale;
            
            return precision1 == precision2 &&
                   scale1 == scale2;
        }
        
        if (IsTextType(dataType1) && IsTextType(dataType2))
        {
            var length1 = GetEffectiveLength(col1, existingDomains);
            var length2 = isCol2Domain 
                ? GetEffectiveLengthFromDomain(col2.DataType, existingDomains)
                : col2.Length;
            
            if (length1.HasValue && length2.HasValue && length1.Value < length2.Value)
            {
                return false;
            }
        }
        
        return true;
    }

    private static bool IsDomainName(string dataType, List<DomainMetadata>? existingDomains)
    {
        return FindDomain(dataType, existingDomains) != null;
    }

    private static DomainMetadata? FindDomain(string? domainName, List<DomainMetadata>? existingDomains)
    {
        if (existingDomains == null || string.IsNullOrWhiteSpace(domainName))
            return null;
            
        return existingDomains.FirstOrDefault(d => 
            d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetEffectiveDataTypeFromDomain(string domainName, List<DomainMetadata>? existingDomains)
    {
        var domain = FindDomain(domainName, existingDomains);
        return domain != null 
            ? NormalizeDataType(domain.DataType) 
            : NormalizeDataType(domainName);
    }

    private static int? GetEffectivePrecisionFromDomain(string domainName, List<DomainMetadata>? existingDomains)
    {
        return FindDomain(domainName, existingDomains)?.Precision;
    }

    private static int? GetEffectiveScaleFromDomain(string domainName, List<DomainMetadata>? existingDomains)
    {
        return FindDomain(domainName, existingDomains)?.Scale;
    }

    private static int? GetEffectiveLengthFromDomain(string domainName, List<DomainMetadata>? existingDomains)
    {
        return FindDomain(domainName, existingDomains)?.Length;
    }

    private static string GetEffectiveDataType(ColumnMetadata column, List<DomainMetadata>? existingDomains)
    {
        var domain = FindDomain(column.DomainName, existingDomains);
        return domain != null 
            ? NormalizeDataType(domain.DataType) 
            : NormalizeDataType(column.DataType);
    }

    private static int? GetEffectivePrecision(ColumnMetadata column, List<DomainMetadata>? existingDomains)
    {
        var domain = FindDomain(column.DomainName, existingDomains);
        return domain?.Precision ?? column.Precision;
    }

    private static int? GetEffectiveScale(ColumnMetadata column, List<DomainMetadata>? existingDomains)
    {
        var domain = FindDomain(column.DomainName, existingDomains);
        return domain?.Scale ?? column.Scale;
    }

    private static int? GetEffectiveLength(ColumnMetadata column, List<DomainMetadata>? existingDomains)
    {
        var domain = FindDomain(column.DomainName, existingDomains);
        return domain?.Length ?? column.Length;
    }

    private static bool IsNumericTypeWithParameters(string dataType)
    {
        return dataType.StartsWith("DECIMAL", StringComparison.OrdinalIgnoreCase) ||
               dataType.StartsWith("NUMERIC", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextType(string dataType)
    {
        return dataType.StartsWith("VARCHAR", StringComparison.OrdinalIgnoreCase) ||
               dataType.StartsWith("CHAR", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDataType(string dataType)
    {
        var normalized = dataType.Replace(" ", "").ToUpperInvariant();
        
        if (normalized.StartsWith("NUMERIC"))
        {
            normalized = normalized.Replace("NUMERIC", "DECIMAL");
        }
        
        return normalized;
    }

}

