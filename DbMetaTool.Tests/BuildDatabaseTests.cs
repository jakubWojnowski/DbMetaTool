using DbMetaTool.Databases;
using DbMetaTool.Features.Commands.BuildDatabase;
using DbMetaTool.Models;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Tests.TestHelpers;
using DbMetaTool.Utilities;
using NSubstitute;

namespace DbMetaTool.Tests;

[TestFixture]
public class BuildDatabaseTests
{
    private TestDirectoryHelper _directoryHelper = null!;
    private string _databaseDirectory = null!;
    private string _scriptsDirectory = null!;
    private ISqlExecutor _mockSqlExecutor = null!;

    [SetUp]
    public void SetUp()
    {
        _directoryHelper = new TestDirectoryHelper();
        _databaseDirectory = _directoryHelper.CreateDatabaseDirectory();
        _scriptsDirectory = _directoryHelper.CreateScriptsDirectory();
        _mockSqlExecutor = Substitute.For<ISqlExecutor>();
        FirebirdDatabaseCreatorStub.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        FirebirdDatabaseCreatorStub.Reset();
        _directoryHelper?.Dispose();
    }

    [Test]
    public void BuildDatabase_WithValidScripts_LoadsAndProcessesAllScripts()
    {
        // Arrange
        var databaseName = "TestDatabase";
        var (_, dbPath) = DatabasePathHelper.BuildDatabasePaths(
            Path.Combine(_databaseDirectory, databaseName));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "domains",
            "D_EMAIL.sql",
            SqlTemplates.CreateDomain("D_EMAIL", "VARCHAR(255)"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "tables",
            "USERS.sql",
            SqlTemplates.CreateSimpleTable(
                "USERS",
                "ID INTEGER NOT NULL",
                "NAME VARCHAR(100)",
                "EMAIL D_EMAIL"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "procedures",
            "GET_USER_COUNT.sql",
            SqlTemplates.CreateFirebirdProcedure(
                "GET_USER_COUNT",
                "",
                "RETURNS (USER_COUNT INTEGER)",
                "SELECT COUNT(*) FROM USERS INTO :USER_COUNT;\n    SUSPEND;"));

        var buildService = new DatabaseBuildServiceTestWrapper(
            _mockSqlExecutor,
            FirebirdDatabaseCreatorStub.CreateDatabaseStub);

        // Act
        var result = buildService.BuildDatabase(dbPath, _scriptsDirectory);

        Assert.That(result.ExecutedCount, Is.EqualTo(3), "Powinno być wykonane 3 skrypty");
        Assert.That(result.DomainScripts, Is.EqualTo(1), "Powinna być 1 domena");
        Assert.That(result.TableScripts, Is.EqualTo(1), "Powinna być 1 tabela");
        Assert.That(result.ProcedureScripts, Is.EqualTo(1), "Powinna być 1 procedura");
        
        _mockSqlExecutor.Received(1).ExecuteBatch(
            Arg.Any<List<string>>(),
            Arg.Any<Action<ISqlExecutor>>());
    }

    [Test]
    public void BuildDatabase_WithEmptyScriptsDirectory_ReturnsEmptyResult()
    {
        // Arrange
        var databaseName = "EmptyDatabase";
        var (dbDir, dbPath) = DatabasePathHelper.BuildDatabasePaths(
            Path.Combine(_databaseDirectory, databaseName));

        var buildService = new DatabaseBuildServiceTestWrapper(
            _mockSqlExecutor,
            FirebirdDatabaseCreatorStub.CreateDatabaseStub);

        // Act
        var result = buildService.BuildDatabase(dbPath, _scriptsDirectory);

        // Assert - w Chicago School sprawdzamy zachowanie: metoda powinna obsłużyć pusty katalog
        Assert.That(result.ExecutedCount, Is.EqualTo(0), "Nie powinno być wykonanych skryptów");
        Assert.That(result.DomainScripts, Is.EqualTo(0), "Nie powinno być domen");
        Assert.That(result.TableScripts, Is.EqualTo(0), "Nie powinno być tabel");
        Assert.That(result.ProcedureScripts, Is.EqualTo(0), "Nie powinno być procedur");
        
        _mockSqlExecutor.DidNotReceive().ExecuteBatch(
            Arg.Any<List<string>>(),
            Arg.Any<Action<ISqlExecutor>>());
    }

    [Test]
    public void BuildDatabase_WithMultipleDomains_ProcessesAllDomains()
    {
        // Arrange
        var databaseName = "MultiDomainDatabase";
        var (dbDir, dbPath) = DatabasePathHelper.BuildDatabasePaths(
            Path.Combine(_databaseDirectory, databaseName));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "domains",
            "D_EMAIL.sql",
            SqlTemplates.CreateDomain("D_EMAIL", "VARCHAR(255)"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "domains",
            "D_TIMESTAMP.sql",
            SqlTemplates.CreateDomain("D_TIMESTAMP", "TIMESTAMP"));

        var buildService = new DatabaseBuildServiceTestWrapper(
            _mockSqlExecutor,
            FirebirdDatabaseCreatorStub.CreateDatabaseStub);

        // Act
        var result = buildService.BuildDatabase(dbPath, _scriptsDirectory);

        // Assert
        Assert.That(result.DomainScripts, Is.EqualTo(2), "Powinny być 2 domeny");
        Assert.That(result.ExecutedCount, Is.EqualTo(2), "Powinno być wykonane 2 skrypty");
    }

    [Test]
    public void BuildDatabase_WhenDatabaseAlreadyExists_ThrowsException()
    {
        // Arrange
        var databaseName = "ExistingDatabase";
        var (dbDir, dbPath) = DatabasePathHelper.BuildDatabasePaths(
            Path.Combine(_databaseDirectory, databaseName));

        FirebirdDatabaseCreatorStub.SetExistingDatabase(dbPath);

        var buildService = new DatabaseBuildServiceTestWrapper(
            _mockSqlExecutor,
            FirebirdDatabaseCreatorStub.CreateDatabaseStub);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            buildService.BuildDatabase(dbPath, _scriptsDirectory),
            "Powinien zostać rzucony wyjątek gdy baza już istnieje");
    }

    [Test]
    public void BuildDatabaseCommandHandler_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BuildDatabaseCommandHandler.Handle(null!),
            "Powinien zostać rzucony ArgumentNullException dla null command");
    }
    

    [Test]
    public void DatabaseBuildService_WithScripts_ReturnsCorrectBuildResult()
    {
        // Arrange
        const string databaseName = "TestResultDatabase";
        var (_, dbPath) = DatabasePathHelper.BuildDatabasePaths(
            Path.Combine(_databaseDirectory, databaseName));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "domains",
            "D_TEST.sql",
            SqlTemplates.CreateDomain("D_TEST", "INTEGER"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "tables",
            "TEST_TABLE.sql",
            SqlTemplates.CreateSimpleTable("TEST_TABLE", "ID INTEGER"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "procedures",
            "TEST_PROC.sql",
            SqlTemplates.CreateSimpleProcedure("TEST_PROC"));

        var buildService = new DatabaseBuildServiceTestWrapper(
            _mockSqlExecutor,
            FirebirdDatabaseCreatorStub.CreateDatabaseStub);

        // Act
        var result = buildService.BuildDatabase(dbPath, _scriptsDirectory);

        // Assert - w Chicago School testujemy zachowanie: wynik powinien odzwierciedlać wykonane skrypty
        Assert.That(result.ExecutedCount, Is.EqualTo(3), "Powinno być wykonane 3 skrypty");
        Assert.That(result.DomainScripts, Is.EqualTo(1), "Powinna być 1 domena");
        Assert.That(result.TableScripts, Is.EqualTo(1), "Powinna być 1 tabela");
        Assert.That(result.ProcedureScripts, Is.EqualTo(1), "Powinna być 1 procedura");
    }

    [Test]
    public void DatabaseBuildService_WithEmptyScripts_ReturnsEmptyResult()
    {
        // Arrange
        var databaseName = "EmptyResultDatabase";
        var (dbDir, dbPath) = DatabasePathHelper.BuildDatabasePaths(
            Path.Combine(_databaseDirectory, databaseName));

        var buildService = new DatabaseBuildServiceTestWrapper(
            _mockSqlExecutor,
            FirebirdDatabaseCreatorStub.CreateDatabaseStub);

        // Act
        var result = buildService.BuildDatabase(dbPath, _scriptsDirectory);

        // Assert
        Assert.That(result.ExecutedCount, Is.EqualTo(0), "Nie powinno być wykonanych skryptów");
        Assert.That(result.DomainScripts, Is.EqualTo(0), "Nie powinno być domen");
        Assert.That(result.TableScripts, Is.EqualTo(0), "Nie powinno być tabel");
        Assert.That(result.ProcedureScripts, Is.EqualTo(0), "Nie powinno być procedur");
        
        _mockSqlExecutor.DidNotReceive().ExecuteBatch(
            Arg.Any<List<string>>(),
            Arg.Any<Action<ISqlExecutor>>());
    }

    [Test]
    public void ScriptLoader_LoadsScriptsInCorrectOrder()
    {
        // Arrange
        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "domains",
            "D_EMAIL.sql",
            SqlTemplates.CreateDomain("D_EMAIL", "VARCHAR(255)"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "tables",
            "USERS.sql",
            SqlTemplates.CreateSimpleTable("USERS", "ID INTEGER"));

        _directoryHelper.CreateScriptFile(
            _scriptsDirectory,
            "procedures",
            "GET_COUNT.sql",
            SqlTemplates.CreateSimpleProcedure("GET_COUNT"));

        // Act
        var scripts = ScriptLoader.LoadScriptsInOrder(_scriptsDirectory);

        // Assert 
        Assert.That(scripts, Has.Count.EqualTo(3), "Powinno być 3 skrypty");
        Assert.That(scripts[0].Type, Is.EqualTo(ScriptType.Domain), "Pierwszy powinien być domain");
        Assert.That(scripts[1].Type, Is.EqualTo(ScriptType.Table), "Drugi powinien być table");
        Assert.That(scripts[2].Type, Is.EqualTo(ScriptType.Procedure), "Trzeci powinien być procedure");
    }
}

