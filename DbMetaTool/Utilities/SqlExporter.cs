using DbMetaTool.Models;

namespace DbMetaTool.Utilities;

public class SqlExporter
{
    private readonly string _outputDirectory;

    public SqlExporter(string outputDirectory)
    {
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    }

    public void ExportDomains(List<DomainMetadata> domains)
    {
        var domainsDir = Path.Combine(_outputDirectory, "domains");
        Directory.CreateDirectory(domainsDir);

        foreach (var domain in domains)
        {
            var fileName = $"{domain.Name}.sql";
            var filePath = Path.Combine(domainsDir, fileName);
            var sql = GenerateCreateDomainSql(domain);

            File.WriteAllText(filePath, sql);
        }
    }

    public void ExportTables(List<TableMetadata> tables)
    {
        var tablesDir = Path.Combine(_outputDirectory, "tables");
        Directory.CreateDirectory(tablesDir);

        foreach (var table in tables)
        {
            var fileName = $"{table.Name}.sql";
            var filePath = Path.Combine(tablesDir, fileName);
            var sql = GenerateCreateTableSql(table);

            File.WriteAllText(filePath, sql);
        }
    }

    public void ExportProcedures(List<ProcedureMetadata> procedures)
    {
        var proceduresDir = Path.Combine(_outputDirectory, "procedures");
        Directory.CreateDirectory(proceduresDir);

        foreach (var procedure in procedures)
        {
            var fileName = $"{procedure.Name}.sql";
            var filePath = Path.Combine(proceduresDir, fileName);
            var sql = GenerateCreateProcedureSql(procedure);

            File.WriteAllText(filePath, sql);
        }
    }

    private static string GenerateCreateDomainSql(DomainMetadata domain)
    {
        var sql = $"CREATE DOMAIN {domain.Name} AS {domain.DataType}";

        if (domain.DefaultValue != null)
        {
            sql += $" {domain.DefaultValue}";
        }

        if (!domain.IsNullable)
        {
            sql += " NOT NULL";
        }

        if (domain.CheckConstraint != null)
        {
            sql += $" {domain.CheckConstraint}";
        }

        sql += ";";

        return sql;
    }

    private static string GenerateCreateTableSql(TableMetadata table)
    {
        var lines = new List<string>
        {
            $"CREATE TABLE {table.Name}",
            "("
        };

        for (int i = 0; i < table.Columns.Count; i++)
        {
            var column = table.Columns[i];
            var columnDef = GenerateColumnDefinition(column);

            if (i < table.Columns.Count - 1)
            {
                columnDef += ",";
            }

            lines.Add($"    {columnDef}");
        }

        lines.Add(");");

        return string.Join(Environment.NewLine, lines);
    }

    private static string GenerateColumnDefinition(ColumnMetadata column)
    {
        string definition;

        if (column.DomainName != null)
        {
            definition = $"{column.Name} {column.DomainName}";
        }
        else
        {
            definition = $"{column.Name} {column.DataType}";
        }

        if (column.DefaultValue != null)
        {
            definition += $" {column.DefaultValue}";
        }

        if (!column.IsNullable)
        {
            definition += " NOT NULL";
        }

        return definition;
    }

    private static string GenerateCreateProcedureSql(ProcedureMetadata procedure)
    {
        var lines = new List<string>
        {
            $"CREATE PROCEDURE {procedure.Name}",
            procedure.Source.Trim(),
            "^"
        };

        return string.Join(Environment.NewLine, lines);
    }
}

