using DbMetaTool.Databases;
using DbMetaTool.Models;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Update;
using DbMetaTool.Tests.TestHelpers;
using NSubstitute;

namespace DbMetaTool.Tests;

[TestFixture]
public class DatabaseUpdateServiceTests
{
    private ISqlExecutor _mockExecutor = null!;
    private IScriptLoader _scriptLoader = null!;
    private DatabaseUpdateService _service = null!;
    private SqlScriptHelper _scriptHelper = null!;

    [SetUp]
    public void SetUp()
    {
        _mockExecutor = Substitute.For<ISqlExecutor>();
        _scriptLoader = new ScriptLoader();
        _scriptHelper = new SqlScriptHelper();

        _service = new DatabaseUpdateService(_mockExecutor, _scriptLoader);
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
        var procedures = new List<ProcedureMetadata>();

        // Act
        _service.ProcessUpdate(scripts, domains, tables, procedures);

        // Assert
        Assert.That(_service.GetChanges(), Is.Empty);
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>());
    }

    #endregion

    #region ProcessDomains Tests

    [Test]
    public void ProcessUpdate_WithNewDomain_CreatesDomain()
    {
        // Arrange
        var script = _scriptHelper.CreateDomainScriptFromTemplate("D_EMAIL", "VARCHAR(255)");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        _mockExecutor.Received(1).ExecuteBatch(
            Arg.Is<List<string>>(list => list.Any(s => s.Contains("CREATE DOMAIN"))),
            Arg.Any<Action<ISqlExecutor>>());
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.DomainCreated));
        Assert.That(changes[0].ObjectName, Is.EqualTo("D_EMAIL"));
    }

    [Test]
    public void ProcessUpdate_WithExistingDomain_SkipsDomain()
    {
        // Arrange
        var script = _scriptHelper.CreateDomainScriptFromTemplate("D_EMAIL", "VARCHAR(255)");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>
        {
            TestDataBuilder.CreateDomain("D_EMAIL", "VARCHAR", 255)
        };
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>());
        Assert.That(_service.GetChanges(), Is.Empty);
    }

    [Test]
    public void ProcessUpdate_DomainCreationFails_ChangesAddedBeforeException()
    {
        // Arrange
        var script = _scriptHelper.CreateDomainScriptFromTemplate("D_EMAIL", "VARCHAR(255)");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        _mockExecutor
            .When(x => x.ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>()))
            .Do(x => throw new Exception("Database error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures));
        
        // Changes are added before ExecuteBatch is called, so they should be present even if ExecuteBatch fails
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.DomainCreated));
        Assert.That(changes[0].ObjectName, Is.EqualTo("D_EMAIL"));
    }

    #endregion

    #region ProcessTables Tests

    [Test]
    public void ProcessUpdate_WithNewTable_CreatesTable()
    {
        // Arrange
        var script = _scriptHelper.CreateTableScriptFromTemplate(
            "USERS",
            "ID INTEGER NOT NULL",
            "NAME VARCHAR(100)");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        _mockExecutor.Received(1).ExecuteBatch(
            Arg.Is<List<string>>(list => list.Any(s => s.Contains("CREATE TABLE"))),
            Arg.Any<Action<ISqlExecutor>>());
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.TableCreated));
        Assert.That(changes[0].ObjectName, Is.EqualTo("USERS"));
    }

    [Test]
    public void ProcessUpdate_WithExistingTableAndNewColumn_AddsColumn()
    {
        // Arrange
        var script = _scriptHelper.CreateTableColumnsOnlyScript(
            "USERS",
            "ID INTEGER NOT NULL",
            "NAME VARCHAR(100)",
            "EMAIL VARCHAR(255)");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>
        {
            TestDataBuilder.CreateTable(
                "USERS",
                TestDataBuilder.CreateIntegerColumn("ID", 0),
                TestDataBuilder.CreateVarcharColumn("NAME", 1, 100))
        };
        var existingProcedures = new List<ProcedureMetadata>();

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        _mockExecutor.Received(1).ExecuteBatch(
            Arg.Is<List<string>>(list => list.Any(s => s.Contains("ALTER TABLE") && s.Contains("ADD"))),
            Arg.Any<Action<ISqlExecutor>>());
        
        var changes = _service.GetChanges();
        var columnAdded = changes.FirstOrDefault(c => c.Type == ChangeType.ColumnAdded);
        Assert.That(columnAdded, Is.Not.Null);
        Assert.That(columnAdded!.ObjectName, Does.Contain("USERS"));
        Assert.That(columnAdded!.ObjectName, Does.Contain("EMAIL"));
    }

    [Test]
    public void ProcessUpdate_TableCreationFails_ChangesAddedBeforeException()
    {
        // Arrange
        var script = _scriptHelper.CreateTableScriptFromTemplate("USERS", "ID INTEGER");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        _mockExecutor
            .When(x => x.ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>()))
            .Do(x => throw new Exception("Table creation failed"));

        // Act & Assert
        Assert.Throws<Exception>(() => _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures));
        
        // Changes are added before ExecuteBatch is called
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.TableCreated));
        Assert.That(changes[0].ObjectName, Is.EqualTo("USERS"));
    }

    #endregion

    #region ProcessProcedures Tests

    [Test]
    public void ProcessUpdate_WithProcedure_ExecutesProcedureScript()
    {
        // Arrange
        var script = _scriptHelper.CreateFirebirdProcedureScript(
            "GET_USER_COUNT",
            "",
            "RETURNS (USER_COUNT INTEGER)",
            "SELECT COUNT(*) FROM USERS INTO :USER_COUNT;\n    SUSPEND;");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        // Mock ExecuteRead for ProcedureDependencyValidator.GetCallingProcedures
        _mockExecutor.ExecuteRead(Arg.Any<string>(), Arg.Any<Func<System.Data.IDataReader, string>>())
            .Returns(new List<string>());

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        _mockExecutor.Received(1).ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>());
        
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.ProcedureModified));
        Assert.That(changes[0].ObjectName, Is.EqualTo("GET_USER_COUNT"));
        Assert.That(changes[0].Details, Is.EqualTo("Wykonano skrypt"));
    }

    [Test]
    public void ProcessUpdate_WithExistingProcedureWithCreateStatement_SkipsProcedure()
    {
        // Arrange
        var script = _scriptHelper.CreateSimpleProcedureScript("EXISTING_PROC");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>
        {
            TestDataBuilder.CreateProcedure("EXISTING_PROC", "CREATE PROCEDURE EXISTING_PROC AS BEGIN END")
        };

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>());
        Assert.That(_service.GetChanges(), Is.Empty);
    }

    [Test]
    public void ProcessUpdate_ProcedureFails_ChangesAddedBeforeException()
    {
        // Arrange
        var script = _scriptHelper.CreateSimpleProcedureScript("TEST_PROC");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        // Mock ExecuteRead for ProcedureDependencyValidator.GetCallingProcedures
        _mockExecutor.ExecuteRead(Arg.Any<string>(), Arg.Any<Func<System.Data.IDataReader, string>>())
            .Returns(new List<string>());

        _mockExecutor
            .When(x => x.ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>()))
            .Do(x => throw new Exception("Procedure error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures));
        
        // Changes are added before ExecuteBatch is called
        var changes = _service.GetChanges();
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].Type, Is.EqualTo(ChangeType.ProcedureModified));
        Assert.That(changes[0].ObjectName, Is.EqualTo("TEST_PROC"));
    }

    #endregion

    #region Multiple Scripts Tests

    [Test]
    public void ProcessUpdate_WithMultipleScriptTypes_ProcessesAllInCorrectOrder()
    {
        // Arrange
        var domainScript = _scriptHelper.CreateDomainScriptFromTemplate("D_TEST", "INTEGER");
        var tableScript = _scriptHelper.CreateTableScriptFromTemplate("TEST_TABLE", "ID INTEGER");
        var procScript = _scriptHelper.CreateSimpleProcedureScript("TEST_PROC");
        
        var scripts = new List<ScriptFile>
        {
            procScript,
            tableScript,
            domainScript
        };
        var existingDomains = new List<DomainMetadata>();
        var existingTables = new List<TableMetadata>();
        var existingProcedures = new List<ProcedureMetadata>();

        // Mock ExecuteRead for ProcedureDependencyValidator.GetCallingProcedures
        _mockExecutor.ExecuteRead(Arg.Any<string>(), Arg.Any<Func<System.Data.IDataReader, string>>())
            .Returns(new List<string>());

        // Act
        _service.ProcessUpdate(scripts, existingDomains, existingTables, existingProcedures);

        // Assert
        // ExecuteBatch should be called once with all statements
        _mockExecutor.Received(1).ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>());
        
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
        var script = _scriptHelper.CreateDomainScriptFromTemplate("D_CHANGES", "INTEGER");
        
        var scripts = new List<ScriptFile> { script };

        // Act
        _service.ProcessUpdate(scripts, new List<DomainMetadata>(), new List<TableMetadata>(), new List<ProcedureMetadata>());
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
        var script = _scriptHelper.CreateDomainScriptFromTemplate("d_email", "VARCHAR(255)");
        
        var scripts = new List<ScriptFile> { script };
        var existingDomains = new List<DomainMetadata>
        {
            TestDataBuilder.CreateDomain("D_EMAIL", "VARCHAR", 255)
        };

        // Act
        _service.ProcessUpdate(scripts, existingDomains, new List<TableMetadata>(), new List<ProcedureMetadata>());

        // Assert
        _mockExecutor.DidNotReceive().ExecuteBatch(Arg.Any<List<string>>(), Arg.Any<Action<ISqlExecutor>>());
        Assert.That(_service.GetChanges(), Is.Empty);
    }

    [Test]
    public void ProcessUpdate_WithCaseInsensitiveTableMatch_ChecksColumns()
    {
        // Arrange
        var script = _scriptHelper.CreateTableScriptFromTemplate("users", "ID INTEGER NOT NULL");
        
        var scripts = new List<ScriptFile> { script };
        var existingTables = new List<TableMetadata>
        {
            TestDataBuilder.CreateTable(
                "USERS",
                TestDataBuilder.CreateIntegerColumn("ID", 0))
        };

        // Act
        _service.ProcessUpdate(scripts, new List<DomainMetadata>(), existingTables, new List<ProcedureMetadata>());

        // Assert - tabela nie powinna być tworzona ponownie
        _mockExecutor.DidNotReceive().ExecuteBatch(
            Arg.Is<List<string>>(list => list.Any(s => s.Contains("CREATE TABLE"))),
            Arg.Any<Action<ISqlExecutor>>());
    }

    #endregion
}