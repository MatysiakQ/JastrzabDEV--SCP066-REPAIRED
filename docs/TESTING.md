# SCP-066 2.0.1 — testy serwerowe / server checklist

Nie wykonano ich w kontenerze. Wymagają działającego serwera SCP:SL, właściwych zasobów oraz co najmniej dwóch klientów. Testy w tests/ dotyczą konfiguracji i audio, nie symulują pełnej gry.

## Przygotowanie

- Zanotuj wersje gry, EXILED, ProjectMER i AudioPlayerApi.
- Zostaw aktywny drugi plugin CustomRole, aby sprawdzić izolację sprzątania.
- Do testów losowego spawnu ustaw spawn_chance: 100; potem przywróć docelową wartość.

## Scenariusze i oczekiwane wyniki

| Test | Oczekiwany wynik |
|---|---|
| Nadanie przez RA | HP z configu od razu; brak leczenia po 0,5 s; 500 startowego HS domyślnie |
| Próba give na aktywnej CustomRole, również SCP-066 | Odmowa bez leczenia, odrodzenia lub utraty obecnej roli |
| Literówka w podkomendzie, np. giv | Błąd składni; rola i życie bez zmian |
| remove gracza bez SCP-066 | Odmowa bez zmiany roli |
| Poprawne remove | SCP-066 trafia do Spectator; brak modelu, audio i pozostałych efektów |
| LPM i jedzenie ciał | Brak obrażeń melee i rozpoczęcia konsumpcji |
| Zbicie HS bez SCP-049 | Regeneracja po 10 s, 10 HS/s do 500 przy domyślnym configu |
| Obrażenia podczas oczekiwania na regenerację | Odliczanie zaczyna się ponownie; zablokowane trafienie go nie resetuje |
| Regeneracja obok zawołania SCP-049 | Brak dodatkowej regeneracji zombie ponad ustalone tempo |
| Bloker HS | Regeneracja wstrzymana zgodnie z kontrolerem blokad |
| Brak schematu / zła nazwa / anulowany spawn MER | Widoczny model zombie i ostrzeżenie; brak niewidzialnej postaci |
| Usunięcie modelu w trakcie rundy | Powrót widocznego zombie |
| Właściciel, obserwator i późno dołączający klient | Ocena widoczności modelu, kamery, hitboxa i synchronizacji; obserwator nie jest objęty ukrywaniem schematu przed właścicielem |
| Brak folderu audio lub Beethovena | Atak nadal działa; komunikat o braku audio |
| Uszkodzony OGG obok poprawnych klipów | Pozostałe klipy działają; tylko uszkodzony jest odrzucony |
| Krótszy/dłuższy Beethoven niż atak | Zapętlenie/ucięcie dokładnie przy końcu lub przerwaniu ataku |
| Brak części Eric/Notes | Losowane wyłącznie dostępne nagrania |
| Zewnętrzny plugin anuluje Hurting | Brak utraty HP i brak Concussed od tego trafienia |
| Ofiara umiera lub zmienia rolę w pipeline trafienia | Brak Concussed na nowej postaci/obserwatorze |
| Atak przez ścianę, drzwi i między piętrami | Działa według odległości 3D; to zachowane zachowanie projektu |
| Śmierć, zmiana roli, rozłączenie w trakcie ataku | Atak i audio zatrzymane; brak dalszego obrażania lub leczenia nowej postaci |
| Zmiana roli przed opóźnionym teleportem | Brak przeniesienia nowej postaci |
| Szybkie odebranie i ponowne nadanie roli | Stary callback nie dotyka nowej sesji; brak podwójnych komponentów aktywnych |
| Koniec/restart rundy w trakcie ataku | Brak audio, ataku i callbacków poprzedniej rundy |
| Wyłączenie pluginu | Inne CustomRoles i bindy pozostają; własne sesje sprzątnięte; własny żywy gracz wraca do zwykłego zombie |
| Kilka włączeń i wyłączeń | Jedna grupa SS2 po włączeniu, brak wpisów po wyłączeniu; odświeżenie zmienionych OGG |
| Konflikt role_id lub *_keybind_id | Plugin zgłasza błąd inicjalizacji; nie używa ani nie usuwa cudzej roli/ustawień |
| Zablokowana zmiana roli przy give | Brak sukcesu RA; stan gracza nie powinien otrzymać statystyk SCP-066 |
| HCZ Armory i losowy pokój HCZ | Brak utknięcia lub wypadnięcia z mapy; pozycja +2,5 m wymaga oceny na używanej wersji mapy |

Przetestuj także dwie postacie nadane przez RA i docelowe lobby 6–14 osób. Kompilacja nie potwierdza balansu ani zgodności modelu z hitboxem.

## English

Run these checks on the target game/dependency versions with real clients. Verify cleanup isolation, rejected commands/ID conflicts, immediate HP, independent HS with damage delay and blockers, visible fallback for missing models, owner/spectator/late-join camera behavior, malformed/missing audio, attack/audio timing, cancelled hits, role/session changes, round transitions and repeated reloads. The HCZ teleport position and model collisions must be checked on the actual map. Regression tests do not cover these game/server behaviors.
