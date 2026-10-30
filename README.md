# SCP-066 Repaired — 2.0.1

🇵🇱 [Polski](#polski) · 🇬🇧 [English](#english)

## Polski

Custom SCP dla SCP: Secret Laboratory, oparty na [SCP-066 RisottoMan](https://github.com/RisottoMan/SCP-066), z modelem ProjectMER i umiejętnościami przypisywanymi przez natywne Server-specific Settings (SS2).

### Poprawki 2.0.1

- Wyłączenie pluginu usuwa wyłącznie jego rolę, sesje, keybindy i dane audio.
- Callbacki spawnu i teleportu są anulowane przy wyłączeniu i zmianie rundy; teleport sprawdza konkretne nadanie roli.
- Brak lub usunięcie schematu pozostawia widoczny model zombie.
- HS regeneruje się niezależnie od SCP-049, z opóźnieniem po rzeczywistym otrzymaniu obrażeń i z uwzględnieniem blokad tarczy.
- HP jest ustawiane podczas nadania roli, bez ponownego leczenia po 0,5 s.
- Głośność ma skalę 0–1. Stare `volume: 100` zostaje ograniczone do 1 z ostrzeżeniem w logu.
- Każdy klip jest walidowany i ładowany osobno. Niepoprawny OGG nie blokuje pozostałego audio.
- Audio jest ładowane raz na instancję pluginu, z prywatnymi aliasami i kontrolą własności przy usuwaniu.
- Beethoven zapętla się do końca ataku i jest zatrzymywany przy jego zakończeniu lub przerwaniu.
- Concussed jest nakładany wyłącznie po zaakceptowanym trafieniu, na nadal żywą ofiarę w tej samej roli.
- Nieznana podkomenda RA jest odrzucana. Konflikt ID roli lub keybindów zatrzymuje inicjalizację z komunikatem w logu.

### Wymagania i zakres weryfikacji

Wymagane: serwer x64, EXILED z włączonymi Events, CustomRoles i Permissions, [ProjectMER](https://github.com/Michal78900/MapEditorReborn) oraz [AudioPlayerApi](https://github.com/ExMod-Team/AudioPlayerApi) z jego zależnościami.

Projekt celuje w .NET Framework 4.8.1. Minimalna deklarowana wersja EXILED to **9.13.2**. Kompilację sprawdzono z DLL dołączonymi do repozytorium: EXILED 9.13.2 i LabApi 1.1.6. `packages.config` wskazuje AudioPlayerApi 1.1.2; AssemblyVersion dołączonej DLL wynosi 0.0.0.0, a ProjectMER 1.0.0.0 — nie identyfikują one jednoznacznie wydania produktu.

**Kompilacja i testy poza grą nie potwierdzają zgodności z każdą nowszą wersją gry/pluginów.** Po instalacji wykonaj [testy serwerowe](docs/TESTING.md), szczególnie przy innym zestawie bibliotek. Nie zastępuj bibliotek działającego serwera plikami z REFERENCES; ten katalog służy do kompilacji.

### Instalacja i aktualizacja

1. Wgraj skompilowany `Scp066.dll` do `EXILED/Plugins/`. Wynik kompilacji znajduje się w `bin/Release/Scp066.dll`. Usuń starszą kopię pluginu, aby uniknąć podwójnego ładowania.
2. Dostarcz własny schemat do katalogu Schematics używanej instalacji ProjectMER. Nazwa w `schematic_name` musi odpowiadać faktycznej nazwie. Domyślnie jest to **scp066** małymi literami. Model i nagrania rozgrywki nie są dołączone do repozytorium.
3. Umieść nagrania w `EXILED/Configs/Plugins/scp066/Audio/`:
   - `Beethoven.ogg`
   - `Eric1.ogg`, `Eric2.ogg`, `Eric3.ogg`
   - `Notes1.ogg` … `Notes6.ogg`
4. Wymagany jest OGG Vorbis, **mono, 48 kHz**, do 300 s i 32 MiB na plik. Brak audio nie blokuje obrażeń; brak modelu nie ukrywa zombie.
5. Zrestartuj lub przeładuj plugin i sprawdź log. Ustawienia pojawiają się w konfiguracji EXILED pod prefiksem `scp066`. Dla konfiguracji indywidualnej jest to `EXILED/Configs/Plugins/scp066/<port>.yml`; przy konfiguracji zbiorczej użyj sekcji `scp066` w `EXILED/Configs/<port>-config.yml`.

Zachowane ustawienia YAML mają pierwszeństwo przed nowymi domyślnymi. Nieprawidłowe wartości są korygowane podczas włączenia i raportowane w logu. Dla starego `volume: 100` warto samodzielnie zapisać `volume: 0.8` lub `1` w YAML.

Wyłączenie pluginu przywraca jego żywych graczy do zwykłej roli zombie z bazowymi statystykami. Inne CustomRoles nie są wyrejestrowywane.

### Konfiguracja

| Opcja | Domyślnie | Znaczenie / dozwolony zakres |
|---|---:|---|
| is_enabled | true | Włączenie pluginu |
| debug | false | Ustawienie debug EXILED |
| spawn_chance | 35 | 0–100%; jedno losowanie po 3 s od początku rundy |
| max_health | 2000 | Początkowe i maksymalne HP; 1–65535 |
| hume_shield | 500 | Początkowe i maksymalne HS; 0–65535 |
| hume_shield_regen_rate | 10 | HS/s; 0–500, 0 wyłącza regenerację |
| hume_shield_regen_delay | 10 | Czas bez obrażeń przed regeneracją; 0–300 s |
| schematic_name | scp066 | Nazwa modelu ProjectMER |
| attack_radius | 15 | Promień obrażeń i strefy pełnej głośności; 0,1–100 m |
| damage | 7 | Bazowe obrażenia co sekundę; 0–1000 |
| attack_duration | 22 | Liczba sekund/ticków ataku; 1–300 |
| volume | 0.8 | Mnożnik amplitudy; 0–1 |
| cooldown_attack | 25 | Czas od zakończenia ataku; 0–3600 s |
| cooldown_eric | 5 | Odnowienie Erica; 0–3600 s |
| cooldown_music | 8 | Odnowienie Notes; 0–3600 s |
| role_id | 660 | Unikalne ID CustomRole, większe od zera |
| beethoven_keybind_id | 6601 | Unikalne dodatnie ID SS2 |
| eric_keybind_id | 6602 | Unikalne dodatnie ID SS2 |
| music_keybind_id | 6603 | Unikalne dodatnie ID SS2 |

### Rozgrywka

- Losowy spawn zastępuje jednego SCP; pomija SCP-079 oraz graczy z inną CustomRole. Próbuje teleportować SCP-066 do HCZ Armory; awaryjnie wybiera pokój HCZ.
- Bazą jest zombie w skali 0,5 ze Slowness 10. **Bloodlust zombie pozostaje aktywny**; ruch nie ma jednej sztywnej prędkości.
- LPM zombie i jedzenie ciał są blokowane. Blokada LPM nie odrzuca wszystkich innych typów obrażeń pochodzących z zewnętrznych pluginów.
- Atak przechodzi przez ściany i zamknięte drzwi; filtr jest odległościowy w 3D.
- Pomijane są SCP według bazowej drużyny, Tutorial, godmode, spawn protection i sam atakujący. Custom SCP oparty na ludzkiej roli wymaga osobnej polityki sojuszy.
- Pierwszy tick obrażeń jest natychmiastowy. Domyślnie 22 × 7 = **154 bazowe obrażenia na cel**; kolejny atak może rozpocząć się około **47 s** po poprzednim.
- Concussed trwa 2 s i jest odnawiany po kolejnych zaakceptowanych trafieniach.
- Krótszy Beethoven jest zapętlany, dłuższy ucinany na końcu ataku. Eric i Notes są losowane wyłącznie z poprawnie załadowanych klipów; nowy dźwięk zastępuje poprzedni. Podczas ataku nie można uruchamiać Erica ani Notes.
- Przy cooldownie kosmetycznych dźwięków równym 0 obowiązuje minimalny odstęp 0,5 s, aby ograniczyć zalewanie odtwarzacza.
- Klawisze przypisuje się w menu **ESC → Server-specific → SCP-066**. Domyślnie nie są przypisane.

### Komendy RA

Wymagane uprawnienie: `scp066.admin`. Alias komendy: `066`.

| Komenda | Działanie |
|---|---|
| scp066 give <nick/id> | Nadaje rolę; odrzuca gracza mającego już dowolną CustomRole |
| scp066 remove <nick/id> | Odbiera SCP-066 i **przenosi gracza do obserwatorów** |

### Budowanie i testy

Z katalogu repozytorium, z .NET SDK 8:

```bash
dotnet build "SCP066 REPAIRED.csproj" -c Release
dotnet run --project tests/RegressionTests.csproj -c Release
```

Testy uruchamiają produkcyjny kod konfiguracji i katalogu audio z rzeczywistymi bibliotekami AudioPlayerApi i NVorbis. Sprawdzają m.in. uszkodzone/niewłaściwe OGG, izolację właścicieli klipów, sprzątanie i bezpieczną migrację głośności. Krótkie sinusoidy w tests/Fixtures są wyłącznie danymi testowymi.

Pełna lista testów wymagających gry: [docs/TESTING.md](docs/TESTING.md).

## English

SCP-066 custom role for SCP: Secret Laboratory, based on [RisottoMan's SCP-066](https://github.com/RisottoMan/SCP-066), with ProjectMER visuals and native SS2 keybinds.

### Version 2.0.1

Cleanup is scoped to this plugin's roles, settings, timers and audio. Delayed work checks the current round and role session. Missing models retain the visible zombie. HS regenerates independently of SCP-049. HP is assigned on spawn without a delayed refill. Volume uses a 0–1 amplitude multiplier; legacy 100 is clamped to 1 with a warning. Clips are validated individually, loaded once per plugin instance, and stored under private aliases. Beethoven loops until the attack ends and is stopped on cancellation. Concussed requires an accepted hit against a living target in the same native role. Invalid RA subcommands and conflicting IDs are rejected.

### Requirements and installation

Requires x64 server, EXILED Events/CustomRoles/Permissions, ProjectMER and AudioPlayerApi with their dependencies. Target: .NET Framework 4.8.1; declared minimum EXILED 9.13.2. Compilation was checked against the included EXILED 9.13.2 and LabApi 1.1.6 references. Do not replace live server libraries with REFERENCES files. Other game/dependency versions require server testing.

1. Install the compiled `bin/Release/Scp066.dll` into `EXILED/Plugins/` and remove older copies.
2. Supply the model in your ProjectMER installation's Schematics directory. Match `schematic_name` exactly; the default is lowercase **scp066**.
3. Put `Beethoven.ogg`, `Eric1.ogg`–`Eric3.ogg` and `Notes1.ogg`–`Notes6.ogg` in `EXILED/Configs/Plugins/scp066/Audio/`. Required: Vorbis, mono, 48 kHz, positive duration up to 300 s, up to 32 MiB each. Gameplay model/audio assets are not included.
4. Restart/reload and inspect logs. Individual EXILED config: `EXILED/Configs/Plugins/scp066/<port>.yml`; combined config: section `scp066` in `EXILED/Configs/<port>-config.yml`.

Existing YAML overrides defaults. Invalid values are repaired at enable time and logged. Set legacy `volume: 100` to `0.8` or `1` in your YAML. Unloading restores this plugin's living players to ordinary zombies with native stats; other CustomRoles are left registered.

### Default configuration and behavior

Default values: 35% spawn chance; 2000 HP; 500 HS; HS regeneration 10/s after 10 s without accepted damage; 15 m radius; 7 damage per tick; 22 ticks; volume 0.8; attack cooldown 25 s after completion; Eric cooldown 5 s; Notes cooldown 8 s. ID defaults: role 660, Beethoven 6601, Eric 6602, Notes 6603. The configuration table above lists all keys and validated ranges.

The first tick is immediate. A full attack deals **154 base damage per target**, with about **47 s between activations**. Walls/doors do not block the distance-based attack. SCP team members, Tutorial, godmode, spawn protection and the attacker are excluded. Custom SCPs on human base roles need a separate alliance policy.

The role retains zombie Bloodlust and Slowness 10 at scale 0.5. Base melee and corpse consumption are blocked. Beethoven loops if short and is cut off at attack end. Cosmetic sounds choose only loaded clips and replace the previous sound; they are blocked during the attack. Zero cosmetic cooldown still has a 0.5 s anti-flood interval. Bind abilities in ESC → Server-specific → SCP-066.

RA permission: `scp066.admin`. `scp066 give <nickname/id>` rejects players with an existing custom role. `scp066 remove <nickname/id>` removes SCP-066 and **moves the player to Spectator**. Unknown subcommands do nothing.

Build and run the regression commands above using .NET SDK 8. Tests cover production config/audio loading against real dependencies, not a running game. Complete [the server checklist](docs/TESTING.md) before live use.

## Credits

Matysiak / JastrząbDEV — repaired implementation. Original plugin: **RisottoMan**. Model and project foundation: **PaRRot & Parrot Industry**. Additional thanks: **TayTay**. MIT license: [LICENSE.txt](LICENSE.txt).
