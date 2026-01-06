using DbMetaTool.Firebird;


namespace DbMetaTool.Commands.UpdateDatabase;

public static class UpdateDatabaseCommandHandler
{
    public static void Handle(UpdateDatabaseCommand command)
    {
        var (connectionString, scriptsDirectory) = command;
        
        // musi przyjac connection string do bazy na ktorej chcemy wprowadzic zmiany oraz katalog z skryptami na podstawie ktorych wykonamy zmiane
        
        //Wczytaj skrypty z katalogu i sklasyfikuj (domains/tables/procedures).
        // Domains: dla każdego pliku sprawdź czy domena istnieje ( SELECT ... FROM RDB$FIELDS
        //     lub RDB$FIELDS/RDB$DOMAINS) — jeśli nie istnieje: CREATE DOMAIN . Jeżeli istnieje i różni się:
        // zaloguj i albo próbuj ALTER DOMAIN (jeśli proste), albo zostaw do manualnego poprawienia.
        //     Tables: dla każdej tabeli:
        // jeśli tabela nie istnieje → wykonaj CREATE TABLE z pliku.
        //     jeśli istnieje → porównaj kolumny (nazwy i typy):
        // brakująca kolumna → ALTER TABLE <t> ADD <col> .
        //     jeżeli typ kolumny różni się → nie robić automatycznego ALTER (można próbować z
        //     logowaniem i wymagać potwierdzenia manualnego).
        // Procedures: Wywołaj CREATE OR ALTER PROCEDURE <name> AS ... (Firebird 2+ obsługuje
        // CREATE OR ALTER) — w ten sposób nadpiszesz procedurę kodem ze skryptu.
        //     Loguj wszystkie zmiany i generuj raport: added tables , added columns , modified
        //     procedures , manual review required .
        //     Zasada bezpieczeństwa: automatyczne usuwanie (DROP) wyłączone. Wszystkie
        //     destrukcyjne operacje wymagają ręcznego potwierdzenia.

    }
}
