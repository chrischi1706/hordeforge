# HordeForge

Vertical-Slice-Prototyp eines Fantasy-Tower-Defense-/Siedlungs-Strategiespiels für **Linux Desktop**.

> **Kern:** Begrenzte Bevölkerung zwischen Wirtschaft, Militär und Verteidigung aufteilen, während immer stärkere Horden angreifen.

---

## Voraussetzungen

| Tool | Zweck |
|------|--------|
| **Unity 6.3 LTS** | Spielen, Szene bauen, Linux-Build |
| **Unity Hub** (Flatpak auf Bazzite) | `flatpak install flathub com.unity.UnityHub` |
| **.NET SDK** (optional) | Tests und Simulation ohne Unity |

Die Spiellogik läuft auch **ohne Unity-Editor** über `./dev.sh test`.

---

## Unity-Projekt öffnen

1. Unity Hub starten, **Unity 6.3 LTS** installieren.
2. Projektordner hinzufügen: `~/Projects/HordeForge`
3. Beim ersten Öffnen legt der Editor automatisch Szene und Inhalte an  
   (Menü: **HordeForge → Projekt einrichten (Szene und Inhalte)** falls nötig).
4. Szene `Assets/HordeForge/Scenes/HordeForge.unity` öffnen und **Play** drücken.

### Steuerung im Spiel

| Eingabe | Aktion |
|---------|--------|
| WASD / Pfeiltasten | Kamera bewegen |
| Mausrad | Zoomen |
| Mittlere Maustaste | Karte schieben |
| Linksklick | Auswählen / Bauen / Truppe setzen |
| Rechtsklick / Esc | Modus abbrechen |
| Leertaste | Welle sofort starten |

---

## Entwicklung ohne Unity

```bash
# Einmalig: lokales .NET SDK (falls noch nicht vorhanden)
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel LTS --install-dir .tools/dotnet --no-path

# Core kompilieren
./dev.sh build

# Alle Tests (89+)
./dev.sh test

# Unity-Layer Syntax-Check (Stub-APIs)
./dev.sh unity

# Alles zusammen
./dev.sh all

# Simulation headless spielen (Balancing)
./dev.sh play 12 2026
```

Speicherstand (im Unity-Build): `Application.persistentDataPath/hordeforge-save.json`  
Unter Linux typischerweise `~/.config/unity3d/HordeForge/HordeForge/`.

---

## Architektur

```
Assets/HordeForge/Core/     → reine C#-Logik (testbar ohne Unity)
Assets/HordeForge/Unity/    → Kamera, HUD, Weltdarstellung
Assets/HordeForge/Editor/   → Szene- und Asset-Setup
Tools/                      → Standalone-Build, Tests, Playthrough-Bot
```

Balancewerte: `Assets/HordeForge/Core/Data/DefaultContent.cs`  
Roadmap: [ROADMAP.md](ROADMAP.md)

---

## Linux-Build

In Unity: **File → Build Settings → Linux → Build**  
Zielplattform muss **Linux Standalone** sein. Keine Windows-Pfade oder VS-Abhängigkeiten.

---

## MVP-Checkliste

Der Prototyp deckt ab:

- Bevölkerung (Gesamtzahl, frei, Echtzeit-Zuweisung)
- Ressourcen und Produktionsketten (Holz → Waffen)
- Gebäude platzieren und besetzen
- Truppen erstellen, erweitern, auflösen, platzieren
- Verteidigungsanlagen mit Besatzung und Munition
- Horden mit Budgetgenerator und Elitewellen
- Globales Inventar (Items aus Elitewellen)
- Save/Load (JSON)
- HUD mit Bevölkerung, Ressourcen, Welle, Gebäude-UI, Truppen, Inventar
