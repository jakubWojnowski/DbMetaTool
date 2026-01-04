using DbMetaTool.Models;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.Update;
using DbMetaTool.Tests.TestHelpers;
using NSubstitute;

namespace DbMetaTool.Tests;

[TestFixture]
public class DatabaseUpdateServiceTests
{
    private ISqlExecutor _mockExecutor = null!;
    private DatabaseUpdateService _service = null!;
    private SqlScriptHelper _scriptHelper = null!;

    [SetUp]
    public void SetUp()
    {
        _mockExecutor = Substitute.For<ISqlExecutor>();
        _scriptHelper = new SqlScriptHelper();

        _service = new DatabaseUpdateService(_mockExecutor);
    }

    [TearDown]
    public void TearDown()
    {
        _scriptHelper?.Dispose();
    }

    #region ProcessUpdate Tests

    [Test]
    public void ProcessUpdate_WithEmptyScripts_CompletesWithoutChanges()
    {
        // Arrange
        var scripts = new List<ScriptFile>();
        var domains = new List<DomainMetadata>();
        var tables = new List<TableMetadata>();

        // Act
        _service.ProcessUpdate(scripts, domains, tables);

        // Assert
        Assert.That(_service.GetChanges(), Is.Empty);
    }

    #endregion

    #region ProcessDomains Tests

    [Test]
    public void ProcessUpdate_WithNewDomain_CreatesDomain()
    {
        // Arrange
        var domainSql = SqlTemplates.CreateDomain("D_EMAIL", "VARCHAR(255)");
        var script = _scriptHelper.CreateDomainScript("D_EMAIL", domainSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables);

        // Assert
        var containsDomainCreate = Arg.Is<string>(s => s.Contains("CREATE DOMAIN"));
        _mockExecutor.Received(1).ExecuteBatch(Arg.Is<List<string>>(list => 
            list.Any(s => s.Contains("CREATE DOMAIN"))));
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.DomainCreated));
        Assert.That(changes[0].ObjectName, Is.EqualTo("D_EMAIL"));
    }

    [Test]
    public void ProcessUpdate_WithExistingDomain_SkipsDomain()
    {
        // Arrange
        var domainSql = SqlTemplates.CreateDomain("D_EMAIL", "VARCHAR(255)");
        var script = _scriptHelper.CreateDomainScript("D_EMAIL", domainSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>
        {
            TestDataBuilder.CreateDomain("D_EMAIL", "VARCHAR", 255)
        };
        var existingTables = new List<TableMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables);

        // Assert
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Any<List<string>>());
        Assert.That(_service.GetChanges(), Is.Empty);
    }

    [Test]
    public void ProcessUpdate_DomainCreationFails_AddsManualReviewChange()
    {
        // Arrange
        var domainSql = SqlTemplates.CreateDomain("D_EMAIL", "VARCHAR(255)");
        var script = _scriptHelper.CreateDomainScript("D_EMAIL", domainSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        _mockExecutor
            .When(x => x.ExecuteBatch(Arg.Any<List<string>>()))
            .Do(x => throw new Exception("Database error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _service.ProcessUpdate(scripts, existingDomains, existingTables));
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.ManualReviewRequired));
        Assert.That(changes[0].ObjectName, Is.EqualTo("D_EMAIL"));
        Assert.That(changes[0].Details, Does.Contain("Błąd tworzenia"));
    }

    #endregion

    #region ProcessTables Tests

    [Test]
    public void ProcessUpdate_WithNewTable_CreatesTable()
    {
        // Arrange
        var tableSql = SqlTemplates.CreateSimpleTable(
            "USERS",
            "ID INTEGER NOT NULL",
            "NAME VARCHAR(100)");
        var script = _scriptHelper.CreateTableScript("USERS", tableSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables);

        // Assert
        var containsTableCreate = Arg.Is<string>(s => s.Contains("CREATE TABLE"));
        _mockExecutor.Received().ExecuteBatch(Arg.Is<List<string>>(list => 
            list.Any(s => s.Contains("CREATE TABLE"))));
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.TableCreated));
        Assert.That(changes[0].ObjectName, Is.EqualTo("USERS"));
    }

    [Test]
    public void ProcessUpdate_WithExistingTableAndNewColumn_AddsColumn()
    {
        // Arrange
        var tableSql = SqlTemplates.CreateSimpleTable(
            "USERS",
            "ID INTEGER NOT NULL",
            "NAME VARCHAR(100)",
            "EMAIL VARCHAR(255)");
        var script = _scriptHelper.CreateTableScript("USERS", tableSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>
        {
            TestDataBuilder.CreateTable(
                "USERS",
                TestDataBuilder.CreateIntegerColumn("ID", 0),
                TestDataBuilder.CreateVarcharColumn("NAME", 1, 100))
        };

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables);

        // Assert
        var containsAlterAdd = Arg.Is<string>(s => 
            s.Contains("ALTER TABLE") && 
            s.Contains("ADD") && 
            s.Contains("EMAIL"));
        _mockExecutor.Received().ExecuteBatch(Arg.Is<List<string>>(list => 
            list.Any(s => s.Contains("ALTER TABLE") && s.Contains("ADD"))));
        
        var changes = _service.GetChanges();
        var columnAdded = changes.FirstOrDefault(c => c.Type == ChangeType.ColumnAdded);
        Assert.That(columnAdded, Is.Not.Null);
        Assert.That(columnAdded!.ObjectName, Does.Contain("USERS"));
        Assert.That(columnAdded!.ObjectName, Does.Contain("EMAIL"));
    }

    [Test]
    public void ProcessUpdate_TableCreationFails_AddsManualReviewChange()
    {
        // Arrange
        var tableSql = SqlTemplates.CreateSimpleTable("USERS", "ID INTEGER");
        var script = _scriptHelper.CreateTableScript("USERS", tableSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        _mockExecutor
            .When(x => x.ExecuteBatch(Arg.Any<List<string>>()))
            .Do(x => throw new Exception("Table creation failed"));

        // Act & Assert
        Assert.Throws<Exception>(() => _service.ProcessUpdate(scripts, existingDomains, existingTables));
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.ManualReviewRequired));
        Assert.That(changes[0].ObjectName, Is.EqualTo("USERS"));
    }

    #endregion

    #region ProcessProcedures Tests

    [Test]
    public void ProcessUpdate_WithProcedure_ExecutesProcedureScript()
    {
        // Arrange
        var procedureSql = SqlTemplates.CreateFirebirdProcedure(
            "GET_USER_COUNT",
            "",
            "RETURNS (USER_COUNT INTEGER)",
            "SELECT COUNT(*) FROM USERS INTO :USER_COUNT;\n    SUSPEND;");
        var script = _scriptHelper.CreateProcedureScript("GET_USER_COUNT", procedureSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables);

        // Assert
        _mockExecutor.Received().ExecuteBatch(Arg.Any<List<string>>());
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.ProcedureModified));
        Assert.That(changes[0].ObjectName, Is.EqualTo("GET_USER_COUNT"));
    }

    [Test]
    public void ProcessUpdate_ProcedureFails_AddsManualReviewChange()
    {
        // Arrange
        var procedureSql = SqlTemplates.CreateSimpleProcedure("TEST_PROC");
        var script = _scriptHelper.CreateProcedureScript("TEST_PROC", procedureSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        _mockExecutor
            .When(x => x.ExecuteBatch(Arg.Any<List<string>>()))
            .Do(x => throw new Exception("Procedure error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _service.ProcessUpdate(scripts, existingDomains, existingTables));
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.ManualReviewRequired));
        Assert.That(changes[0].ObjectName, Is.EqualTo("TEST_PROC"));
    }

    #endregion

    #region Multiple Scripts Tests

    [Test]
    public void ProcessUpdate_WithMultipleScriptTypes_ProcessesAllInCorrectOrder()
    {
        // Arrange
        var domainSql = SqlTemplates.CreateDomain("D_TEST", "INTEGER");
        var domainScript = _scriptHelper.CreateDomainScript("D_TEST", domainSql);
        
        var tableSql = SqlTemplates.CreateSimpleTable("TEST_TABLE", "ID INTEGER");
        var tableScript = _scriptHelper.CreateTableScript("TEST_TABLE", tableSql);
        
        var procSql = SqlTemplates.CreateSimpleProcedure("TEST_PROC");
        var procScript = _scriptHelper.CreateProcedureScript("TEST_PROC", procSql);
        
        var scripts = new List<ScriptFile>
        {
            procScript,
            tableScript,
            domainScript
        };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables);

        // Assert
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(3));
        
        var hasDomainCreated = changes.Any(c => c.Type == ChangeType.DomainCreated);
        var hasTableCreated = changes.Any(c => c.Type == ChangeType.TableCreated);
        var hasProcedureModified = changes.Any(c => c.Type == ChangeType.ProcedureModified);
        
        Assert.That(hasDomainCreated, Is.True);
        Assert.That(hasTableCreated, Is.True);
        Assert.That(hasProcedureModified, Is.True);
    }

    #endregion

    #region GetChanges Tests

    [Test]
    public void GetChanges_InitiallyEmpty_ReturnsEmptyList()
    {
        // Act
        var changes = _service.GetChanges();

        // Assert
        Assert.That(changes, Is.Not.Null);
        Assert.That(changes, Is.Empty);
    }

    [Test]
    public void GetChanges_AfterProcessing_ReturnsAccumulatedChanges()
    {
        // Arrange
        var domainSql = SqlTemplates.CreateDomain("D_CHANGES", "INTEGER");
        var script = _scriptHelper.CreateDomainScript("D_CHANGES", domainSql);
        
        var scripts = new List<ScriptFile> { script };

        // Act
        _service.ProcessUpdate(scripts, new List<DomainMetadata>(), new List<TableMetadata>());
        var changes = _service.GetChanges();

        // Assert
        Assert.That(changes, Is.Not.Empty);
        Assert.That(changes[0].ObjectName, Is.EqualTo("D_CHANGES"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ProcessUpdate_WithCaseInsensitiveDomainMatch_SkipsDomain()
    {
        // Arrange
        var domainSql = SqlTemplates.CreateDomain("d_email", "VARCHAR(255)");
        var script = _scriptHelper.CreateDomainScript("d_email", domainSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>
        {
            TestDataBuilder.CreateDomain("D_EMAIL", "VARCHAR", 255)
        };

        // Act
        _service.ProcessUpdate(scripts, existingDomains, new List<TableMetadata>());

        // Assert
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Any<List<string>>());
        Assert.That(_service.GetChanges(), Is.Empty);
    }

    [Test]
    public void ProcessUpdate_WithCaseInsensitiveTableMatch_ChecksColumns()
    {
        // Arrange
        var tableSql = SqlTemplates.CreateSimpleTable("users", "ID INTEGER NOT NULL");
        var script = _scriptHelper.CreateTableScript("users", tableSql);
        
        var scripts = new List<ScriptFile> { script };
        var existingTables = new List<TableMetadata>
        {
            TestDataBuilder.CreateTable(
                "USERS",
                TestDataBuilder.CreateIntegerColumn("ID", 0))
        };

        // Act
        _service.ProcessUpdate(scripts, new List<DomainMetadata>(), existingTables);

        // Assert - tabela nie powinna być tworzona ponownie
        var containsCreateTable = Arg.Is<string>(s => s.Contains("CREATE TABLE"));
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Is<List<string>>(list => 
            list.Any(s => s.Contains("CREATE TABLE"))));
    }

    #endregion
}