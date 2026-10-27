# SCP-066 Repaired

> 🇵🇱 [Polski](#polski) | 🇬🇧 [English](#english)

---

# Polski

![Version](https://img.shields.io/badge/wersja-2.0.0-blue)
![EXILED](https://img.shields.io/badge/EXILED-compatible-brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

Całkowicie przebudowana i naprawiona wersja pluginu [SCP-066 autorstwa RisottoMan](https://github.com/RisottoMan/SCP-066), dostosowana do najnowszego silnika EXILED (9.0+) i ProjectMER.

> 💙 **Szczególne podziękowania dla:**
> * **[RisottoMan](https://github.com/RisottoMan)** – za oryginalny plugin,
> * **PaRRot & Parrot Industry** – za model i podstawę projektu, na której bazuje ta wersja,

---

## ✨ Co zostało zmienione w wersji 2.0.0 (Full Rebuild)
- **Natywne Menu Keybindów (SS2):** Usunięto przestarzałe komendy czatowe. Umiejętności binduje się teraz bezpośrednio w menu gry (Ustawienia Serwera), co omija błędy z `ClientSettings` w EXILED.
- **Fail-safe Audio:** Brak plików na serwerze nie blokuje już zadawania obrażeń atakiem Beethovena.
- **Blokada zwykłego ataku:** Zablokowano możliwość zadawania obrażeń "z łapy" (LPM).
- **Optymalizacja Hintów:** Teksty odświeżają się co 1 sekundę, eliminując obciążenie serwera.
- Zmiana struktury projektu na `.csproj` SDK-style (kompatybilność z .NET 4.8.1).
- Zmiana API audio na nowsze `AudioPlayerApi`.

---

## Wymagania
- [EXILED](https://github.com/ExMod-Team/EXILED) (najnowsza wersja 9.0+)
- [Exiled.CustomRoles](https://github.com/ExMod-Team/EXILED) (dołączone do EXILED)
- [ProjectMER](https://github.com/Michal78900/MapEditorReborn)
- [AudioPlayerApi](https://github.com/ExMod-Team/AudioPlayerApi) (zamiast starego SCPSLAudioApi)

---

## Instalacja

### 1. Plugin
Wrzuć skompilowany plik `Scp066.dll` do folderu:

    EXILED/Plugins/

### 2. Schematic (model)
Wrzuć folder ze schematem `Scp066` do folderu MapEditorReborn:

    EXILED/Configs/ProjectMER/Schematics/Scp066/

### 3. Audio
Wrzuć pliki dźwiękowe do folderu:

    EXILED/Configs/Plugins/scp066/Audio/

Wymagane pliki (format `.ogg`, **mono**, **48kHz**):

    Beethoven.ogg   — główny atak symfoniczny
    Eric1.ogg
    Eric2.ogg
    Eric3.ogg       — dźwięki Erica (losowane)
    Notes1.ogg
    Notes2.ogg
    Notes3.ogg
    Notes4.ogg
    Notes5.ogg
    Notes6.ogg      — dźwięki muzyczne (losowane)

---

## Konfiguracja
Po pierwszym uruchomieniu serwera config pojawi się automatycznie w:

    EXILED/Configs/config_gameplay.yml


| Opcja | Domyślnie | Opis |
|---|---|---|
| `is_enabled` | `true` | Włącza/wyłącza plugin |
| `debug` | `false` | Logi debugowania w konsoli |
| `schematic_name` | `Scp066` | Nazwa schematu z MapEditorReborn |
| `spawn_chance` | `10` | Szansa spawnu w % (10 = 10%) |
| `volume` | `0.8` | Głośność dźwięków (0.0 - 1.0) |
| `attack_duration` | `24` | Czas trwania ataku Beethovena (sekundy) |
| `damage` | `5` | Obrażenia na sekundę podczas ataku |
| `attack_radius` | `8` | Zasięg ataku i słyszalności dźwięku |
| `cooldown_eric` | `10` | Cooldown dźwięku Eric (sekundy) |
| `cooldown_music` | `15` | Cooldown dźwięków Notes (sekundy) |
| `cooldown_attack` | `45` | Cooldown ataku Beethovena (sekundy) |

---

## Jak działa
- Na starcie każdej rundy plugin rzuca kością — **10% szansy**, że SCP-066 w ogóle się pojawi.
- Wybiera **losowego SCPa** z puli (pomija SCP-079 i graczy, którzy już mają inną CustomRole).
- Wybrany gracz spawnuje się w **HCZ Armory**.
- SCP-066 ma **2000 HP**, **500 HumeShield** i zmniejszoną skalę postaci.
- SCP-066 nie zadaje obrażeń zwykłym atakiem (LPM) – używa do tego wyłącznie Beethovena.

### Umiejętności (Natywne menu w grze)
Zrezygnowano z wpisywania komend na czacie. Plugin używa teraz menu ustawień gry:
1. Podczas gry wciśnij klawisz `ESC` i wybierz **Ustawienia specyficzne dla serwera (Server-specific)**.
2. W sekcji **SCP-066** przypisz dowolne klawisze do poniższych akcji:
   - **Atak: Beethoven** (Aktywuje atak obszarowy)
   - **Dźwięk: Eric** (Odtwarza losowy dźwięk Erica)
   - **Dźwięk: Notes** (Odtwarza losową melodię)

### Atak Beethovena
- Odtwarza `Beethoven.ogg` słyszalny w zasięgu ataku.
- Przez **24 sekundy** zadaje **5 obrażeń/s** wszystkim graczom w zasięgu.
- Trafieni gracze dostają efekt **Concussed** na 2 sekundy.
- Działa prawidłowo nawet w przypadku braku plików audio na serwerze!
- Cooldown: **45 sekund**.

---

## Komendy (Remote Admin)
| Komenda | Opis |
|---|---|
| `scp066 give <nick/id>` | Nadaje rolę SCP-066 graczowi |
| `scp066 remove <nick/id>` | Odbiera rolę SCP-066 graczowi |

> Wymagane uprawnienie: `scp066.admin`

---

## Twórcy
**Matysiak** — wersja 2.0.0 (Full Rebuild & SS2 Menu Update)

Oparty na oryginalnym pluginie **[SCP-066 by RisottoMan](https://github.com/RisottoMan/SCP-066)**
Ogromne podziękowania dla: **RisottoMan**, **PaRRot & Parrot Industry**, **TayTay**.

---
---

# English

![Version](https://img.shields.io/badge/version-2.0.0-blue)
![EXILED](https://img.shields.io/badge/EXILED-compatible-brightgreen)
![License](https://img.shields.io/badge/license-MIT-green)

A completely rebuilt and repaired version of the [SCP-066 plugin by RisottoMan](https://github.com/RisottoMan/SCP-066), updated for the latest EXILED (9.0+) and ProjectMER engine.

> 💙 **Special thanks to:**
> * **[RisottoMan](https://github.com/RisottoMan)** – for the original plugin,
> * **PaRRot & Parrot Industry** – for the model and the project foundation this version is based on,

---

## ✨ What changed in version 2.0.0 (Full Rebuild)
- **Native Keybind Menu (SS2):** Removed obsolete chat commands. Abilities are now bound directly in the game's Server-Specific Settings menu, completely bypassing EXILED's faulty `ClientSettings`.
- **Audio Fail-safe:** Missing audio files on the server no longer prevent the Beethoven attack from dealing damage.
- **Basic Attack Blocked:** Prevented the ability to deal standard melee damage (LMB).
- **Hint Optimization:** Hints now refresh exactly once per second, eliminating server strain.
- Project structure rebuilt to SDK-style `.csproj` (maintaining `.NET 4.8.1` compatibility).
- Audio API switched to the newer `AudioPlayerApi`.

---

## Requirements
- [EXILED](https://github.com/ExMod-Team/EXILED) (latest version 9.0+)
- [Exiled.CustomRoles](https://github.com/ExMod-Team/EXILED) (included with EXILED)
- [ProjectMER](https://github.com/Michal78900/MapEditorReborn)
- [AudioPlayerApi](https://github.com/ExMod-Team/AudioPlayerApi) (instead of the old SCPSLAudioApi)

---

## Installation

### 1. Plugin
Drop the compiled `Scp066.dll` into:

    EXILED/Plugins/

### 2. Schematic (model)
Place the `Scp066` schematic folder in the MapEditorReborn directory:

    EXILED/Configs/ProjectMERSchematics/Scp066/

### 3. Audio
Place audio files in:

    EXILED/Configs/Plugins/scp066/Audio/


Required files (`.ogg` format, **mono**, **48kHz**):

    Beethoven.ogg   — main symphony attack sound
    Eric1.ogg
    Eric2.ogg
    Eric3.ogg       — Eric sounds (randomly picked)
    Notes1.ogg
    Notes2.ogg
    Notes3.ogg
    Notes4.ogg
    Notes5.ogg
    Notes6.ogg      — music notes sounds (randomly picked)

---

## Configuration
After the first server launch, the config will appear automatically in:

    EXILED/Configs/config_gameplay.yml


| Option | Default | Description |
|---|---|---|
| `is_enabled` | `true` | Enables/disables the plugin |
| `debug` | `false` | Debug logs in console |
| `schematic_name` | `Scp066` | Schematic name from MapEditorReborn |
| `spawn_chance` | `10` | Spawn chance in % (10 = 10%) |
| `volume` | `0.8` | Sound volume (0.0 - 1.0) |
| `attack_duration` | `24` | Duration of Beethoven attack (seconds) |
| `damage` | `5` | Damage per second during attack |
| `attack_radius` | `8` | Attack and audio range |
| `cooldown_eric` | `10` | Eric sound cooldown (seconds) |
| `cooldown_music` | `15` | Notes sound cooldown (seconds) |
| `cooldown_attack` | `45` | Beethoven attack cooldown (seconds) |

---

## How it works
- At the start of each round, the plugin rolls the dice — **10% chance** that SCP-066 will appear at all.
- Picks **one random SCP** from the pool (skips SCP-079 and players who already have another CustomRole).
- The chosen player spawns at **HCZ Armory**.
- SCP-066 has **2000 HP**, **500 HumeShield**, and a reduced player scale.
- SCP-066 cannot deal damage with standard melee attacks (LMB) – the Beethoven ability is the only way to inflict damage.

### Abilities (Native Game Menu)
Chat commands are a thing of the past. The plugin now uses the game's built-in settings menu:
1. Press `ESC` while in-game and navigate to **Server-specific settings**.
2. Under the **SCP-066** section, bind any keys you want to the following actions:
   - **Attack: Beethoven** (Activates AoE damage attack)
   - **Sound: Eric** (Plays a random Eric sound)
   - **Sound: Notes** (Plays a random music note)

### Beethoven Attack
- Plays `Beethoven.ogg` audible within attack range.
- For **24 seconds** deals **5 damage/s** to all players in range.
- Hit players receive the **Concussed** effect for 2 seconds.
- Works perfectly even if the audio folder is missing on the server!
- Cooldown: **45 seconds**.

---

## Commands (Remote Admin)
| Command | Description |
|---|---|
| `scp066 give <nick/id>` | Grants the SCP-066 role to a player |
| `scp066 remove <nick/id>` | Removes the SCP-066 role from a player |

> Required permission: `scp066.admin`

---

## Authors
**Matysiak** — version 2.0.0 (Full Rebuild & SS2 Menu Update)

Based on the original **[SCP-066 by RisottoMan](https://github.com/RisottoMan/SCP-066)**
Huge thanks to: **RisottoMan**, **PaRRot & Parrot Industry**, **TayTay**.
