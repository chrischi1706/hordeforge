namespace HordeForge.Core.Data
{
    /// <summary>
    /// Die eine Stelle, an der Balancewerte stehen. Kein anderer Teil der Codebasis
    /// darf Zahlen zu Kosten, Schaden, Produktionsraten oder Budgets festverdrahten.
    /// Der Unity-Layer kann daraus ScriptableObjects erzeugen, die diese Werte dann
    /// im Editor ueberschreiben.
    /// </summary>
    public static class DefaultContent
    {
        private static ResourceAmount A(ResourceType resource, int amount)
        {
            return new ResourceAmount(resource, amount);
        }

        private static ResourceAmount[] C(params ResourceAmount[] entries)
        {
            return entries;
        }

        public static GameConfig CreateConfig()
        {
            GameConfig config = new GameConfig();
            config.Settings = CreateSettings();
            config.Waves = new WaveSettings();
            config.Map = CreateMap();
            config.Resources = CreateResources();
            config.Buildings = CreateBuildings();
            config.Units = CreateUnits();
            config.Enemies = CreateEnemies();
            config.Items = CreateItems();
            config.Initialize();
            return config;
        }

        private static GameSettings CreateSettings()
        {
            GameSettings settings = new GameSettings();
            settings.StartingResources = C(
                A(ResourceType.Wood, 260),
                A(ResourceType.Stone, 140),
                A(ResourceType.Planks, 45),
                A(ResourceType.CutStone, 35),
                A(ResourceType.Metal, 25),
                A(ResourceType.Thread, 15),
                A(ResourceType.Bread, 60),
                A(ResourceType.Fish, 40),
                A(ResourceType.Arrows, 120),
                A(ResourceType.SiegeAmmo, 20),

                // Ohne Startausruestung waere in den ersten Wellen ueberhaupt keine
                // Truppe moeglich – Bogen und Speer haengen an langen Ketten, die erst
                // anlaufen muessen. Genug fuer eine kleine gemischte Truppe.
                A(ResourceType.Bow, 4),
                A(ResourceType.Spear, 6),
                A(ResourceType.Shield, 8),
                A(ResourceType.Sword, 2),
                A(ResourceType.Armor, 2));
            return settings;
        }

        // ------------------------------------------------------------------
        // Testkarte
        // ------------------------------------------------------------------

        /// <summary>
        /// Kleine Testkarte: Rathaus im Zentrum, Rohstoffgebiete rundherum, dazwischen
        /// freier Platz fuer Mauern, Tuerme und Truppen. Die Horden kommen von aussen.
        /// </summary>
        private static MapDefinition CreateMap()
        {
            MapDefinition map = new MapDefinition();

            map.Zones = new[]
            {
                new ResourceZone("Westwald", BuildingType.LumberCamp, new Vec2(-30f, 20f), 13f),
                new ResourceZone("Ostwald", BuildingType.LumberCamp, new Vec2(32f, -18f), 11f),
                new ResourceZone("Steinbruchkante", BuildingType.Quarry, new Vec2(28f, 24f), 10f),
                new ResourceZone("Erzader", BuildingType.OreMine, new Vec2(-32f, -24f), 10f),
                new ResourceZone("Kornfelder", BuildingType.GrainFarm, new Vec2(0f, 32f), 12f),
                new ResourceZone("Baumwollfelder", BuildingType.CottonFarm, new Vec2(-38f, 2f), 9f),
                new ResourceZone("Schafweide", BuildingType.SheepPasture, new Vec2(38f, 2f), 9f),
                new ResourceZone("Fischgruende", BuildingType.FishingHut, new Vec2(0f, -34f), 12f)
            };

            // Genug, damit sofort etwas laeuft – der Rest wird vom Spieler gebaut.
            map.StartingBuildings = new[]
            {
                new BuildingPlacement(BuildingType.TownHall, new Vec2(0f, 0f)),
                new BuildingPlacement(BuildingType.LumberCamp, new Vec2(-28f, 18f)),
                new BuildingPlacement(BuildingType.GrainFarm, new Vec2(0f, 30f)),
                new BuildingPlacement(BuildingType.FishingHut, new Vec2(0f, -32f)),
                new BuildingPlacement(BuildingType.Sawmill, new Vec2(-10f, 8f))
            };

            return map;
        }

        // ------------------------------------------------------------------
        // Ressourcen
        // ------------------------------------------------------------------

        private static ResourceDefinition[] CreateResources()
        {
            return new[]
            {
                new ResourceDefinition(ResourceType.Wood, "Holz", ResourceCategory.Raw),
                new ResourceDefinition(ResourceType.Stone, "Stein", ResourceCategory.Raw),
                new ResourceDefinition(ResourceType.Ore, "Erz", ResourceCategory.Raw),
                new ResourceDefinition(ResourceType.Grain, "Getreide", ResourceCategory.Raw),
                new ResourceDefinition(ResourceType.Fish, "Fisch", ResourceCategory.Raw, 1),
                new ResourceDefinition(ResourceType.Cotton, "Baumwolle", ResourceCategory.Raw),
                new ResourceDefinition(ResourceType.Wool, "Wolle", ResourceCategory.Raw),

                new ResourceDefinition(ResourceType.Planks, "Bretter", ResourceCategory.Processed),
                new ResourceDefinition(ResourceType.CutStone, "Bearb. Stein", ResourceCategory.Processed),
                new ResourceDefinition(ResourceType.Metal, "Metall", ResourceCategory.Processed),
                new ResourceDefinition(ResourceType.Flour, "Mehl", ResourceCategory.Processed),
                new ResourceDefinition(ResourceType.Thread, "Faden", ResourceCategory.Processed),
                new ResourceDefinition(ResourceType.Cloth, "Stoff", ResourceCategory.Processed),
                new ResourceDefinition(ResourceType.Bread, "Brot", ResourceCategory.Processed, 3),

                new ResourceDefinition(ResourceType.Tool, "Werkzeug", ResourceCategory.Tool),

                new ResourceDefinition(ResourceType.Bow, "Bogen", ResourceCategory.Equipment),
                new ResourceDefinition(ResourceType.Spear, "Speer", ResourceCategory.Equipment),
                new ResourceDefinition(ResourceType.Sword, "Schwert", ResourceCategory.Equipment),
                new ResourceDefinition(ResourceType.Shield, "Schild", ResourceCategory.Equipment),
                new ResourceDefinition(ResourceType.Armor, "Ruestung", ResourceCategory.Equipment),

                new ResourceDefinition(ResourceType.Arrows, "Pfeile", ResourceCategory.Ammunition),
                new ResourceDefinition(ResourceType.SiegeAmmo, "Belagerungsmunition", ResourceCategory.Ammunition)
            };
        }

        // ------------------------------------------------------------------
        // Gebaeude
        // ------------------------------------------------------------------

        private static BuildingDefinition Gathering(
            BuildingType type, string name, int slots, float cycle,
            ResourceAmount output, ResourceAmount[] buildCost, int health = 620)
        {
            BuildingDefinition definition = new BuildingDefinition();
            definition.Type = type;
            definition.DisplayName = name;
            definition.Category = BuildingCategory.Gathering;
            definition.WorkerSlots = slots;
            definition.MaxHealth = health;
            definition.Radius = 1.8f;
            definition.BuildCost = buildCost;
            definition.Recipes = new[]
            {
                new RecipeDefinition(
                    name, cycle, ResourceAmountExtensions.Empty, C(output))
            };
            return definition;
        }

        private static BuildingDefinition Production(
            BuildingType type, string name, int slots,
            ResourceAmount[] buildCost, RecipeDefinition[] recipes, int health = 700)
        {
            BuildingDefinition definition = new BuildingDefinition();
            definition.Type = type;
            definition.DisplayName = name;
            definition.Category = BuildingCategory.Production;
            definition.WorkerSlots = slots;
            definition.MaxHealth = health;
            definition.Radius = 1.9f;
            definition.BuildCost = buildCost;
            definition.Recipes = recipes;
            return definition;
        }

        private static BuildingDefinition[] CreateBuildings()
        {
            BuildingDefinition townHall = new BuildingDefinition();
            townHall.Type = BuildingType.TownHall;
            townHall.DisplayName = "Rathaus";
            townHall.Category = BuildingCategory.Core;
            townHall.WorkerSlots = 0;
            townHall.MaxHealth = 6000;
            townHall.Radius = 4f;
            townHall.BuildCost = ResourceAmountExtensions.Empty;

            BuildingDefinition wall = new BuildingDefinition();
            wall.Type = BuildingType.Wall;
            wall.DisplayName = "Mauer";
            wall.Category = BuildingCategory.Fortification;
            wall.WorkerSlots = 0;
            wall.MaxHealth = 1400;
            wall.Radius = 1.2f;
            wall.BuildCost = C(A(ResourceType.Wood, 10), A(ResourceType.CutStone, 8));

            BuildingDefinition gate = new BuildingDefinition();
            gate.Type = BuildingType.Gate;
            gate.DisplayName = "Tor";
            gate.Category = BuildingCategory.Fortification;
            gate.WorkerSlots = 0;
            gate.MaxHealth = 1500;
            gate.Radius = 1.4f;
            gate.BuildCost = C(
                A(ResourceType.Wood, 15), A(ResourceType.CutStone, 10), A(ResourceType.Metal, 5));

            BuildingDefinition archerTower = new BuildingDefinition();
            archerTower.Type = BuildingType.ArcherTower;
            archerTower.DisplayName = "Bogenturm";
            archerTower.Category = BuildingCategory.Defense;
            archerTower.WorkerSlots = 2;
            archerTower.MaxHealth = 950;
            archerTower.Radius = 1.8f;
            archerTower.BuildCost = C(
                A(ResourceType.Planks, 15), A(ResourceType.CutStone, 15), A(ResourceType.Metal, 5));
            archerTower.AttackDamage = 20f;
            archerTower.AttackRange = 18f;
            archerTower.AttackInterval = 1.1f;
            archerTower.AmmoResource = ResourceType.Arrows;
            archerTower.AmmoPerShot = 1;

            BuildingDefinition ballista = new BuildingDefinition();
            ballista.Type = BuildingType.Ballista;
            ballista.DisplayName = "Ballista";
            ballista.Category = BuildingCategory.Defense;
            ballista.WorkerSlots = 3;
            ballista.MaxHealth = 820;
            ballista.Radius = 1.9f;
            ballista.BuildCost = C(
                A(ResourceType.Planks, 20), A(ResourceType.Metal, 10),
                A(ResourceType.Thread, 5), A(ResourceType.CutStone, 10));
            ballista.AttackDamage = 60f;
            ballista.AttackRange = 22f;
            ballista.AttackInterval = 3f;
            ballista.AmmoResource = ResourceType.SiegeAmmo;
            ballista.AmmoPerShot = 1;

            BuildingDefinition catapult = new BuildingDefinition();
            catapult.Type = BuildingType.Catapult;
            catapult.DisplayName = "Katapult";
            catapult.Category = BuildingCategory.Defense;
            catapult.WorkerSlots = 5;
            catapult.MaxHealth = 760;
            catapult.Radius = 2.1f;
            catapult.BuildCost = C(
                A(ResourceType.Planks, 30), A(ResourceType.Metal, 15),
                A(ResourceType.Thread, 8), A(ResourceType.CutStone, 15));
            catapult.AttackDamage = 130f;
            catapult.AttackRange = 28f;
            catapult.AttackInterval = 5f;
            catapult.AmmoResource = ResourceType.SiegeAmmo;
            catapult.AmmoPerShot = 2;

            BuildingDefinition tradeChamber = new BuildingDefinition();
            tradeChamber.Type = BuildingType.TradeChamber;
            tradeChamber.DisplayName = "Handelskammer";
            tradeChamber.Category = BuildingCategory.Support;
            tradeChamber.WorkerSlots = 3;
            tradeChamber.MaxHealth = 700;
            tradeChamber.Radius = 2f;
            tradeChamber.BuildCost = C(
                A(ResourceType.Wood, 40), A(ResourceType.CutStone, 20), A(ResourceType.Cloth, 10));

            return new[]
            {
                townHall,

                Gathering(BuildingType.LumberCamp, "Holzfaellerhuette", 3, 6f,
                    A(ResourceType.Wood, 3), C(A(ResourceType.Wood, 20))),
                Gathering(BuildingType.Quarry, "Steinbruch", 3, 7f,
                    A(ResourceType.Stone, 3), C(A(ResourceType.Wood, 25))),
                Gathering(BuildingType.OreMine, "Erzmine", 3, 8f,
                    A(ResourceType.Ore, 3), C(A(ResourceType.Wood, 30), A(ResourceType.Stone, 10))),
                Gathering(BuildingType.GrainFarm, "Getreidefeld", 4, 6f,
                    A(ResourceType.Grain, 4), C(A(ResourceType.Wood, 20))),
                Gathering(BuildingType.FishingHut, "Fischerhuette", 3, 6f,
                    A(ResourceType.Fish, 3), C(A(ResourceType.Wood, 25))),
                Gathering(BuildingType.CottonFarm, "Baumwollfarm", 3, 8f,
                    A(ResourceType.Cotton, 3), C(A(ResourceType.Wood, 20))),
                Gathering(BuildingType.SheepPasture, "Schaeferei", 3, 9f,
                    A(ResourceType.Wool, 2), C(A(ResourceType.Wood, 25))),

                Production(BuildingType.Sawmill, "Saegewerk", 3,
                    C(A(ResourceType.Wood, 30), A(ResourceType.Stone, 10)),
                    new[]
                    {
                        new RecipeDefinition("Bretter", 5f,
                            C(A(ResourceType.Wood, 2)), C(A(ResourceType.Planks, 1)))
                    }),

                Production(BuildingType.Stonemason, "Steinmetz", 3,
                    C(A(ResourceType.Wood, 30)),
                    new[]
                    {
                        new RecipeDefinition("Bearb. Stein", 6f,
                            C(A(ResourceType.Stone, 2)), C(A(ResourceType.CutStone, 1)))
                    }),

                Production(BuildingType.Smeltery, "Schmelzofen", 3,
                    C(A(ResourceType.Wood, 40), A(ResourceType.Stone, 20)),
                    new[]
                    {
                        new RecipeDefinition("Metall", 7f,
                            C(A(ResourceType.Ore, 2)), C(A(ResourceType.Metal, 1)))
                    }),

                Production(BuildingType.Mill, "Muehle", 2,
                    C(A(ResourceType.Wood, 30)),
                    new[]
                    {
                        new RecipeDefinition("Mehl", 5f,
                            C(A(ResourceType.Grain, 2)), C(A(ResourceType.Flour, 1)))
                    }),

                Production(BuildingType.Bakery, "Baeckerei", 2,
                    C(A(ResourceType.Wood, 30), A(ResourceType.Stone, 15)),
                    new[]
                    {
                        new RecipeDefinition("Brot", 5f,
                            C(A(ResourceType.Flour, 1)), C(A(ResourceType.Bread, 2)))
                    }),

                // Zwei Rezepte, weil Baumwolle und Wolle beide zu Faden werden.
                Production(BuildingType.Spinnery, "Spinnerei", 2,
                    C(A(ResourceType.Wood, 25)),
                    new[]
                    {
                        new RecipeDefinition("Faden aus Baumwolle", 6f,
                            C(A(ResourceType.Cotton, 2)), C(A(ResourceType.Thread, 1))),
                        new RecipeDefinition("Faden aus Wolle", 6f,
                            C(A(ResourceType.Wool, 2)), C(A(ResourceType.Thread, 1)))
                    }),

                Production(BuildingType.Weavery, "Weberei", 2,
                    C(A(ResourceType.Wood, 30)),
                    new[]
                    {
                        new RecipeDefinition("Stoff", 7f,
                            C(A(ResourceType.Thread, 2)), C(A(ResourceType.Cloth, 1)))
                    }),

                Production(BuildingType.Smithy, "Schmiede", 3,
                    C(A(ResourceType.Wood, 40), A(ResourceType.CutStone, 20), A(ResourceType.Metal, 10)),
                    new[]
                    {
                        new RecipeDefinition("Werkzeug", 8f,
                            C(A(ResourceType.Metal, 1), A(ResourceType.Wood, 1)),
                            C(A(ResourceType.Tool, 1))),
                        new RecipeDefinition("Speer", 9f,
                            C(A(ResourceType.Planks, 1), A(ResourceType.Metal, 1)),
                            C(A(ResourceType.Spear, 1))),
                        new RecipeDefinition("Schwert", 12f,
                            C(A(ResourceType.Planks, 2), A(ResourceType.Metal, 2)),
                            C(A(ResourceType.Sword, 1))),
                        new RecipeDefinition("Schild", 9f,
                            C(A(ResourceType.Planks, 1), A(ResourceType.Metal, 1)),
                            C(A(ResourceType.Shield, 1))),
                        new RecipeDefinition("Ruestung", 14f,
                            C(A(ResourceType.Metal, 2), A(ResourceType.Cloth, 1)),
                            C(A(ResourceType.Armor, 1))),
                        new RecipeDefinition("Belagerungsmunition", 8f,
                            C(A(ResourceType.Wood, 1), A(ResourceType.Stone, 2)),
                            C(A(ResourceType.SiegeAmmo, 4)))
                    }, 820),

                Production(BuildingType.Bowyer, "Bogner", 2,
                    C(A(ResourceType.Wood, 35), A(ResourceType.Planks, 10)),
                    new[]
                    {
                        new RecipeDefinition("Bogen", 10f,
                            C(A(ResourceType.Planks, 2), A(ResourceType.Thread, 1)),
                            C(A(ResourceType.Bow, 1))),
                        new RecipeDefinition("Pfeile", 6f,
                            C(A(ResourceType.Wood, 1), A(ResourceType.Metal, 1), A(ResourceType.Thread, 1)),
                            C(A(ResourceType.Arrows, 10)))
                    }),

                wall,
                gate,
                archerTower,
                ballista,
                catapult,
                tradeChamber
            };
        }

        // ------------------------------------------------------------------
        // Einheiten
        // ------------------------------------------------------------------

        private static UnitDefinition[] CreateUnits()
        {
            UnitDefinition archer = new UnitDefinition();
            archer.Type = UnitType.Archer;
            archer.DisplayName = "Bogenschuetze";
            archer.MaxHealth = 75;
            archer.AttackDamage = 13f;
            archer.AttackRange = 14f;
            archer.AttackInterval = 1.1f;
            archer.MoveSpeed = 3f;

            // Die Leine bestimmt, wie gross der Bereich ist, den eine stationierte
            // Truppe tatsaechlich verteidigt. Mit zu kleinen Werten laufen Gegner
            // einfach daran vorbei und die Truppe schaut zu.
            archer.LeashRadius = 5f;
            archer.AmmoResource = ResourceType.Arrows;
            archer.AmmoPerShot = 1;
            archer.RecruitCost = C(A(ResourceType.Bow, 1), A(ResourceType.Arrows, 12));
            archer.FoodCost = 5;

            UnitDefinition spearman = new UnitDefinition();
            spearman.Type = UnitType.Spearman;
            spearman.DisplayName = "Speertraeger";
            spearman.MaxHealth = 150;
            spearman.AttackDamage = 18f;
            spearman.AttackRange = 2.6f;
            spearman.AttackInterval = 1.2f;
            spearman.MoveSpeed = 3.2f;
            spearman.LeashRadius = 12f;
            spearman.RecruitCost = C(A(ResourceType.Spear, 1), A(ResourceType.Shield, 1));
            spearman.FoodCost = 5;

            UnitDefinition swordsman = new UnitDefinition();
            swordsman.Type = UnitType.Swordsman;
            swordsman.DisplayName = "Schwertkaempfer";
            swordsman.MaxHealth = 210;
            swordsman.AttackDamage = 30f;
            swordsman.AttackRange = 2f;
            swordsman.AttackInterval = 1.4f;
            swordsman.MoveSpeed = 3.4f;
            swordsman.LeashRadius = 14f;
            swordsman.RecruitCost = C(
                A(ResourceType.Sword, 1), A(ResourceType.Shield, 1), A(ResourceType.Armor, 1));
            swordsman.FoodCost = 6;

            return new[] { archer, spearman, swordsman };
        }

        // ------------------------------------------------------------------
        // Gegner
        // ------------------------------------------------------------------

        private static EnemyDefinition Enemy(
            EnemyType type, string name, int health, float speed, float damage,
            float interval, float range, float buildingMultiplier, int budgetCost, float radius)
        {
            EnemyDefinition definition = new EnemyDefinition();
            definition.Type = type;
            definition.DisplayName = name;
            definition.MaxHealth = health;
            definition.MoveSpeed = speed;
            definition.AttackDamage = damage;
            definition.AttackInterval = interval;
            definition.AttackRange = range;
            definition.BuildingDamageMultiplier = buildingMultiplier;
            definition.BudgetCost = budgetCost;
            definition.Radius = radius;
            return definition;
        }

        private static EnemyDefinition[] CreateEnemies()
        {
            return new[]
            {
                // Der Gebaeudefaktor trennt die Rollen: Goblins und Schattenreiter sind
                // vor allem fuer Truppen gefaehrlich, Ork-Krieger und Trolle reissen
                // Mauern und Tuerme nieder.
                Enemy(EnemyType.Goblin, "Goblin", 45, 4.2f, 6f, 1f, 1.6f, 0.6f, 10, 0.5f),
                Enemy(EnemyType.Ork, "Ork", 130, 2.8f, 14f, 1.3f, 1.8f, 1f, 25, 0.7f),
                Enemy(EnemyType.OrkWarrior, "Ork-Krieger", 260, 2.2f, 20f, 1.6f, 2f, 2.5f, 50, 0.8f),
                Enemy(EnemyType.ShadowRider, "Schattenreiter", 150, 6.5f, 18f, 1f, 1.8f, 0.8f, 75, 0.7f),
                Enemy(EnemyType.Troll, "Troll", 900, 1.5f, 45f, 2.4f, 2.4f, 3.5f, 200, 1.2f)
            };
        }

        // ------------------------------------------------------------------
        // Items
        // ------------------------------------------------------------------

        private static ItemDefinition[] CreateItems()
        {
            return new[]
            {
                new ItemDefinition(ItemType.OldSword, "Altes Schwert"),
                new ItemDefinition(ItemType.BatteredShield, "Zerbeulter Schild"),
                new ItemDefinition(ItemType.SignetRing, "Siegelring"),
                new ItemDefinition(ItemType.Crystal, "Kristall"),
                new ItemDefinition(ItemType.Relic, "Relikt"),
                new ItemDefinition(ItemType.AncientDocument, "Altes Dokument")
            };
        }
    }
}
