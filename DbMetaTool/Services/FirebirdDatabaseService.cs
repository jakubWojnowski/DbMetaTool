using System.Text;
using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Exceptions;
using DbMetaTool.Utilities;

namespace DbMetaTool.Services;

public class FirebirdDatabaseService
{
    public void CreateDatabase(string connectionString)
    {
        try
        {
            FbConnection.CreateDatabase(connectionString, pageSize: 8192, forcedWrites: true, overwrite: true);
        }
        catch (FbException ex)
        {
            throw new DatabaseException($"Błąd podczas tworzenia bazy danych: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new DatabaseException($"Nieoczekiwany błąd podczas tworzenia bazy danych: {ex.Message}", ex);
        }
    }

    public void ExecuteScript(string connectionString, string sqlScript)
    {
        if (string.IsNullOrWhiteSpace(sqlScript))
            return;

        try
        {
            using var connection = new FbConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                var commands = SplitSqlScript(sqlScript);

                foreach (var command in commands)
                {
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    using var cmd = new FbCommand(command, connection, transaction);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (FbException ex)
        {
            throw new DatabaseException($"Błąd wykonania skryptu SQL: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new DatabaseException($"Nieoczekiwany błąd podczas wykonywania skryptu: {ex.Message}", ex);
        }
    }

    public void ExecuteScripts(string connectionString, IEnumerable<string> sqlScripts)
    {
        foreach (var script in sqlScripts)
        {
            ExecuteScript(connectionString, script);
        }
    }

    public bool TestConnection(string connectionString)
    {
        try
        {
            using var connection = new FbConnection(connectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> SplitSqlScript(string sqlScript)
    {
        var commands = new List<string>();
        var currentCommand = new StringBuilder();
        var inString = false;
        var stringChar = '\0';
        var inComment = false;
        var commentType = CommentType.None;

        for (int i = 0; i < sqlScript.Length; i++)
        {
            var current = sqlScript[i];
            var next = i + 1 < sqlScript.Length ? sqlScript[i + 1] : '\0';

            if (!inString)
            {
                if (current == '-' && next == '-' && !inComment)
                {
                    commentType = CommentType.SingleLine;
                    inComment = true;
                    currentCommand.Append(current);
                    continue;
                }
                else if (current == '/' && next == '*' && !inComment)
                {
                    commentType = CommentType.MultiLine;
                    inComment = true;
                    currentCommand.Append(current);
                    continue;
                }
                else if (inComment)
                {
                    currentCommand.Append(current);
                    
                    if (commentType == CommentType.SingleLine && (current == '\n' || current == '\r'))
                    {
                        inComment = false;
                        commentType = CommentType.None;
                    }
                    else if (commentType == CommentType.MultiLine && current == '*' && next == '/')
                    {
                        currentCommand.Append(next);
                        i++;
                        inComment = false;
                        commentType = CommentType.None;
                    }
                    continue;
                }
            }

            if ((current == '\'' || current == '"') && !inComment)
            {
                if (!inString)
                {
                    inString = true;
                    stringChar = current;
                }
                else if (current == stringChar)
                {
                    if (i > 0 && sqlScript[i - 1] != '\\')
                    {
                        inString = false;
                        stringChar = '\0';
                    }
                }
                currentCommand.Append(current);
                continue;
            }

            if (!inString && !inComment && current == ';')
            {
                var command = currentCommand.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(command))
                {
                    commands.Add(command);
                }
                currentCommand.Clear();
                continue;
            }

            currentCommand.Append(current);
        }

        var lastCommand = currentCommand.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastCommand))
        {
            commands.Add(lastCommand);
        }

        return commands;
    }

    private enum CommentType
    {
        None,
        SingleLine,
        MultiLine
    }
}

