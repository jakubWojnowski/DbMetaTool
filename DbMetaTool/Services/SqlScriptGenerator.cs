using DbMetaTool.Models;
using System.Text;

namespace DbMetaTool.Services;

public static class SqlScriptGenerator
{
    public static string GenerateDomainScript(DomainMetadata domain)
    {
        var sb = new StringBuilder();

        sb.Append($"CREATE DOMAIN {domain.Name} AS {domain.DataType}");

        if (!domain.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrWhiteSpace(domain.DefaultValue))
        {
            sb.Append($" {domain.DefaultValue.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(domain.CheckConstraint))
        {
            sb.Append($" {domain.CheckConstraint.Trim()}");
        }

        sb.AppendLine(";");

        return sb.ToString();
    }

    public static string GenerateTableScript(TableMetadata table)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE {table.Name}");
        sb.AppendLine("(");

        var columnLines = new List<string>();

        foreach (var column in table.Columns.OrderBy(c => c.Position))
        {
            var columnDef = new StringBuilder();
            columnDef.Append($"    {column.Name} {column.DataType}");

            if (!column.IsNullable)
            {
                columnDef.Append(" NOT NULL");
            }

            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
            {
                columnDef.Append($" {column.DefaultValue.Trim()}");
            }

            columnLines.Add(columnDef.ToString());
        }

        sb.AppendLine(string.Join("," + Environment.NewLine, columnLines));
        sb.AppendLine(");");

        return sb.ToString();
    }

    public static string GenerateProcedureScript(ProcedureMetadata procedure)
    {
        var sb = new StringBuilder();

        sb.AppendLine("SET TERM ^;");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(procedure.Source))
        {
            sb.AppendLine(procedure.Source.Trim());
            sb.AppendLine("^");
        }
        else
        {
            sb.AppendLine($"-- Brak źródła dla procedury {procedure.Name}");
        }

        sb.AppendLine();
        sb.AppendLine("SET TERM ;^");

        return sb.ToString();
    }
}

