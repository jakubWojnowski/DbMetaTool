# DbMetaTool

Narzędzie CLI do zarządzania metadanymi i schematem bazy danych Firebird 5.0.

## ✨ Kluczowe funkcje

- **🔒 Izolacja snapshot** - Eksport metadanych w spójnym stanie z jednego momentu w czasie
- **✅ Walidacja kompilacji** - Automatyczna weryfikacja integralności procedur (BLR) po każdej zmianie
- **⚠️ Analiza zależności** - Ostrzeżenia o procedurach wywołujących modyfikowane procedury
- **🔄 Transakcyjność** - Wszystkie operacje w jednej transakcji z automatycznym rollback przy błędach
- **🛡️ Bezpieczeństwo** - Ochrona przed przypadkową utratą danych (brak automatycznego usuwania)

## 📋 Spis treści

- [Wymagania](#wymagania)
- [Instalacja](#instalacja)
- [Konfiguracja Docker](#konfiguracja-docker)
- [Budowanie projektu](#budowanie-projektu)
- [Użycie](#użycie)
  - [build-db - Budowanie nowej bazy](#build-db---budowanie-nowej-bazy)
  - [export-scripts - Eksport metadanych](#export-scripts---eksport-metadanych)
  - [update-db - Aktualizacja bazy](#update-db---aktualizacja-bazy)
- [Struktura projektu](#struktura-projektu)
- [Przykłady](#przykłady)

---

## 🔧 Wymagania

- **.NET 8.0 SDK** - [Pobierz](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop** - [Pobierz](https://www.docker.com/products/docker-desktop)
- **Firebird 5.0** (uruchamiany w Dockerze)

---

## 📦 Instalacja

### 1. Klonowanie repozytorium

```bash
git clone <repository-url>
cd DbMetaTool
```

### 2. Konfiguracja Docker

#### Uruchomienie kontenera Firebird

```bash
cd compose
docker-compose up -d
```

Sprawdzenie statusu:

```bash
docker-compose ps
```

Powinieneś zobaczyć:

```
NAME           IMAGE                         STATUS         PORTS
firebird-db    firebirdsql/firebird:5.0.1   Up 2 minutes   0.0.0.0:3050->3050/tcp
```

#### Domyślne dane dostępowe

- **Host**: `localhost`
- **Port**: `3050`
- **User**: `SYSDBA`
- **Password**: `masterkey`
- **Connection String**: `DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/<nazwa_bazy>/<nazwa_bazy>.fdb;User=SYSDBA;Password=masterkey`

#### Przechowywanie danych

Bazy danych są przechowywane w **Docker volume** `firebird-data`. Dane są trwałe i przetrwają restart kontenera.

#### Zatrzymanie kontenera

```bash
docker-compose down
```

## 🚀 Użycie

### Składnia ogólna

```bash
dotnet run --project DbMetaTool <komenda> [opcje]
```

lub po zbudowaniu:

```bash
DbMetaTool.exe <komenda> [opcje]
```

---

## 📝 Komendy

### `build-db` - Budowanie nowej bazy

Tworzy nową bazę danych Firebird i wykonuje skrypty SQL (domeny, tabele, procedury) w jednej transakcji.

#### Składnia

```bash
dotnet run --project DbMetaTool build-db --db-dir <ścieżka> --scripts-dir <ścieżka>
```

#### Parametry

| Parametr | Opis | Wymagany |
|----------|------|----------|
| `--db-dir` | Ścieżka do katalogu, gdzie zostanie utworzona baza (dla Dockera: `/var/lib/firebird/data/<nazwa>`) | ✅ Tak |
| `--scripts-dir` | Ścieżka do katalogu ze skryptami SQL | ✅ Tak |

#### Przykład

```bash
# Docker - baza bezpośrednio w /var/lib/firebird/data/
dotnet run --project DbMetaTool build-db \
  --db-dir "/var/lib/firebird/data/myapp" \
  --scripts-dir "./scripts"

# Wynik: /var/lib/firebird/data/myapp.fdb
```

#### Struktura katalogu skryptów

```
scripts/
├── domains/
│   ├── D_EMAIL.sql
│   └── D_TIMESTAMP.sql
├── tables/
│   ├── USERS.sql
│   └── PRODUCTS.sql
└── procedures/
    └── GET_USER_COUNT.sql
```

#### Kolejność wykonania

1. **Domeny** (`domains/`) - definicje domenowe
2. **Tabele** (`tables/`) - struktury tabel
3. **Procedury** (`procedures/`) - stored procedures

#### Wyjście

```
=== Budowanie bazy danych Firebird ===

Katalog bazy: /var/lib/firebird/data/myapp
Plik bazy: /var/lib/firebird/data/myapp/myapp.fdb
Katalog skryptów: ./scripts

✓ Utworzono pustą bazę danych
Znaleziono 5 skryptów do wykonania:
  - Domeny: 2
  - Tabele: 2
  - Procedury: 1

Wykonywanie: Domain/D_EMAIL.sql... ✓
Wykonywanie: Domain/D_TIMESTAMP.sql... ✓
Wykonywanie: Table/USERS.sql... ✓
Wykonywanie: Table/PRODUCTS.sql... ✓
Wykonywanie: Procedure/GET_USER_COUNT.sql... ✓

=== Podsumowanie ===
Wykonano pomyślnie: 5
```

#### ⚠️ Ważne

- **Transakcyjność**: Wszystkie skrypty są wykonywane w **jednej transakcji**. Jeśli którykolwiek zawiedzie, cała operacja jest wycofywana (ROLLBACK).
- **Ochrona przed nadpisaniem**: Jeśli baza o podanej nazwie już istnieje, operacja zostanie **przerwana** z komunikatem błędu. Ze względów bezpieczeństwa narzędzie **nie nadpisuje** istniejących baz danych.
  - Aby utworzyć bazę, najpierw usuń starą ręcznie lub użyj innej nazwy
  - To zapobiega przypadkowej utracie danych

#### ✅ Walidacja kompilacji procedur (BLR)

Po wykonaniu wszystkich skryptów, narzędzie automatycznie **waliduje integralność procedur**:

- Sprawdza, czy wszystkie procedury mają poprawny BLR (Binary Language Representation)
- Jeśli któraś procedura ma nieprawidłowy BLR (`RDB$VALID_BLR = 0`), operacja zostaje **przerwana** i transakcja jest wycofywana
- Zapobiega to sytuacjom, w których zmiana sygnatury jednej procedury powoduje błędy kompilacji w procedurach zależnych

**Przykład błędu walidacji:**

```
=== Walidacja integralności procedur ===
⚠ Znaleziono 2 procedur z nieprawidłowym BLR:
  - IMPORT_EMPLOYEES_DATA
  - PROCESS_ORDERS

Błąd walidacji integralności procedur:

Znaleziono 2 procedur z nieprawidłowym BLR:
  - IMPORT_EMPLOYEES_DATA
  - PROCESS_ORDERS

Prawdopodobna przyczyna: Niezgodność sygnatur wywołań procedur.
Sprawdź skrypty SQL i upewnij się, że wszystkie wywołania procedur mają poprawne sygnatury.
```

**Co to oznacza?**
- Procedury `IMPORT_EMPLOYEES_DATA` i `PROCESS_ORDERS` nie mogą się skompilować
- Prawdopodobnie wywołują inną procedurę (np. `CREATE_EMPLOYEE`) z nieprawidłową sygnaturą
- Wszystkie zmiany zostały wycofane (ROLLBACK) - baza pozostaje w poprzednim stanie

---

### `export-scripts` - Eksport metadanych

Eksportuje metadane z istniejącej bazy danych do plików SQL.

#### Składnia

```bash
dotnet run --project DbMetaTool export-scripts \
  --connection-string <connection-string> \
  --output-dir <ścieżka>
```

#### Parametry

| Parametr | Opis | Wymagany |
|----------|------|----------|
| `--connection-string` | Connection string do bazy Firebird | ✅ Tak |
| `--output-dir` | Katalog wyjściowy dla wygenerowanych skryptów | ✅ Tak |

#### Przykład

```bash
dotnet run --project DbMetaTool export-scripts \
  --connection-string "DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/database1.fdb;User=SYSDBA;Password=masterkey" \
  --output-dir "./output/database1"
```

#### Wyjście

```
=== Eksport metadanych z bazy Firebird ===

Connection String: DataSource=localhost;Port=3050;Database=...
Katalog wyjściowy: ./output/database1

Pobieranie metadanych...
✓ Znaleziono 2 domen
✓ Znaleziono 3 tabel
✓ Znaleziono 1 procedur

Generowanie skryptów SQL...
✓ Zapisano 2 skryptów domen
✓ Zapisano 3 skryptów tabel
✓ Zapisano 1 skryptów procedur

=== Podsumowanie ===
Katalog wyjściowy: D:\Projects\DbMetaTool\output\database1
Łącznie plików: 6

Skrypty zostały wyeksportowane pomyślnie.
```

#### Struktura wygenerowanych plików

```
output/database1/
├── domains/
│   ├── D_EMAIL.sql
│   └── D_TIMESTAMP.sql
├── tables/
│   ├── USERS.sql
│   ├── PRODUCTS.sql
│   └── CARS.sql
└── procedures/
    └── GET_USER_COUNT.sql
```

#### Co jest eksportowane?

- **Domeny**: Pełne definicje `CREATE DOMAIN`
- **Tabele**: Pełne definicje `CREATE TABLE` z kolumnami, typami, NOT NULL, DEFAULT
- **Procedury**: Pełne definicje `CREATE PROCEDURE` z parametrami i ciałem

#### ⚠️ Filtrowanie

Narzędzie automatycznie **pomija systemowe obiekty Firebird**:
- `RDB$*` - systemowe tabele/procedury
- `MON$*` - monitoring
- `SEC$*` - security

#### 🔒 Izolacja transakcyjna (Snapshot Isolation)

Operacja eksportu używa **izolacji snapshot** (Concurrency) do zapewnienia spójności metadanych:

- Wszystkie odczyty metadanych (domeny, tabele, procedury) są wykonywane w **jednej transakcji snapshot**
- Gwarantuje to, że wyeksportowane skrypty reprezentują **spójny stan bazy danych** z jednego momentu w czasie
- Zapobiega sytuacjom, w których mogłyby zostać pobrane metadane z różnych momentów (np. stara wersja jednej procedury i nowa wersja innej)
- Dzięki temu pliki w repozytorium Git zawsze reprezentują działający, spójny stan bazy danych

**Techniczne szczegóły:**
- Transakcja używa `FbTransactionBehavior.Concurrency | FbTransactionBehavior.Wait | FbTransactionBehavior.Read`
- Wszystkie zapytania do `RDB$*` tabel systemowych używają tej samej transakcji snapshot

---

### `update-db` - Aktualizacja bazy

Aktualizuje schemat istniejącej bazy danych na podstawie skryptów. Wykonuje tylko **bezpieczne operacje** (dodawanie, modyfikacja procedur). Operacje destrukcyjne wymagają ręcznej interwencji.

#### Składnia

```bash
dotnet run --project DbMetaTool update-db \
  --connection-string <connection-string> \
  --scripts-dir <ścieżka>
```

#### Parametry

| Parametr | Opis | Wymagany |
|----------|------|----------|
| `--connection-string` | Connection string do bazy Firebird | ✅ Tak |
| `--scripts-dir` | Katalog ze skryptami SQL (docelowy stan bazy) | ✅ Tak |

#### Przykład

```bash
dotnet run --project DbMetaTool update-db \
  --connection-string "DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/database1.fdb;User=SYSDBA;Password=masterkey" \
  --scripts-dir "./scripts"
```

#### Wyjście

```
=== Aktualizacja bazy danych Firebird ===

Connection String: DataSource=localhost;Port=3050;Database=...
Katalog skryptów: ./scripts

Pobieranie aktualnego stanu bazy...
✓ Obecny stan: 2 domen, 2 tabel, 1 procedur

Wczytano 6 skryptów

=== Przetwarzanie domen ===
  Domena D_EMAIL już istnieje - pomijam
  Domena D_TIMESTAMP już istnieje - pomijam
  Tworzenie domeny D_PHONE... ✓

=== Przetwarzanie tabel ===
  Tabela USERS istnieje - sprawdzam kolumny...
    Dodawanie kolumny... ✓
  Tabela PRODUCTS istnieje - sprawdzam kolumny...
    ⚠ -- MANUAL REVIEW REQUIRED: Column PRICE exists but has different definition
  Tworzenie tabeli ORDERS... ✓

=== Przetwarzanie procedur ===
  Procedura GET_USER_COUNT... ✓
  Procedura CALCULATE_TOTAL... ✓

=== Raport zmian ===

DomainCreated:
  - D_PHONE

TableCreated:
  - ORDERS

ColumnAdded:
  - USERS.PHONE

ProcedureModified:
  - GET_USER_COUNT: Wykonano skrypt
  - CALCULATE_TOTAL: Wykonano skrypt

ManualReviewRequired:
  - PRODUCTS: -- MANUAL REVIEW REQUIRED: Column PRICE exists but has different definition

Podsumowanie:
  Domeny utworzone: 1
  Tabele utworzone: 1
  Kolumny dodane: 1
  Procedury zmodyfikowane: 2
  Wymaga przeglądu manualnego: 1
```

#### Co jest aktualizowane?

✅ **Bezpieczne operacje (automatyczne)**:
- Tworzenie nowych domen
- Tworzenie nowych tabel
- Dodawanie nowych kolumn do istniejących tabel
- Tworzenie/modyfikacja procedur (`CREATE OR ALTER PROCEDURE`)

⚠️ **Wymaga przeglądu manualnego**:
- Zmiana typu kolumny
- Zmiana NOT NULL na kolumnie

❌ **NIE jest obsługiwane (ze względu na bezpieczeństwo danych)**:
- **Usuwanie kolumn** - kolumny, które istnieją w bazie ale nie ma ich w skryptach, pozostają nietknięte
- **Usuwanie tabel** - tabele, które istnieją w bazie ale nie ma ich w skryptach, pozostają nietknięte
- **Usuwanie domen** - domeny, które istnieją w bazie ale nie ma ich w skryptach, pozostają nietknięte

> **⚠️ WAŻNE**: Jeśli potrzebujesz usunąć kolumnę, tabelę lub domenę, musisz to zrobić **ręcznie** przez klienta SQL (np. `isql`, FlameRobin). Narzędzie **świadomie pomija** operacje destrukcyjne, aby zapobiec przypadkowej utracie danych.

#### ⚠️ Transakcyjność

Wszystkie operacje są wykonywane w **jednej transakcji**. Jeśli jakakolwiek operacja zawiedzie, całość zostaje wycofana (ROLLBACK).

#### ✅ Walidacja kompilacji procedur (BLR)

Po wykonaniu wszystkich zmian, narzędzie automatycznie **waliduje integralność procedur**:

- Sprawdza, czy wszystkie procedury mają poprawny BLR (Binary Language Representation)
- Jeśli któraś procedura ma nieprawidłowy BLR (`RDB$VALID_BLR = 0`), operacja zostaje **przerwana** i transakcja jest wycofywana
- Zapobiega to sytuacjom, w których zmiana sygnatury jednej procedury powoduje błędy kompilacji w procedurach zależnych

**Przykład błędu walidacji:**

```
=== Walidacja integralności procedur ===
⚠ Znaleziono 1 procedur z nieprawidłowym BLR:
  - IMPORT_EMPLOYEES_DATA

Błąd walidacji integralności procedur:

Znaleziono 1 procedur z nieprawidłowym BLR:
  - IMPORT_EMPLOYEES_DATA

Prawdopodobna przyczyna: Niezgodność sygnatur wywołań procedur.
Sprawdź skrypty SQL i upewnij się, że wszystkie wywołania procedur mają poprawne sygnatury.
```

**Co to oznacza?**
- Procedura `IMPORT_EMPLOYEES_DATA` nie może się skompilować po zmianach
- Prawdopodobnie wywołuje inną procedurę (np. `CREATE_EMPLOYEE`) z nieprawidłową sygnaturą po jej modyfikacji
- Wszystkie zmiany zostały wycofane (ROLLBACK) - baza pozostaje w poprzednim stanie

#### ⚠️ Ostrzeżenia o zależnościach procedur

Przed modyfikacją procedury, narzędzie sprawdza **zależności** i ostrzega, jeśli procedura jest wywoływana przez inne:

**Przykład ostrzeżenia:**

```
=== Przetwarzanie procedur ===
  Procedura CREATE_EMPLOYEE... 
    ⚠ Procedura jest wywoływana przez: IMPORT_EMPLOYEES_DATA, PROCESS_NEW_HIRES
  ✓
```

**Co to oznacza?**
- Procedura `CREATE_EMPLOYEE` jest używana przez `IMPORT_EMPLOYEES_DATA` i `PROCESS_NEW_HIRES`
- Zmiana sygnatury `CREATE_EMPLOYEE` może spowodować błędy kompilacji w procedurach zależnych
- Narzędzie kontynuuje operację, ale ostrzega o potencjalnych problemach
- Jeśli walidacja BLR wykryje błędy, transakcja zostanie wycofana

**Zalecenie:**
- Przed modyfikacją procedury używanej przez inne, sprawdź wszystkie zależności
- Upewnij się, że wszystkie wywołania mają poprawną sygnaturę
- Rozważ aktualizację wszystkich procedur zależnych w jednej operacji

---

## 🛡️ Bezpieczeństwo i integralność danych

DbMetaTool implementuje szereg mechanizmów zapewniających bezpieczeństwo i integralność danych:

### Transakcyjność i atomowość

- **Wszystkie operacje DDL** (CREATE, ALTER) są wykonywane w **jednej transakcji**
- Jeśli jakakolwiek operacja zawiedzie, **cała transakcja jest wycofywana** (ROLLBACK)
- Gwarantuje to, że baza danych zawsze pozostaje w spójnym stanie - albo wszystkie zmiany są zatwierdzone, albo żadna

### Walidacja integralności procedur

- Po każdej operacji modyfikującej procedury, narzędzie **automatycznie sprawdza kompilację** wszystkich procedur
- Wykrywa błędy kompilacji wynikające z niezgodności sygnatur (np. zmiana parametrów procedury wywoływanej przez inne)
- Jeśli wykryje nieprawidłowe procedury, **automatycznie wycofuje wszystkie zmiany**
- Zapobiega sytuacjom, w których baza pozostaje w stanie z niekompilującymi się procedurami

### Izolacja snapshot w eksporcie

- Eksport metadanych używa **izolacji snapshot** (Concurrency) Firebird
- Wszystkie odczyty są wykonywane w **jednej transakcji snapshot**
- Gwarantuje **spójność metadanych** - wyeksportowane skrypty reprezentują stan bazy z jednego momentu
- Zapobiega sytuacjom, w których mogłyby zostać pobrane metadane z różnych momentów w czasie

### Ochrona przed utratą danych

- **Brak automatycznego usuwania** - narzędzie nie usuwa kolumn, tabel ani domen, nawet jeśli nie ma ich w skryptach
- **Ochrona przed nadpisaniem** - operacja `build-db` nie nadpisze istniejącej bazy danych
- **Ostrzeżenia o zależnościach** - przed modyfikacją procedury, narzędzie informuje o procedurach, które ją wywołują

### Obsługa blokad i współbieżności

- Operacje zapisu używają `FbTransactionBehavior.Wait` z timeoutem 10 sekund
- Pozwala to narzędziu poczekać na zwolnienie blokady przez inne sesje
- Zapobiega błędom "object in use" podczas aktualizacji metadanych na "żywym" systemie

### Przykładowy scenariusz bezpieczeństwa

```
1. Użytkownik modyfikuje procedurę CREATE_EMPLOYEE (dodaje nowy parametr)
2. Narzędzie ostrzega: "Procedura jest wywoływana przez: IMPORT_EMPLOYEES_DATA"
3. Narzędzie wykonuje zmianę w transakcji
4. Walidacja BLR wykrywa, że IMPORT_EMPLOYEES_DATA ma nieprawidłowy BLR
5. Transakcja jest automatycznie wycofywana (ROLLBACK)
6. Baza pozostaje w poprzednim, działającym stanie
7. Użytkownik otrzymuje czytelny komunikat o błędzie i może poprawić skrypty
```

---

## 📁 Struktura projektu

```
DbMetaTool/
├── Commands/              # Handlery komend CLI
│   ├── BuildDatabase/     # build-db
│   ├── ExportMetadata/    # export-scripts
│   └── UpdateDatabase/    # update-db
├── Configuration/         # Konfiguracja (domyślne wartości)
├── Firebird/             # Factory dla połączeń Firebird
├── Models/               # Modele danych (metadata, results)
├── Services/             # Logika biznesowa
│   ├── DatabaseBuildService.cs
│   ├── MetadataExportService.cs
│   ├── DatabaseUpdateService.cs
│   ├── FirebirdMetadataReader.cs
│   ├── SqlScriptGenerator.cs
│   └── ...
├── Utilities/            # Narzędzia pomocnicze
│   ├── DatabasePathHelper.cs
│   ├── ScriptDefinitionParser.cs
│   └── SqlScriptParser.cs
└── Program.cs            # Entry point
```

