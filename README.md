# Tartarus Chroma

Windows-Anwendung zur sichtbaren Anzeige aktiver Makros auf Razer-Chroma-Geräten.

## Version 0.2

Neu:

- 20 globale Umschalt-Tastenkürzel
- beliebig viele Profile
- Profile erstellen, kopieren und löschen
- Namen für alle 20 Makro-Tasten
- dauerhafte Speicherung aller Zustände und Farben
- automatischer Verbindungsversuch zu Razer Chroma
- optionaler Windows-Autostart
- System-Tray mit Weiterbetrieb im Hintergrund
- alle Makro-Zustände bleiben nach einem Neustart erhalten

## Globale Tastenkürzel

- Makros 01–10: `Strg + Alt + 1` bis `Strg + Alt + 0`
- Makros 11–20: `Strg + Alt + Umschalt + 1` bis `Strg + Alt + Umschalt + 0`

Ein Tastenkürzel schaltet den jeweiligen Zustand unabhängig vom aktuell geöffneten Spiel oder Programm um.

## Makro-Namen

Mit einem Rechtsklick auf eine der 20 Schaltflächen lässt sich ihre Bezeichnung ändern.

## Profile

Jedes Profil besitzt:

- eigene Makro-Zustände
- eigene Bezeichnungen
- 20 getrennte Schalter

## Voraussetzungen

- Windows 11
- Razer Synapse mit Chroma Connect
- laufende Chroma-SDK-Dienste
- für lokale Builds: .NET 8 SDK

## Build

```powershell
./build-release.ps1
```

## Projektinhaberin

**Antonia Weiß**  
GitHub: **MissEthernia**
