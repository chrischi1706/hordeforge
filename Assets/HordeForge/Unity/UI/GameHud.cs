using System.Collections.Generic;
using System.Text;
using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Items;
using HordeForge.Core.Military;
using HordeForge.Core.Waves;
using UnityEngine;
using UnityEngine.UI;

namespace HordeForge.Unity.UI
{
    /// <summary>
    /// Das komplette HUD. Statische Bereiche entstehen einmal beim Start, dynamische
    /// Listen werden nur neu aufgebaut, wenn sich ihr Inhalt tatsaechlich aendert –
    /// sonst wuerde jeder Klick im selben Bild wieder zerstoert.
    /// </summary>
    public sealed class GameHud
    {
        /// <summary>Reihenfolge der Rohstoffanzeige, wie im Konzept vorgegeben.</summary>
        private static readonly ResourceType[] GoodsOrder =
        {
            ResourceType.Wood, ResourceType.Planks, ResourceType.Stone, ResourceType.CutStone,
            ResourceType.Ore, ResourceType.Metal, ResourceType.Grain, ResourceType.Flour,
            ResourceType.Bread, ResourceType.Fish, ResourceType.Cotton, ResourceType.Thread,
            ResourceType.Cloth, ResourceType.Wool
        };

        private static readonly ResourceType[] EquipmentOrder =
        {
            ResourceType.Tool, ResourceType.Bow, ResourceType.Spear, ResourceType.Sword,
            ResourceType.Shield, ResourceType.Armor, ResourceType.Arrows, ResourceType.SiegeAmmo
        };

        private static readonly UnitType[] RecruitableUnits =
        {
            UnitType.Archer, UnitType.Spearman, UnitType.Swordsman
        };

        private const float TopBarHeight = 130f;
        private const float BottomBarHeight = 140f;

        private readonly GameSimulation _sim;
        private readonly PlayerController _controller;
        private readonly System.Action _onNewGame;
        private readonly System.Action _onSave;
        private readonly System.Action _onLoad;

        private readonly StringBuilder _builder = new StringBuilder();

        private Text _populationText;
        private Text _goodsText;
        private Text _equipmentText;
        private Text _waveText;
        private Text _waveDetailText;
        private Text _statusText;
        private Text _logText;
        private Button _startWaveButton;

        private RectTransform _inspectorContent;
        private RectTransform _squadListContent;
        private RectTransform _inventoryContent;
        private Text _inventoryHeading;

        private GameObject _gameOverPanel;

        // Merker, damit dynamische Listen nicht in jedem Bild neu gebaut werden.
        private string _inspectorKey = "?";
        private int _squadListSignature = -1;
        private int _inventorySignature = -1;

        private Text _inspectorDynamicText;
        private Building _inspectedBuilding;
        private Squad _inspectedSquad;

        public GameHud(
            GameSimulation simulation,
            PlayerController controller,
            Transform root,
            System.Action onNewGame,
            System.Action onSave,
            System.Action onLoad)
        {
            _sim = simulation;
            _controller = controller;
            _onNewGame = onNewGame;
            _onSave = onSave;
            _onLoad = onLoad;

            Canvas canvas = UiBuilder.CreateCanvas(root);
            BuildTopBar(canvas.transform);
            BuildBuildMenu(canvas.transform);
            BuildRightColumn(canvas.transform);
            BuildBottomBar(canvas.transform);
            BuildGameOverPanel(canvas.transform);
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private void BuildTopBar(Transform parent)
        {
            RectTransform bar = UiBuilder.CreatePanel(
                parent, "TopBar", UiBuilder.PanelColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -TopBarHeight), new Vector2(0f, 0f));

            // Zwei Zeilen: oben Bevoelkerung und Wellenstatus, darunter die Bestaende.
            // Alles in Pixeln am oberen Rand verankert, damit die Anordnung bei jeder
            // Fenstergroesse vorhersagbar bleibt.
            Vector2 top = new Vector2(0f, 1f);

            _populationText = UiBuilder.CreateStretchedText(
                bar, "Population", "", 20, TextAnchor.MiddleLeft, UiBuilder.TextColor,
                top, new Vector2(0.45f, 1f),
                new Vector2(16f, -34f), new Vector2(0f, -8f), FontStyle.Bold);

            _waveText = UiBuilder.CreateStretchedText(
                bar, "Wave", "", 19, TextAnchor.MiddleRight, UiBuilder.AccentColor,
                new Vector2(0.45f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -32f), new Vector2(-200f, -8f), FontStyle.Bold);

            _waveDetailText = UiBuilder.CreateStretchedText(
                bar, "WaveDetail", "", 14, TextAnchor.MiddleRight, UiBuilder.MutedColor,
                new Vector2(0.45f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -54f), new Vector2(-200f, -34f));

            // Vierzehn Warenposten passen nicht in eine Zeile – hier ist Umbruch
            // eingeplant, deshalb der hoehere Bereich.
            _goodsText = UiBuilder.CreateStretchedText(
                bar, "Goods", "", 14, TextAnchor.UpperLeft, UiBuilder.TextColor,
                top, new Vector2(1f, 1f),
                new Vector2(16f, -100f), new Vector2(-16f, -58f));

            _equipmentText = UiBuilder.CreateStretchedText(
                bar, "Equipment", "", 14, TextAnchor.UpperLeft, UiBuilder.MutedColor,
                top, new Vector2(1f, 1f),
                new Vector2(16f, -124f), new Vector2(-16f, -102f));

            _startWaveButton = UiBuilder.CreateButton(
                bar, "StartWave", "Welle starten", OnStartWave, 34f, 15);
            UiBuilder.Stretch(
                _startWaveButton.GetComponent<RectTransform>(),
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-186f, -52f), new Vector2(-16f, -10f));
        }

        private void BuildBuildMenu(Transform parent)
        {
            RectTransform panel = UiBuilder.CreatePanel(
                parent, "BuildPanel", UiBuilder.PanelColor,
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, BottomBarHeight + 6f), new Vector2(286f, -(TopBarHeight + 6f)));

            UiBuilder.CreateStretchedText(
                panel, "Title", "BAUEN", 17, TextAnchor.MiddleLeft, UiBuilder.AccentColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -32f), new Vector2(-12f, -4f), FontStyle.Bold);

            RectTransform content = UiBuilder.CreateScrollList(
                panel, "Buildings",
                Vector2.zero, Vector2.one,
                new Vector2(0f, 0f), new Vector2(0f, -34f));

            AddBuildSection(content, "Rohstoffe", BuildingCategory.Gathering);
            AddBuildSection(content, "Verarbeitung", BuildingCategory.Production);
            AddBuildSection(content, "Verteidigung", BuildingCategory.Defense);
            AddBuildSection(content, "Befestigung", BuildingCategory.Fortification);
            AddBuildSection(content, "Sonstiges", BuildingCategory.Support);
        }

        private void AddBuildSection(RectTransform content, string caption, BuildingCategory category)
        {
            BuildingDefinition[] definitions = _sim.Config.Buildings;

            bool headingAdded = false;
            for (int i = 0; i < definitions.Length; i++)
            {
                BuildingDefinition definition = definitions[i];
                if (definition.Category != category)
                {
                    continue;
                }

                if (!headingAdded)
                {
                    UiBuilder.CreateListHeading(content, caption.ToUpperInvariant());
                    headingAdded = true;
                }

                BuildingType type = definition.Type;
                UiBuilder.CreateButton(
                    content, "Build_" + type, definition.DisplayName,
                    () => _controller.BeginBuildingPlacement(type), 30f, 14);

                UiBuilder.CreateListText(
                    content, "Cost_" + type,
                    Texts.Costs(definition.BuildCost, _sim.Config)
                    + (definition.WorkerSlots > 0 ? "  ·  " + definition.WorkerSlots + " Arbeiter" : ""),
                    12, UiBuilder.MutedColor, 18f);
            }
        }

        private void BuildRightColumn(Transform parent)
        {
            RectTransform column = UiBuilder.CreatePanel(
                parent, "RightColumn", new Color(0f, 0f, 0f, 0f),
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-372f, BottomBarHeight + 6f), new Vector2(0f, -(TopBarHeight + 6f)));

            // Auswahl
            RectTransform inspector = UiBuilder.CreatePanel(
                column, "Inspector", UiBuilder.PanelColor,
                new Vector2(0f, 0.52f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));

            UiBuilder.CreateStretchedText(
                inspector, "Title", "AUSWAHL", 17, TextAnchor.MiddleLeft, UiBuilder.AccentColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -32f), new Vector2(-12f, -4f), FontStyle.Bold);

            _inspectorContent = UiBuilder.CreateScrollList(
                inspector, "InspectorContent",
                Vector2.zero, Vector2.one,
                new Vector2(0f, 0f), new Vector2(0f, -34f));

            // Truppen
            RectTransform squads = UiBuilder.CreatePanel(
                column, "Squads", UiBuilder.PanelColor,
                new Vector2(0f, 0.21f), new Vector2(1f, 0.51f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));

            UiBuilder.CreateStretchedText(
                squads, "Title", "TRUPPEN", 17, TextAnchor.MiddleLeft, UiBuilder.AccentColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -32f), new Vector2(-140f, -4f), FontStyle.Bold);

            Button newSquad = UiBuilder.CreateButton(
                squads, "NewSquad", "Neue Truppe", _controller.BeginSquadPlacement, 26f, 13);
            UiBuilder.Stretch(
                newSquad.GetComponent<RectTransform>(),
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-136f, -32f), new Vector2(-10f, -6f));

            _squadListContent = UiBuilder.CreateScrollList(
                squads, "SquadList",
                Vector2.zero, Vector2.one,
                new Vector2(0f, 0f), new Vector2(0f, -34f));

            // Inventar
            RectTransform inventory = UiBuilder.CreatePanel(
                column, "Inventory", UiBuilder.PanelColor,
                new Vector2(0f, 0f), new Vector2(1f, 0.20f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));

            _inventoryHeading = UiBuilder.CreateStretchedText(
                inventory, "Title", "INVENTAR", 17, TextAnchor.MiddleLeft, UiBuilder.AccentColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -30f), new Vector2(-12f, -4f), FontStyle.Bold);

            _inventoryContent = UiBuilder.CreateScrollList(
                inventory, "InventoryList",
                Vector2.zero, Vector2.one,
                new Vector2(0f, 0f), new Vector2(0f, -32f),
                2f);
        }

        private void BuildBottomBar(Transform parent)
        {
            RectTransform bar = UiBuilder.CreatePanel(
                parent, "BottomBar", UiBuilder.PanelColor,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, BottomBarHeight));

            _logText = UiBuilder.CreateStretchedText(
                bar, "Log", "", 14, TextAnchor.LowerLeft, UiBuilder.TextColor,
                new Vector2(0f, 0f), new Vector2(0.62f, 1f),
                new Vector2(16f, 8f), new Vector2(0f, -30f));

            _statusText = UiBuilder.CreateStretchedText(
                bar, "Status", "", 16, TextAnchor.UpperLeft, UiBuilder.AccentColor,
                new Vector2(0f, 1f), new Vector2(0.62f, 1f),
                new Vector2(16f, -28f), new Vector2(0f, -4f));

            UiBuilder.CreateStretchedText(
                bar, "Hints",
                "WASD / Pfeile bewegen · Mausrad zoomt · mittlere Maustaste schiebt\n"
                + "Linksklick waehlt · Rechtsklick oder Esc bricht ab · Leertaste startet die Welle",
                13, TextAnchor.LowerRight, UiBuilder.MutedColor,
                new Vector2(0.62f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 8f), new Vector2(-16f, -46f));

            RectTransform row = UiBuilder.CreateRect(bar, "Actions");
            UiBuilder.Stretch(row,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-400f, -40f), new Vector2(-16f, -6f));

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            UiBuilder.CreateButton(row, "Save", "Speichern", () => _onSave(), 30f, 14);
            UiBuilder.CreateButton(row, "Load", "Laden", () => _onLoad(), 30f, 14);
            UiBuilder.CreateButton(row, "NewGame", "Neues Spiel", () => _onNewGame(), 30f, 14);
        }

        private void BuildGameOverPanel(Transform parent)
        {
            RectTransform panel = UiBuilder.CreatePanel(
                parent, "GameOver", new Color(0.06f, 0.05f, 0.07f, 0.94f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-260f, -110f), new Vector2(260f, 110f));

            UiBuilder.CreateStretchedText(
                panel, "Title", "DIE SIEDLUNG IST GEFALLEN", 26,
                TextAnchor.MiddleCenter, UiBuilder.AlertColor,
                new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                new Vector2(16f, 0f), new Vector2(-16f, -16f), FontStyle.Bold);

            UiBuilder.CreateStretchedText(
                panel, "Hint", "Das Rathaus wurde zerstoert.", 16,
                TextAnchor.MiddleCenter, UiBuilder.TextColor,
                new Vector2(0f, 0.35f), new Vector2(1f, 0.55f),
                new Vector2(16f, 0f), new Vector2(-16f, 0f));

            Button restart = UiBuilder.CreateButton(
                panel, "Restart", "Neues Spiel", () => _onNewGame(), 40f, 17);
            UiBuilder.Stretch(
                restart.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-110f, 22f), new Vector2(110f, 62f));

            _gameOverPanel = panel.gameObject;
            _gameOverPanel.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Aktualisierung
        // ------------------------------------------------------------------

        public void Refresh()
        {
            RefreshTopBar();
            RefreshInspector();
            RefreshSquadList();
            RefreshInventory();
            RefreshLog();

            _statusText.text = _controller.StatusMessage ?? string.Empty;
            _gameOverPanel.SetActive(_sim.IsGameOver);
            UiBuilder.SetInteractable(_startWaveButton, _sim.Waves.Phase == WavePhase.Preparation);
        }

        private void RefreshTopBar()
        {
            _builder.Length = 0;
            _builder.Append("Bevoelkerung: ").Append(_sim.Population.Total);
            _builder.Append("    Frei: ").Append(_sim.Population.Free);
            _builder.Append("    Nahrung: ").Append(_sim.Resources.FoodStock);

            if (_sim.Population.IsStarving)
            {
                _builder.Append("   <color=#F97B40>NAHRUNG KNAPP</color>");
            }

            _populationText.text = _builder.ToString();

            _goodsText.text = BuildResourceLine(GoodsOrder);
            _equipmentText.text = BuildResourceLine(EquipmentOrder);

            WavePhase phase = _sim.Waves.Phase;
            _builder.Length = 0;
            _builder.Append("Welle ").Append(_sim.Waves.WaveNumber);
            _builder.Append("  ·  ").Append(Texts.Describe(phase));

            if (phase == WavePhase.Preparation)
            {
                _builder.Append(" ").Append(Texts.Timer(_sim.Waves.PreparationRemaining));
            }

            _waveText.text = _builder.ToString();

            _builder.Length = 0;
            if (phase == WavePhase.Active)
            {
                _builder.Append("Budget: ").Append(_sim.Waves.CurrentBudget);
                _builder.Append("  ·  Elite: ").Append(_sim.Waves.IsEliteWave ? "JA" : "nein");
                _builder.Append("  ·  Gegner verbleibend: ").Append(_sim.Waves.EnemiesRemaining);
            }
            else if (phase == WavePhase.Preparation)
            {
                _builder.Append("Budget: ").Append(_sim.Waves.UpcomingBudget);
                _builder.Append("  ·  Elite: ?");
            }

            _waveDetailText.text = _builder.ToString();
        }

        private string BuildResourceLine(ResourceType[] order)
        {
            _builder.Length = 0;

            for (int i = 0; i < order.Length; i++)
            {
                if (i > 0)
                {
                    _builder.Append("   ");
                }

                _builder.Append(_sim.Config.GetResource(order[i]).DisplayName);
                _builder.Append(' ');
                _builder.Append(_sim.Resources.Get(order[i]));
            }

            return _builder.ToString();
        }

        // ------------------------------------------------------------------
        // Auswahl
        // ------------------------------------------------------------------

        private void RefreshInspector()
        {
            Building building = _controller.SelectedBuilding;
            Squad squad = _controller.SelectedSquad;

            string key = building != null
                ? "b" + building.Id
                : (squad != null ? "s" + squad.Id : "-");

            if (key != _inspectorKey)
            {
                _inspectorKey = key;
                _inspectedBuilding = building;
                _inspectedSquad = squad;
                _inspectorDynamicText = null;

                UiBuilder.ClearChildren(_inspectorContent);

                if (building != null)
                {
                    BuildBuildingInspector(building);
                }
                else if (squad != null)
                {
                    BuildSquadInspector(squad);
                }
                else
                {
                    UiBuilder.CreateListText(
                        _inspectorContent, "Hint",
                        "Nichts ausgewaehlt.\n\nKlicke ein Gebaeude oder eine Truppe an, "
                        + "um Arbeiter zuzuweisen, Rezepte zu wechseln oder Einheiten aufzustellen.",
                        14, UiBuilder.MutedColor, 110f);
                }
            }

            UpdateInspectorText();
        }

        private void BuildBuildingInspector(Building building)
        {
            BuildingDefinition definition = building.Definition;

            UiBuilder.CreateListHeading(_inspectorContent, definition.DisplayName.ToUpperInvariant());

            _inspectorDynamicText = UiBuilder.CreateListText(
                _inspectorContent, "Details", "", 14, UiBuilder.TextColor, 132f);

            if (definition.WorkerSlots > 0)
            {
                RectTransform row = UiBuilder.CreateRow(_inspectorContent, 30f);
                UiBuilder.CreateButton(row, "Minus", "-1",
                    () => _sim.Buildings.AddWorkers(building, -1), 28f, 15);
                UiBuilder.CreateButton(row, "Plus", "+1",
                    () => _sim.Buildings.AddWorkers(building, 1), 28f, 15);
                UiBuilder.CreateButton(row, "None", "0",
                    () => _sim.Buildings.SetWorkers(building, 0), 28f, 15);
                UiBuilder.CreateButton(row, "Full", "Voll",
                    () => _sim.Buildings.SetWorkers(building, definition.WorkerSlots), 28f, 15);
            }

            if (definition.HasRecipes && definition.Recipes.Length > 1)
            {
                UiBuilder.CreateListHeading(_inspectorContent, "REZEPT");

                for (int i = 0; i < definition.Recipes.Length; i++)
                {
                    int index = i;
                    RecipeDefinition recipe = definition.Recipes[i];

                    UiBuilder.CreateButton(
                        _inspectorContent, "Recipe_" + i,
                        recipe.DisplayName + "  (" + Texts.Costs(recipe.Inputs, _sim.Config) + ")",
                        () => _sim.Buildings.SetRecipe(building, index), 26f, 13);
                }
            }
        }

        private void BuildSquadInspector(Squad squad)
        {
            UiBuilder.CreateListHeading(_inspectorContent, squad.Name.ToUpperInvariant());

            _inspectorDynamicText = UiBuilder.CreateListText(
                _inspectorContent, "Details", "", 14, UiBuilder.TextColor, 110f);

            UiBuilder.CreateListHeading(_inspectorContent, "EINHEITEN AUFSTELLEN");

            for (int i = 0; i < RecruitableUnits.Length; i++)
            {
                UnitType type = RecruitableUnits[i];
                UnitDefinition definition = _sim.Config.GetUnit(type);

                RectTransform row = UiBuilder.CreateRow(_inspectorContent, 28f);
                UiBuilder.CreateButton(row, "Add1_" + type, "+1 " + definition.DisplayName,
                    () => Recruit(squad, type, 1), 26f, 13);
                UiBuilder.CreateButton(row, "Add5_" + type, "+5",
                    () => Recruit(squad, type, 5), 26f, 13);

                UiBuilder.CreateListText(
                    _inspectorContent, "Cost_" + type,
                    Texts.Costs(definition.RecruitCost, _sim.Config)
                    + ", " + definition.FoodCost + " Nahrung, 1 Buerger",
                    12, UiBuilder.MutedColor, 16f);
            }

            RectTransform actions = UiBuilder.CreateRow(_inspectorContent, 30f);
            UiBuilder.CreateButton(actions, "Move", "Verlegen",
                _controller.BeginSquadMove, 28f, 14);
            UiBuilder.CreateButton(actions, "Disband", "Aufloesen",
                () => Disband(squad), 28f, 14);
        }

        private void Recruit(Squad squad, UnitType type, int count)
        {
            RecruitResult result = _sim.Squads.TryAddUnits(squad, type, count);
            _controller.SetStatus(Texts.Describe(result));
        }

        private void Disband(Squad squad)
        {
            _sim.Squads.Disband(squad);
            _controller.ClearSelection();
            _controller.SetStatus("Truppe aufgeloest, Buerger und Ausruestung sind zurueck.");
        }

        /// <summary>Aktualisiert nur die wechselnden Zahlen der aktuellen Auswahl.</summary>
        private void UpdateInspectorText()
        {
            if (_inspectorDynamicText == null)
            {
                return;
            }

            if (_inspectedBuilding != null)
            {
                Building building = _inspectedBuilding;
                BuildingDefinition definition = building.Definition;

                _builder.Length = 0;
                _builder.Append("Zustand: ").Append(Mathf.CeilToInt(building.Health))
                    .Append(" / ").Append(definition.MaxHealth).Append('\n');

                if (definition.WorkerSlots > 0)
                {
                    _builder.Append("Arbeiter: ").Append(building.AssignedWorkers)
                        .Append(" / ").Append(definition.WorkerSlots)
                        .Append("   (").Append(Mathf.RoundToInt(building.Efficiency * 100f))
                        .Append(" %)\n");
                }

                RecipeDefinition recipe = building.ActiveRecipe;
                if (recipe != null)
                {
                    _builder.Append("Produktion: ").Append(recipe.DisplayName).Append('\n');
                    _builder.Append("Input: ").Append(Texts.Costs(recipe.Inputs, _sim.Config)).Append('\n');
                    _builder.Append("Status: ").Append(
                        Texts.Describe(building.Status, building.MissingInput, _sim.Config)).Append('\n');
                    _builder.Append("Ausstoss: +")
                        .Append(building.OutputPerMinute.ToString("0.0"))
                        .Append(" ").Append(_sim.Config.GetResource(building.PrimaryOutput).DisplayName)
                        .Append("/min");
                }
                else if (definition.CanAttack)
                {
                    _builder.Append("Schaden: ").Append(definition.AttackDamage.ToString("0"))
                        .Append("   Reichweite: ").Append(definition.AttackRange.ToString("0")).Append('\n');
                    _builder.Append("Munition: ")
                        .Append(_sim.Config.GetResource(definition.AmmoResource).DisplayName)
                        .Append(" (Lager: ").Append(_sim.Resources.Get(definition.AmmoResource)).Append(")\n");
                    _builder.Append("Status: ").Append(
                        building.CanOperate
                            ? (_sim.Resources.Get(definition.AmmoResource) >= definition.AmmoPerShot
                                ? "GEFECHTSBEREIT"
                                : "KEINE MUNITION")
                            : "UNBESETZT");
                }
                else if (definition.Category == BuildingCategory.Support)
                {
                    _builder.Append("Bereitet das spaetere Item-System vor.\n");
                    _builder.Append("Bindet Bevoelkerung, hat aber noch keine Wirkung.");
                }
                else if (definition.IsCore)
                {
                    _builder.Append("Faellt das Rathaus, ist die Partie verloren.");
                }

                _inspectorDynamicText.text = _builder.ToString();
                return;
            }

            if (_inspectedSquad == null)
            {
                return;
            }

            Squad squad = _inspectedSquad;

            _builder.Length = 0;
            _builder.Append("Groesse: ").Append(squad.Count).Append('\n');
            _builder.Append(squad.Composition).Append('\n');
            _builder.Append("Position: ").Append(squad.Position.ToString()).Append('\n');
            _builder.Append("Ziel: ").Append(DescribeTarget(squad));

            _inspectorDynamicText.text = _builder.ToString();
        }

        private string DescribeTarget(Squad squad)
        {
            for (int i = 0; i < squad.Units.Count; i++)
            {
                int targetId = squad.Units[i].TargetEnemyId;
                if (targetId == 0)
                {
                    continue;
                }

                Enemy enemy = _sim.Enemies.GetById(targetId);
                if (enemy != null)
                {
                    return enemy.Definition.DisplayName;
                }
            }

            return "kein Gegner in Reichweite";
        }

        // ------------------------------------------------------------------
        // Truppenliste und Inventar
        // ------------------------------------------------------------------

        private void RefreshSquadList()
        {
            IReadOnlyList<Squad> squads = _sim.Squads.Squads;

            int signature = squads.Count * 7919;
            for (int i = 0; i < squads.Count; i++)
            {
                signature = signature * 31 + squads[i].Id * 17 + squads[i].Count;
            }

            if (signature == _squadListSignature)
            {
                return;
            }

            _squadListSignature = signature;
            UiBuilder.ClearChildren(_squadListContent);

            if (squads.Count == 0)
            {
                UiBuilder.CreateListText(
                    _squadListContent, "Empty",
                    "Noch keine Truppen. \"Neue Truppe\" waehlt eine Position auf der Karte.",
                    13, UiBuilder.MutedColor, 42f);
                return;
            }

            for (int i = 0; i < squads.Count; i++)
            {
                Squad squad = squads[i];
                UiBuilder.CreateButton(
                    _squadListContent, "Squad_" + squad.Id,
                    squad.Name + "  ·  " + squad.Count + " Einheiten",
                    () => _controller.SelectSquad(squad), 26f, 13);

                UiBuilder.CreateListText(
                    _squadListContent, "SquadInfo_" + squad.Id,
                    squad.Composition, 12, UiBuilder.MutedColor, 16f);
            }
        }

        private void RefreshInventory()
        {
            IReadOnlyList<InventoryItem> items = _sim.Inventory.Items;

            if (items.Count == _inventorySignature)
            {
                return;
            }

            _inventorySignature = items.Count;
            _inventoryHeading.text = "INVENTAR  " + items.Count + " / " + _sim.Inventory.Capacity;

            UiBuilder.ClearChildren(_inventoryContent);

            if (items.Count == 0)
            {
                UiBuilder.CreateListText(
                    _inventoryContent, "Empty",
                    "Leer. Fundstuecke kommen ausschliesslich aus Elitewellen.",
                    12, UiBuilder.MutedColor, 32f);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                UiBuilder.CreateListText(
                    _inventoryContent, "Item_" + i,
                    "· " + items[i].DisplayName, 13, UiBuilder.TextColor, 16f);
            }
        }

        private void RefreshLog()
        {
            IReadOnlyList<LogEntry> entries = _sim.Log.Entries;

            _builder.Length = 0;
            int start = entries.Count - 5;
            if (start < 0)
            {
                start = 0;
            }

            for (int i = start; i < entries.Count; i++)
            {
                if (_builder.Length > 0)
                {
                    _builder.Append('\n');
                }

                LogEntry entry = entries[i];
                switch (entry.Severity)
                {
                    case LogSeverity.Alert:
                        _builder.Append("<color=#F55A52>").Append(entry.Message).Append("</color>");
                        break;
                    case LogSeverity.Warning:
                        _builder.Append("<color=#F98C40>").Append(entry.Message).Append("</color>");
                        break;
                    default:
                        _builder.Append(entry.Message);
                        break;
                }
            }

            _logText.text = _builder.ToString();
        }

        private void OnStartWave()
        {
            _sim.Waves.StartWaveNow();
        }

        /// <summary>Nach dem Laden oder einem Neustart zeigen alle Listen auf Altbestand.</summary>
        public void InvalidateAll()
        {
            _inspectorKey = "?";
            _squadListSignature = -1;
            _inventorySignature = -1;
            _inspectedBuilding = null;
            _inspectedSquad = null;
            _inspectorDynamicText = null;
        }
    }
}
