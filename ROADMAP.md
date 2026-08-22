# HordeForge – Technische Roadmap

Vertical-Slice-Prototyp eines Fantasy-Tower-Defense-/Siedlungs-Strategiespiels.

**Kernsatz, an dem sich jede Designentscheidung messen lassen muss:**

> Ich muss meine begrenzte Bevölkerung zwischen Wirtschaft, Militär und Verteidigung
> aufteilen, während immer stärkere Horden meine Siedlung angreifen.

---

## Zielumgebung

| Punkt          | Entscheidung                                                     |
| -------------- | ---------------------------------------------------------------- |
| OS             | Linux (Bazzite / Fedora Silverblue-Basis, immutable)              |
| Unity          | Unity 6.3 LTS (Support bis 12/2027; 6.0 LTS endet 10/2026)        |
| Render Pipeline| Built-in – kein URP-Setup nötig, hand-authorbar, zero dependencies|
| Input          | Legacy Input Manager – kein Package-Setup, out of the box nutzbar |
| UI             | uGUI, komplett zur Laufzeit aus Code aufgebaut                    |
| Serialisierung | Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`, Unity-offiziell)|
| Zielplattform  | Linux Desktop Standalone, Maus + Tastatur                         |

Es werden keine proprietären Plugins und keine Windows-only Tools verwendet.
Alle Pfade sind relativ bzw. laufzeitermittelt (`Application.persistentDataPath`).

---

## Architektur-Grundentscheidung

Das Projekt ist in **zwei Assemblies** geteilt:

```
Assets/HordeForge/Core   →  HordeForge.Core    reines C#, KEINE UnityEngine-Referenz
Assets/HordeForge/Unity  →  HordeForge.Unity   MonoBehaviours, Rendering, Input, UI
```

Der Grund ist nicht Dogma, sondern Testbarkeit: `HordeForge.Core` lässt sich mit dem
normalen .NET SDK kompilieren und mit NUnit testen – ohne laufenden Unity-Editor.
Die gesamte Spiellogik (Ökonomie, Bevölkerung, Produktion, Truppen, Kampf, Wellen,
Inventar, Save) ist damit automatisiert prüfbar, und der Unity-Layer bleibt eine
dünne Darstellungs- und Eingabeschicht.

Werkzeugkette dafür liegt in `Tools/`:

```
Tools/HordeForge.Core.Standalone   kompiliert die Core-Sourcen als netstandard2.1
Tools/HordeForge.Core.Tests        NUnit-Tests gegen die Core-Logik
```

Aufruf über `./dev.sh test` (nutzt das projektlokale SDK in `.tools/dotnet`).

---

## Datenorientierung

Balancewerte stehen **nicht** verstreut im Code. Alle Definitionen sind POCOs in
`HordeForge.Core.Data` und werden an genau einer Stelle befüllt: `DefaultContent`.

* `ResourceDefinition` – Kategorie, Anzeigename, Nahrungswert
* `BuildingDefinition` – Baukosten, Arbeiterplätze, HP, Rezepte, Kampfwerte
* `RecipeDefinition`  – Inputs, Outputs, Zykluszeit
* `UnitDefinition`    – HP, Schaden, Reichweite, Munition, Rekrutierungskosten
* `EnemyDefinition`   – HP, Tempo, Schaden, Budgetkosten
* `ItemDefinition`    – Anzeigename
* `WaveSettings`      – Budgetformel, Elitechance, Vorbereitungszeit

Da alle Definitionen `[Serializable]` POCOs sind, können sie unverändert in
ScriptableObjects eingebettet werden – der Unity-Layer bringt `GameContentAsset`
plus einen Editor-Menüpunkt, der die Assets aus `DefaultContent` erzeugt. Damit ist
Punkt 39/40 erfüllt, ohne dass YAML-Assets von Hand geschrieben werden müssen.

---

## Systeme

| System              | Assembly | Verantwortung                                              |
| ------------------- | -------- | ---------------------------------------------------------- |
| `ResourceLedger`    | Core     | Bestände, Einzahlen/Abbuchen, Kapazität                     |
| `PopulationSystem`  | Core     | Gesamtzahl, freie Bürger, Nahrungsverbrauch, Hungerwarnung  |
| `BuildingSystem`    | Core     | Platzierung, Baukosten, HP, Zerstörung, Arbeiterzuweisung   |
| `ProductionSystem`  | Core     | Rezeptzyklen, Arbeiterskalierung, Input-/Output-Handling    |
| `SquadSystem`       | Core     | Truppen erstellen/erweitern/auflösen, Platzierung           |
| `CombatSystem`      | Core     | Zielsuche „nächster Gegner", Schaden, Munitionsverbrauch     |
| `WaveSystem`        | Core     | Phasen, Wellenzähler, Budget, Elite-Roll, Siegbedingung     |
| `HordeGenerator`    | Core     | Gegnerzusammensetzung aus Budget, Spawnrichtungen           |
| `EnemySystem`       | Core     | Gegnerinstanzen, Bewegung, Zielwahl, Angriff                |
| `InventorySystem`   | Core     | Globales Item-Inventar mit fester Slotzahl                  |
| `SaveSystem`        | Core     | Snapshot/Restore des kompletten Spielstands als JSON        |
| `GameSimulation`    | Core     | Verdrahtet die Systeme, definiert die Tick-Reihenfolge      |
| Unity-Layer         | Unity    | Szene, Kamera, Map, Sichtbarkeit, HUD, Eingabe              |

`GameSimulation` ist bewusst kein Monolith – sie hält Referenzen und legt nur die
Reihenfolge des Ticks fest.

---

## Bewusst NICHT im MVP

Multiplayer, Online, Accounts, Cloud-Saves, Bürger-Biografien (Name/Alter/Geschlecht),
Krankheit/Verletzung/Tod, Diplomatie, gegnerische Wirtschaft oder Basen, offene Welt,
Quests, Item-Stats/Raritäten/Buffs, Helden, Magie, großer Techbaum, komplexe
Zielprioritäten.

Zwei Regeln, die daraus konkret folgen:

* **Bürger sterben nicht.** Fällt eine Einheit oder ein besetztes Gebäude aus, kehrt
  die Bevölkerung als freie Bürger zurück; verloren geht nur die Ausrüstung.
* **Zielwahl ist immer „nächster Gegner"** – keine Priorisierung, keine Taktik-KI.

---

## Entwicklungsreihenfolge und Status

| # | Schritt                                  | Status |
| - | ---------------------------------------- | ------ |
| 1 | Projekt-Setup, Toolchain, Roadmap         | erledigt |
| 2 | Core-Fundament, Definitionen, Content     | erledigt |
| 3 | Ressourcen-/Economy-System                | erledigt |
| 4 | Bevölkerung + Echtzeit-Zuweisung          | erledigt |
| 5 | Produktionsgebäude                        | erledigt |
| 6 | Ressourcenketten                          | erledigt |
| 7 | Gebäudeplatzierung                        | erledigt |
| 8 | Militäreinheiten                          | erledigt |
| 9 | Truppensystem                             | erledigt |
|10 | Truppenplatzierung                        | erledigt |
|11 | Kampfsystem                               | erledigt |
|12 | Gegner                                    | erledigt |
|13 | Hordensystem + Budgetgenerator            | erledigt |
|14 | Verteidigungsanlagen                      | erledigt |
|15 | Elitewellen                               | erledigt |
|16 | Inventar und Items                        | erledigt |
|17 | HUD/UI                                    | erledigt |
|18 | Save/Load                                 | erledigt |
|19 | Balancing                                 | teilweise (Bot ~4–5 Wellen) |
|20 | Linux-Build                               | vorbereitet (`build-linux.sh`) |

Nach jedem abgeschlossenen System gilt: kompilieren, Tests grün, dann weiter.
