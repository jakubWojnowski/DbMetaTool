# Testy jednostkowe - Przegląd

## Wprowadzenie

Projekt zawiera dwa główne pliki testowe, które używają różnych podejść testowych:

- **`BuildDatabaseTests.cs`** - Testy w stylu **Chicago School of Testing** (Classical Testing)
- **`DatabaseUpdateServiceTests.cs`** - Testy w stylu **Londyńskiej Szkoły Testowania** (Mockist Testing)

## Różnice między podejściami

### Chicago School (BuildDatabaseTests.cs)
- **Fokus**: Testowanie zachowania i wyników (output-based testing)
- **Stuby**: Używa stubów dla zewnętrznych zależności (`FirebirdDatabaseCreatorStub`)
- **Mockowanie**: Minimalne użycie mocków, głównie do weryfikacji wywołań
- **Filozofia**: "Testuj jak użytkownik" - sprawdzamy co metoda zwraca, nie jak działa wewnętrznie
- **Zalety**: Testy są bardziej odporne na refaktoryzację, skupiają się na kontrakcie publicznym

### Londyńska Szkoła (DatabaseUpdateServiceTests.cs)
- **Fokus**: Testowanie interakcji między obiektami (interaction-based testing)
- **Mockowanie**: Intensywne użycie mocków (NSubstitute) do izolacji jednostek
- **Filozofia**: "Testuj każdą jednostkę osobno" - weryfikujemy wywołania między obiektami
- **Zalety**: Lepsza izolacja, łatwiejsze debugowanie, wyraźne zależności

---

## BuildDatabaseTests.cs - Chicago School Approach

### Przegląd

Plik testowy `BuildDatabaseTests.cs` zawiera testy jednostkowe dla `DatabaseBuildService` i powiązanych komponentów, napisane w stylu Chicago School. Testy używają stubów (`FirebirdDatabaseCreatorStub`) do izolacji zewnętrznych zależności i skupiają się na weryfikacji zachowania i wyników.

### Pokrycie testów

#### 1. BuildDatabase - Podstawowe scenariusze (3 testy)

- ✅ **BuildDatabase_WithValidScripts_LoadsAndProcessesAllScripts** - Sprawdza, że wszystkie skrypty (domeny, tabele, procedury) są poprawnie wczytane i przetworzone. Weryfikuje liczbę wykonanych skryptów i wywołanie `ExecuteBatch`.
- ✅ **BuildDatabase_WithEmptyScriptsDirectory_ReturnsEmptyResult** - Testuje obsługę pustego katalogu skryptów. Weryfikuje, że metoda zwraca pusty wynik i nie wykonuje żadnych operacji na bazie danych.
- ✅ **BuildDatabase_WithMultipleDomains_ProcessesAllDomains** - Sprawdza przetwarzanie wielu domen w jednym przebiegu. Weryfikuje poprawność zliczania różnych typów skryptów.

#### 2. BuildDatabase - Obsługa błędów (1 test)

- ✅ **BuildDatabase_WhenDatabaseAlreadyExists_ThrowsException** - Testuje ochronę przed nadpisaniem istniejącej bazy danych. Weryfikuje, że `FirebirdDatabaseCreatorStub` poprawnie symuluje istniejącą bazę i rzuca wyjątek.

#### 3. Command Handler - Walidacja (1 test)

- ✅ **BuildDatabaseCommandHandler_WithNullCommand_ThrowsArgumentNullException** - Sprawdza walidację parametrów wejściowych. Weryfikuje, że handler poprawnie obsługuje przypadek `null` command.

#### 4. DatabaseBuildService - Wyniki (2 testy)

- ✅ **DatabaseBuildService_WithScripts_ReturnsCorrectBuildResult** - Testuje poprawność zwracanego `BuildResult`. Weryfikuje, że wszystkie liczniki (ExecutedCount, DomainScripts, TableScripts, ProcedureScripts) są poprawnie ustawione.
- ✅ **DatabaseBuildService_WithEmptyScripts_ReturnsEmptyResult** - Sprawdza zwracanie pustego wyniku gdy brak skryptów. Weryfikuje, że `ExecuteBatch` nie jest wywoływane.

#### 5. ScriptLoader - Kolejność wczytywania (1 test)

- ✅ **ScriptLoader_LoadsScriptsInCorrectOrder** - Testuje poprawność kolejności wczytywania skryptów. Weryfikuje, że skrypty są wczytywane w kolejności: domeny → tabele → procedury, niezależnie od kolejności plików w katalogu.

### Charakterystyka testów Chicago School

- **Użycie stubów**: `FirebirdDatabaseCreatorStub` symuluje tworzenie bazy danych bez faktycznego wywoływania Firebird API
- **Testowanie zachowania**: Testy weryfikują wyniki (`BuildResult`) i wywołania (`ExecuteBatch`), nie szczegóły implementacji
- **Izolacja**: Stuby izolują zewnętrzne zależności, ale testy nie są nadmiernie zależne od wewnętrznej struktury
- **Czytelność**: Testy są łatwe do zrozumienia - sprawdzają "co" a nie "jak"

---

## DatabaseUpdateServiceTests.cs - Londyńska Szkoła Approach

### Przegląd

Plik testowy `DatabaseUpdateServiceTests.cs` zawiera kompleksowy zestaw testów jednostkowych dla klasy `DatabaseUpdateService` używając NUnit i NSubstitute. Testy są napisane w stylu londyńskiej szkoły testowania z intensywnym użyciem mocków.

### Pokrycie testów

#### 1. ProcessUpdate - Ogólne testy (1 test)

- ✅ **ProcessUpdate_WithEmptyScripts_CompletesWithoutChanges** - Sprawdza, że puste skrypty nie powodują zmian. Weryfikuje, że `ExecuteBatch` nie jest wywoływane i lista zmian jest pusta.

#### 2. ProcessDomains - Domeny (3 testy)

- ✅ **ProcessUpdate_WithNewDomain_CreatesDomain** - Sprawdza tworzenie nowej domeny. Weryfikuje wywołanie `ExecuteBatch` z odpowiednim SQL i dodanie zmiany typu `DomainCreated`.
- ✅ **ProcessUpdate_WithExistingDomain_SkipsDomain** - Weryfikuje pomijanie istniejącej domeny. Sprawdza, że `ExecuteBatch` nie jest wywoływane gdy domena już istnieje.
- ✅ **ProcessUpdate_DomainCreationFails_ChangesAddedBeforeException** - Testuje obsługę błędów przy tworzeniu domeny. Weryfikuje, że zmiany są dodawane przed wywołaniem `ExecuteBatch`, nawet gdy operacja się nie powiedzie.

#### 3. ProcessTables - Tabele (3 testy)

- ✅ **ProcessUpdate_WithNewTable_CreatesTable** - Sprawdza tworzenie nowej tabeli. Weryfikuje wywołanie `ExecuteBatch` z `CREATE TABLE` i dodanie odpowiedniej zmiany.
- ✅ **ProcessUpdate_WithExistingTableAndNewColumn_AddsColumn** - Weryfikuje dodawanie kolumny do istniejącej tabeli. Sprawdza generowanie `ALTER TABLE ADD` i dodanie zmiany typu `ColumnAdded`.
- ✅ **ProcessUpdate_TableCreationFails_ChangesAddedBeforeException** - Testuje obsługę błędów przy tworzeniu tabeli. Weryfikuje, że zmiany są rejestrowane przed wykonaniem operacji.

#### 4. ProcessProcedures - Procedury (3 testy)

- ✅ **ProcessUpdate_WithProcedure_ExecutesProcedureScript** - Sprawdza wykonanie skryptu procedury. Weryfikuje wywołanie `ExecuteBatch` i dodanie zmiany typu `ProcedureModified`.
- ✅ **ProcessUpdate_WithExistingProcedureWithCreateStatement_SkipsProcedure** - Testuje pomijanie procedury z `CREATE` gdy już istnieje. Weryfikuje, że `ExecuteBatch` nie jest wywoływane.
- ✅ **ProcessUpdate_ProcedureFails_ChangesAddedBeforeException** - Testuje obsługę błędów przy wykonywaniu procedury. Weryfikuje rejestrowanie zmian przed wykonaniem.

#### 5. Multiple Scripts - Wiele skryptów (1 test)

- ✅ **ProcessUpdate_WithMultipleScriptTypes_ProcessesAllInCorrectOrder** - Weryfikuje przetwarzanie różnych typów skryptów w jednym przebiegu. Sprawdza, że wszystkie typy zmian są poprawnie rejestrowane.

#### 6. GetChanges - Śledzenie zmian (2 testy)

- ✅ **GetChanges_InitiallyEmpty_ReturnsEmptyList** - Sprawdza początkowy stan listy zmian. Weryfikuje, że nowy serwis zwraca pustą listę.
- ✅ **GetChanges_AfterProcessing_ReturnsAccumulatedChanges** - Weryfikuje akumulację zmian. Sprawdza, że wszystkie zmiany są poprawnie gromadzone i zwracane.

#### 7. Edge Cases - Przypadki brzegowe (2 testy)

- ✅ **ProcessUpdate_WithCaseInsensitiveDomainMatch_SkipsDomain** - Sprawdza case-insensitive porównywanie domen. Weryfikuje, że `d_email` i `D_EMAIL` są traktowane jako ta sama domena.
- ✅ **ProcessUpdate_WithCaseInsensitiveTableMatch_ChecksColumns** - Weryfikuje case-insensitive porównywanie tabel. Sprawdza, że `users` i `USERS` są traktowane jako ta sama tabela.

### Charakterystyka testów Londyńskiej Szkoły

- **Intensywne mockowanie**: Używa NSubstitute do mockowania `ISqlExecutor` i wszystkich interakcji
- **Testowanie interakcji**: Testy weryfikują wywołania metod (`Received`, `DidNotReceive`) i przekazywane parametry
- **Izolacja jednostek**: Każdy test izoluje `DatabaseUpdateService` od zewnętrznych zależności
- **Szczegółowość**: Testy sprawdzają zarówno "co" jak i "jak" - weryfikują konkretne wywołania i parametry
