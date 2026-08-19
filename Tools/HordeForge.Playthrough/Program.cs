using System;
using System.Collections.Generic;
using System.Globalization;
using HordeForge.Core;
using HordeForge.Core.Buildings;
using HordeForge.Core.Data;
using HordeForge.Core.Waves;

namespace HordeForge.Playthrough
{
    /// <summary>
    /// Faehrt eine komplette Partie ohne Unity und schreibt einen Bericht.
    ///
    ///   dotnet run --project Tools/HordeForge.Playthrough -- [Wellen] [Seed]
    /// </summary>
    public static class Program
    {
        private const float Step = 0.1f;
        private const float MaxMinutes = 90f;

        public static int Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            int targetWaves = ParseArgument(args, 0, 12);
            int seed = ParseArgument(args, 1, 20260819);

            GameConfig config = DefaultContent.CreateConfig();
            GameSimulation sim = new GameSimulation(config, seed);
            sim.StartNewGame();

            BotPlayer bot = new BotPlayer(sim, seed);

            Console.WriteLine("HordeForge – Durchlauf ohne Editor");
            Console.WriteLine("Seed " + seed + ", Ziel: Welle " + targetWaves);
            Console.WriteLine(new string('-', 96));
            Console.WriteLine(
                "Welle | Zeit  | Bev  frei | Nahrung | Holz Bret Stein Metall | Waffen | Truppen | Geb | Elite");
            Console.WriteLine(new string('-', 96));

            int reportedWave = 0;
            bool hordeReported = false;
            float elapsed = 0f;
            float limit = MaxMinutes * 60f;

            while (!sim.IsGameOver && sim.Waves.WaveNumber <= targetWaves && elapsed < limit)
            {
                sim.Tick(Step);
                bot.Update(Step);
                elapsed += Step;

                if (sim.Waves.Phase == WavePhase.Active && !hordeReported)
                {
                    hordeReported = true;
                    PrintHorde(sim);
                }

                if (sim.Waves.WaveNumber != reportedWave
                    && sim.Waves.Phase == WavePhase.Preparation)
                {
                    reportedWave = sim.Waves.WaveNumber;
                    hordeReported = false;
                    PrintRow(sim, elapsed);
                }
            }

            Console.WriteLine(new string('-', 96));
            PrintSummary(sim, bot, elapsed);

            // Ein Durchlauf, der schon vor der Zielwelle endet, ist ein Balancing-Befund
            // und soll den Aufruf scheitern lassen.
            return sim.IsGameOver ? 1 : 0;
        }

        private static int ParseArgument(string[] args, int index, int fallback)
        {
            int value;
            if (args != null && args.Length > index && int.TryParse(args[index], out value))
            {
                return value;
            }

            return fallback;
        }

        /// <summary>Zeigt, womit die Siedlung es diesmal zu tun bekommt.</summary>
        private static void PrintHorde(GameSimulation sim)
        {
            string composition = "";
            IReadOnlyList<HordeEntry> entries = sim.Waves.Composition;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    composition += ", ";
                }

                composition += entries[i].Count + "x "
                               + sim.Config.GetEnemy(entries[i].Type).DisplayName;
            }

            string directions = "";
            IReadOnlyList<SpawnDirection> spawns = sim.Waves.Directions;
            for (int i = 0; i < spawns.Count; i++)
            {
                if (i > 0)
                {
                    directions += " + ";
                }

                directions += MapLayout.DisplayName(spawns[i]);
            }

            Console.WriteLine("        > Welle " + sim.Waves.WaveNumber
                              + (sim.Waves.IsEliteWave ? " [ELITE]" : "")
                              + " aus " + directions + ": " + composition
                              + "  (Verteidiger: " + sim.Squads.TotalSoldiers + " Soldaten, "
                              + CountDefenses(sim) + " besetzte Anlagen)");
        }

        private static int CountDefenses(GameSimulation sim)
        {
            int count = 0;
            IReadOnlyList<Building> buildings = sim.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].Definition.CanAttack && buildings[i].CanOperate)
                {
                    count++;
                }
            }

            return count;
        }

        private static void PrintRow(GameSimulation sim, float elapsed)
        {
            string weapons =
                "B" + sim.Resources.Get(ResourceType.Bow)
                + " P" + sim.Resources.Get(ResourceType.Arrows)
                + " S" + sim.Resources.Get(ResourceType.Spear)
                + " Sw" + sim.Resources.Get(ResourceType.Sword);

            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,5} | {1,5} | {2,3}  {3,4} | {4,7} | {5,4} {6,4} {7,5} {8,6} | {9,-16} | {10,7} | {11,3} | {12}",
                sim.Waves.WaveNumber - 1,
                FormatTime(elapsed),
                sim.Population.Total,
                sim.Population.Free,
                sim.Resources.FoodStock,
                sim.Resources.Get(ResourceType.Wood),
                sim.Resources.Get(ResourceType.Planks),
                sim.Resources.Get(ResourceType.Stone),
                sim.Resources.Get(ResourceType.Metal),
                weapons,
                sim.Squads.TotalSoldiers,
                sim.Buildings.Buildings.Count,
                sim.Inventory.Count > 0 ? sim.Inventory.Count + " Item(s)" : "-"));
        }

        private static void PrintSummary(GameSimulation sim, BotPlayer bot, float elapsed)
        {
            Console.WriteLine();
            Console.WriteLine("Ergebnis:      " + (sim.IsGameOver
                ? "Rathaus gefallen in Welle " + sim.Waves.WaveNumber
                : "Welle " + (sim.Waves.WaveNumber - 1) + " ueberstanden"));
            Console.WriteLine("Spielzeit:     " + FormatTime(elapsed));
            Console.WriteLine("Bevoelkerung:  " + sim.Population.Total
                              + " gesamt, " + sim.Population.Free + " frei, "
                              + sim.Population.Reserved + " gebunden");
            Console.WriteLine("Gebaeude:      " + sim.Buildings.Buildings.Count
                              + " (" + bot.BuildingsPlaced + " selbst gebaut)");
            Console.WriteLine("Soldaten:      " + sim.Squads.TotalSoldiers
                              + " in " + sim.Squads.Squads.Count + " Truppen, "
                              + bot.UnitsRecruited + " insgesamt aufgestellt");
            Console.WriteLine("Inventar:      " + sim.Inventory.Count + " / "
                              + sim.Inventory.Capacity + " Fundstuecke");
            Console.WriteLine("Nahrung:       " + sim.Resources.FoodStock
                              + (sim.Population.IsStarving ? "  (KNAPP)" : ""));

            Console.WriteLine();
            Console.WriteLine("Gebaeudebestand:");
            PrintBuildingCounts(sim);

            Console.WriteLine();
            Console.WriteLine("Letzte Meldungen:");
            IReadOnlyList<LogEntry> entries = sim.Log.Entries;
            int start = Math.Max(0, entries.Count - 8);
            for (int i = start; i < entries.Count; i++)
            {
                Console.WriteLine("  " + entries[i].Message);
            }
        }

        private static void PrintBuildingCounts(GameSimulation sim)
        {
            Dictionary<BuildingType, int> counts = new Dictionary<BuildingType, int>();
            Dictionary<BuildingType, int> workers = new Dictionary<BuildingType, int>();

            IReadOnlyList<Building> buildings = sim.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingType type = buildings[i].Type;

                int count;
                counts.TryGetValue(type, out count);
                counts[type] = count + 1;

                int assigned;
                workers.TryGetValue(type, out assigned);
                workers[type] = assigned + buildings[i].AssignedWorkers;
            }

            foreach (KeyValuePair<BuildingType, int> pair in counts)
            {
                Console.WriteLine("  " + sim.Config.GetBuilding(pair.Key).DisplayName.PadRight(20)
                                  + pair.Value + "x, " + workers[pair.Key] + " Arbeiter");
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = (int)seconds;
            return (total / 60) + ":" + (total % 60).ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
