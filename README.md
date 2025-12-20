# DbMetaTool

Narzędzie CLI do zarządzania metadanymi i schematem bazy danych Firebird 5.0.

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

# Windows - baza w lokalnym katalogu ./databases/
dotnet run --project DbMetaTool build-db \
  --db-dir "myapp" \
  --scripts-dir "./scripts"

# Wynik: ./databases/myapp.fdb
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

Connection String:
DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/myapp/myapp.fdb;User=SYSDBA;Password=masterkey;Charset=UTF8;ServerType=0;Dialect=3
```

#### ⚠️ Ważne

- **Transakcyjność**: Wszystkie skrypty są wykonywane w **jednej transakcji**. Jeśli którykolwiek zawiedzie, cała operacja jest wycofywana (ROLLBACK).
- **Ochrona przed nadpisaniem**: Jeśli baza o podanej nazwie już istnieje, operacja zostanie **przerwana** z komunikatem błędu. Ze względów bezpieczeństwa narzędzie **nie nadpisuje** istniejących baz danych.
  - Aby utworzyć bazę, najpierw usuń starą ręcznie lub użyj innej nazwy
  - To zapobiega przypadkowej utracie danych

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
  --connection-string "DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/myapp/myapp.fdb;User=SYSDBA;Password=masterkey" \
  --output-dir "./output/myapp"
```

#### Wyjście

```
=== Eksport metadanych z bazy Firebird ===

Connection String: DataSource=localhost;Port=3050;Database=...
Katalog wyjściowy: ./output/myapp

Pobieranie metadanych...
✓ Znaleziono 2 domen
✓ Znaleziono 3 tabel
✓ Znaleziono 1 procedur

Generowanie skryptów SQL...
✓ Zapisano 2 skryptów domen
✓ Zapisano 3 skryptów tabel
✓ Zapisano 1 skryptów procedur

=== Podsumowanie ===
Katalog wyjściowy: D:\Projects\DbMetaTool\output\myapp
Łącznie plików: 6

Skrypty zostały wyeksportowane pomyślnie.
```

#### Struktura wygenerowanych plików

```
output/myapp/
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
  --connection-string "DataSource=localhost;Port=3050;Database=/var/lib/firebird/data/myapp/myapp.fdb;User=SYSDBA;Password=masterkey" \
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

