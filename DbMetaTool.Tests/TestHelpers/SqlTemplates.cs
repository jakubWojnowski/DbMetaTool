using System.Text;

namespace DbMetaTool.Tests.TestHelpers;

public static class SqlTemplates
{
    public static string CreateDomain(string domainName, string dataType)
    {
        var sb = new StringBuilder();
        
        sb.Append("CREATE DOMAIN ");
        sb.Append(domainName);
        sb.Append(" AS ");
        sb.Append(dataType);
        sb.Append(';');
        
        return sb.ToString();
    }

    public static string CreateSimpleTable(string tableName, params string[] columnDefinitions)
    {
        var sb = new StringBuilder();
        sb.Append("CREATE TABLE ");
        sb.AppendLine(tableName);
        sb.AppendLine("(");
        
        for (var i = 0; i < columnDefinitions.Length; i++)
        {
            sb.Append("    ");
            sb.Append(columnDefinitions[i]);
            
            if (i < columnDefinitions.Length - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        
        sb.Append(");");
        
        return sb.ToString();
    }

    public static string CreateTableColumnsOnly(params string[] columnDefinitions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("(");
        
        for (var i = 0; i < columnDefinitions.Length; i++)
        {
            sb.Append("    ");
            sb.Append(columnDefinitions[i]);
            
            if (i < columnDefinitions.Length - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }
        
        sb.Append(");");
        
        return sb.ToString();
    }

    public static string CreateSimpleProcedure(string procedureName, string body = "BEGIN END")
    {
        var sb = new StringBuilder();
        
        sb.Append("CREATE PROCEDURE ");
        sb.Append(procedureName);
        sb.Append(" AS ");
        sb.Append(body);
        
        return sb.ToString();
    }

    public static string CreateFirebirdProcedure(
        string procedureName,
        string parameters,
        string returns,
        string body)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("SET TERM ^ ;");
        sb.Append("CREATE OR ALTER PROCEDURE ");
        sb.AppendLine(procedureName);
        
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            sb.AppendLine(parameters);
        }
        
        if (!string.IsNullOrWhiteSpace(returns))
        {
            sb.AppendLine(returns);
        }
        
        sb.AppendLine("AS");
        sb.AppendLine("BEGIN");
        sb.Append("    ");
        sb.AppendLine(body);
        sb.AppendLine("END^");
        sb.Append("SET TERM ; ^");
        
        return sb.ToString();
    }
}

