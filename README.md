# Tartarus Chroma

Windows-Anwendung zur Anzeige aktiver Makros auf Razer-Chroma-Geräten.

## Aktueller Entwicklungsstand

Die erste vollständige Projektbasis enthält:

- WinForms-Oberfläche für Windows 11
- frei skalierbares Fenster
- Verbindung zur offiziellen lokalen Razer-Chroma-REST-Schnittstelle
- Heartbeat-Verbindung
- Steuerung des Tartarus als 4×5-Raster
- 20 einzeln umschaltbare Makro-Zustände
- frei wählbare Grundfarbe
- frei wählbare Aktivfarbe
- statischer Farbtest für eine Chroma-Tastatur
- sichtbares Diagnoseprotokoll
- saubere Freigabe der Beleuchtung an Synapse
- GitHub-Actions-Build für eine eigenständige Windows-EXE

## Voraussetzungen

- Windows 11
- Razer Synapse mit Chroma Connect
- laufende Chroma-SDK-Dienste
- für lokale Builds: .NET 8 SDK

## Lokaler Build

```powershell
./build-release.ps1
```

Danach liegt die EXE unter:

```text
artifacts/win-x64/TartarusChroma.exe
```

## Bedienung

1. Anwendung starten.
2. **Verbinden** anklicken.
3. Eine oder mehrere der 20 Tasten in der Oberfläche aktivieren.
4. Das Tartarus erhält ein 4×5-Farbraster:
   - Grundfarbe = inaktiv
   - Aktivfarbe = aktiv
5. Über **Beleuchtung freigeben** übernimmt Synapse wieder.

## Nächste Entwicklungsschritte

- konfigurierbare Tastenzuordnung
- globale Hotkeys als Makro-Auslöser
- Speichern und Laden von Profilen
- automatischer Start mit Windows
- System-Tray
- Installer und Deinstaller
- Erkennung laufender Makro-Zustände über konfigurierbare Toggle-Auslöser
- BlackWidow-Einzelbeleuchtung

## Technische Grundlage

Die Anwendung nutzt die offizielle lokale Razer-Chroma-REST-Schnittstelle. Die Chroma-Sitzung wird registriert, durch Heartbeats am Leben gehalten und beim Beenden wieder freigegeben.

## Projektinhaberin

**Antonia Weiß**  
GitHub: **MissEthernia**
