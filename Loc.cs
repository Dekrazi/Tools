using System;
using System.Collections.Generic;
using System.IO;
namespace RepairToolkit
{
    // English is the default language; Polish is selected with the PL button and remembered between runs.
    public static class Loc
    {
        public static bool Pl;
        static readonly Dictionary<string, string> d = Build();

        public static string T(string en)
        {
            string v;
            if (Pl && d.TryGetValue(en, out v)) return v;
            return en;
        }

        public static string F(string en, params object[] args)
        {
            return string.Format(T(en), args);
        }

        // Nothing is stored on the PC and the app always starts in English; the EN / PL buttons switch for the current run only.
        public static void Load() { Pl = false; }

        static Dictionary<string, string> Build()
        {
            var d = new Dictionary<string, string>();
            d["The application could not start."] = "Nie udało się uruchomić aplikacji.";
            d["01   Tool catalog"] = "01   Katalog narzędzi";
            d["02   Operations console"] = "02   Konsola operacji";
            d["OPERATION RUNNING \u2022 Output is in the Operations console tab."] = "OPERACJA W TOKU \u2022 Wynik jest w zakładce Konsola operacji.";
            d["READY \u2022 Operation finished."] = "GOTOWE \u2022 Operacja zakończona.";
            d["An operation is still running. Stop it or wait for it to finish before closing."] = "Operacja nadal trwa. Zatrzymaj ją lub poczekaj na zakończenie przed zamknięciem.";
            d["Search"] = "Szukaj";
            d["Quick PowerShell"] = "Szybkie PowerShell";
            d["Quick CMD"] = "Szybkie CMD";
            d["Open PowerShell"] = "Otwórz PowerShell";
            d["Open CMD"] = "Otwórz CMD";
            d["Open Terminal"] = "Otwórz Terminal";
            d["Full system report"] = "Pełny raport systemu";
            d["READY \u2022 Pick a command and click Run."] = "GOTOWE \u2022 Wybierz polecenie i kliknij Uruchom.";
            d["Restart as administrator"] = "Uruchom ponownie jako administrator";
            d["Running as Administrator"] = "Uruchomiono jako Administrator";
            d["Standard user - admin commands ask for UAC"] = "Zwykły użytkownik - polecenia admina pytają o UAC";
            d["DIAGNOSTICS"] = "DIAGNOSTYKA";
            d["REPAIR"] = "NAPRAWA";
            d["MAINTENANCE"] = "KONSERWACJA";
            d["KNOWLEDGE BASE"] = "BAZA WIEDZY";
            d["No results"] = "Brak wyników";
            d["Change the search or press Esc to clear the filter."] = "Zmień wyszukiwanie lub naciśnij Esc, aby wyczyścić filtr.";
            d["No results for: "] = "Brak wyników dla: ";
            d["Command / tool"] = "Polecenie / narzędzie";
            d["Risk"] = "Ryzyko";
            d["Shell"] = "Powłoka";
            d["\u25B6 Run"] = "\u25B6 Uruchom";
            d["Copy command"] = "Kopiuj polecenie";
            d["Open page"] = "Otwórz stronę";
            d["Low"] = "Niskie";
            d["Caution"] = "Uwaga";
            d["High"] = "Wysokie";
            d["Info"] = "Informacja";
            d["No commands match the filter"] = "Brak poleceń pasujących do filtra";
            d["{0} categories \u2022 {1} commands and links \u2022 {2}"] = "{0} kategorii \u2022 {1} poleceń i odnośników \u2022 {2}";
            d["Filter: "] = "Filtr: ";
            d["Ready - pick a command and click Run"] = "Gotowe - wybierz polecenie i kliknij Uruchom";
            d["Administrator required"] = "Wymagany administrator";
            d["Copy (WinRE only)"] = "Kopiuj (tylko WinRE)";
            d["Run shortcut"] = "Wykonaj skrót";
            d["\u25B6 Run as admin (UAC)"] = "\u25B6 Uruchom jako admin (UAC)";
            d["Copied: "] = "Skopiowano: ";
            d["This command only works from the Windows Recovery Environment (boot from a Windows USB, then Repair, then Command Prompt).\n\nIt has been copied to your clipboard."] = "To polecenie działa tylko w środowisku odzyskiwania Windows (uruchom komputer z pendrive Windows, potem Napraw, potem Wiersz polecenia).\n\nSkopiowano je do schowka.";
            d["Copied"] = "Skopiowano";
            d["Invalid input"] = "Nieprawidłowe dane";
            d["Run this operation?"] = "Uruchomić tę operację?";
            d["Confirm operation"] = "Potwierdź operację";
            d["Cannot open the page."] = "Nie można otworzyć strony.";
            d["Web page"] = "Strona internetowa";
            d["Could not open {0}:\n{1}"] = "Nie można otworzyć {0}:\n{1}";
            d["Launch failed"] = "Uruchomienie nie powiodło się";
            d["Busy"] = "Zajęte";
            d["Input needed"] = "Wymagane dane";
            d["OK"] = "OK";
            d["Cancel"] = "Anuluj";
            d["Could not access the clipboard (another program may be using it)."] = "Brak dostępu do schowka (może go używać inny program).";
            d["Clipboard"] = "Schowek";
            d["Operations console"] = "Konsola operacji";
            d["Pick a command in the catalog and press Run."] = "Wybierz polecenie w katalogu i kliknij Uruchom.";
            d["Copy"] = "Kopiuj";
            d["Select all"] = "Zaznacz wszystko";
            d["Command output appears here."] = "Wyniki poleceń pojawią się tutaj.";
            d["Administrator commands stay in this view too."] = "Polecenia administratora również pozostają w tym widoku.";
            d["Send reply"] = "Wyślij odpowiedź";
            d["Stop"] = "Zatrzymaj";
            d["Copy output"] = "Kopiuj wynik";
            d["Save result"] = "Zapisz wynik";
            d["Clear"] = "Wyczyść";
            d["Replies go to the running command. Choose new commands in the catalog."] = "Odpowiedzi trafiają do działającego polecenia. Nowe komendy wybieraj w katalogu.";
            d["Wait for the current operation to finish."] = "Poczekaj na zakończenie bieżącej operacji.";
            d["Waiting for administrator approval (UAC)..."] = "Oczekiwanie na zgodę administratora (UAC)...";
            d["Starting..."] = "Uruchamianie...";
            d["Running"] = "W toku";
            d["administrator"] = "administrator";
            d["current user"] = "bieżący użytkownik";
            d["Stopped"] = "Zatrzymano";
            d["[stopped]"] = "[zatrzymano]";
            d["Finished \u2022 exit code: {0}"] = "Zakończono \u2022 kod wyjścia: {0}";
            d["[finished - exit code {0}, {1} s]"] = "[zakończono - kod wyjścia {0}, {1} s]";
            d["Use \"Save result\" below to keep this report."] = "Użyj \"Zapisz wynik\" poniżej, aby zachować ten raport.";
            d["UAC approval was cancelled."] = "Anulowano potwierdzenie UAC.";
            d["Could not start the operation."] = "Nie udało się uruchomić operacji.";
            d["Operation error"] = "Błąd operacji";
            d["Stopping..."] = "Zatrzymywanie...";
            d["There is no output to save yet."] = "Nie ma jeszcze wyniku do zapisania.";
            d["Save"] = "Zapis";
            d["Text file (*.txt)|*.txt|All files (*.*)|*.*"] = "Plik tekstowy (*.txt)|*.txt|Wszystkie pliki (*.*)|*.*";
            d["Saved: "] = "Zapisano: ";
            d["Could not connect to the administrator process."] = "Nie udało się połączyć z procesem administratora.";
            d["The administrator process closed the connection without a result."] = "Proces administratora zakończył połączenie bez wyniku.";
            d["The command does not accept input yet."] = "Polecenie nie przyjmuje jeszcze odpowiedzi.";
            d["The value contains a character that is not allowed (quotes, backtick or a control character), is empty, or is longer than 260 characters."] = "Wartość zawiera niedozwolony znak (cudzysłów, apostrof wsteczny lub znak sterujący), jest pusta albo ma ponad 260 znaków.";
            d["(No output - the command finished and found nothing to show.)"] = "(Brak wyniku - polecenie zakończyło się i nie znalazło nic do pokazania.)";
            d["(No output was produced.)"] = "(Polecenie nie wypisało żadnego wyniku.)";
            d["A non-zero exit code usually means the command reported a problem or found nothing - the text above explains why. Some tools also need Administrator rights."] = "Niezerowy kod wyjścia zwykle oznacza, że polecenie zgłosiło problem albo nic nie znalazło - powód opisuje tekst powyżej. Niektóre narzędzia wymagają też uprawnień administratora.";
            d["Open"] = "Otwórz";
            d["Opened: "] = "Otwarto: ";
            d["Windows Terminal (wt.exe) is not installed on this PC."] = "Windows Terminal (wt.exe) nie jest zainstalowany na tym komputerze.";
            d["Run again"] = "Uruchom ponownie";
            d["Copy for ticket"] = "Kopiuj do zgłoszenia";
            d["Copied with a header for a ticket."] = "Skopiowano z nagłówkiem do zgłoszenia.";
            d["Find in output:"] = "Szukaj w wyniku:";
            d["Nothing found: "] = "Nie znaleziono: ";
            d["Restore point first: ON"] = "Punkt przywracania najpierw: WŁ.";
            d["Restore point first: OFF"] = "Punkt przywracania najpierw: WYŁ.";
            d["Recent"] = "Ostatnie";
            d["(none yet)"] = "(jeszcze nic)";
            d["A restore point is created first (Restore point first is ON)."] = "Najpierw zostanie utworzony punkt przywracania (włączono „Punkt przywracania najpierw”).";
            d["Restore point skipped: this command runs without administrator rights, so Windows would refuse it. Use Restart as administrator."] = "Punkt przywracania pominięty: to polecenie działa bez uprawnień administratora, więc Windows by go odrzucił. Użyj „Uruchom ponownie jako administrator”.";
            d["Version {0}"] = "Wersja {0}";
            d["Creates a System Restore point before every Caution or High-risk command (administrator rights needed)."] = "Tworzy punkt przywracania systemu przed każdym poleceniem o ryzyku Uwaga lub Wysokie (wymaga uprawnień administratora).";
            d["Color"] = "Kolor";
            d["START"] = "START";
            d["SEARCH RESULTS"] = "WYNIKI WYSZUKIWANIA";
            d["Quick"] = "Szybkie";
            d["SYSTEM INFO"] = "INFORMACJE O SYSTEMIE";
            d["REPAIR AND NETWORK"] = "NAPRAWA I SIEĆ";
            d["Run the selected command (Enter or double-click)"] = "Uruchom zaznaczone polecenie (Enter lub dwukrotne kliknięcie)";
            d["Copy the command text (Ctrl+C)"] = "Kopiuj tekst polecenia (Ctrl+C)";
            d["The last 10 commands run in this session"] = "10 ostatnich poleceń uruchomionych w tej sesji";
            return d;
        }
    }
}
