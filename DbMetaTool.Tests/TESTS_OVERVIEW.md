# Testy jednostkowe - DatabaseUpdateService

## Przegląd

Plik testowy `DatabaseUpdateServiceTests.cs` zawiera kompleksowy zestaw testów jednostkowych dla klasy `DatabaseUpdateService` używając NUnit i NSubstitute.

## Pokrycie testów

### 1. ProcessUpdate - Ogólne testy (2 testy)

- ✅ **ProcessUpdate_WithEmptyScripts_CompletesWithoutChanges** - Sprawdza, że puste skrypty nie powodują zmian
- ✅ **ProcessUpdate_ExecutesInTransaction** - Weryfikuje, że operacje są wykonywane w transakcji

### 2. ProcessDomains - Domeny (3 testy)

- ✅ **ProcessUpdate_WithNewDomain_CreatesDomain** - Sprawdza tworzenie nowej domeny
- ✅ **ProcessUpdate_WithExistingDomain_SkipsDomain** - Weryfikuje pomijanie istniejącej domeny
- ✅ **ProcessUpdate_DomainCreationFails_AddsManualReviewChange** - Testuje obsługę błędów przy tworzeniu domeny

### 3. ProcessTables - Tabele (3 testy)

- ✅ **ProcessUpdate_WithNewTable_CreatesTable** - Sprawdza tworzenie nowej tabeli
- ✅ **ProcessUpdate_WithExistingTableAndNewColumn_AddsColumn** - Weryfikuje dodawanie kolumny do istniejącej tabeli
- ✅ **ProcessUpdate_TableCreationFails_AddsManualReviewChange** - Testuje obsługę błędów przy tworzeniu tabeli

### 4. ProcessProcedures - Procedury (2 testy)

- ✅ **ProcessUpdate_WithProcedure_ExecutesProcedureScript** - Sprawdza wykonanie skryptu procedury
- ✅ **ProcessUpdate_ProcedureFails_AddsManualReviewChange** - Testuje obsługę błędów przy wykonywaniu procedury

### 5. Multiple Scripts - Wiele skryptów (1 test)

- ✅ **ProcessUpdate_WithMultipleScriptTypes_ProcessesAllInCorrectOrder** - Weryfikuje przetwarzanie różnych typów skryptów

### 6. GetChanges - Śledzenie zmian (2 testy)

- ✅ **GetChanges_InitiallyEmpty_ReturnsEmptyList** - Sprawdza początkowy stan listy zmian
- ✅ **GetChanges_AfterProcessing_ReturnsAccumulatedChanges** - Weryfikuje akumulację zmian

### 7. Edge Cases - Przypadki brzegowe (2 testy)

- ✅ **ProcessUpdate_WithCaseInsensitiveDomainMatch_SkipsDomain** - Sprawdza case-insensitive porównywanie domen
- ✅ **ProcessUpdate_WithCaseInsensitiveTableMatch_ChecksColumns** - Weryfikuje case-insensitive porównywanie tabel
