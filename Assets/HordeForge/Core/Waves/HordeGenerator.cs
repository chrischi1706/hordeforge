using System;
using System.Collections.Generic;
using HordeForge.Core.Data;

namespace HordeForge.Core.Waves
{
    /// <summary>Ein Posten der Hordenzusammensetzung.</summary>
    public struct HordeEntry
    {
        public EnemyType Type;
        public int Count;

        public HordeEntry(EnemyType type, int count)
        {
            Type = type;
            Count = count;
        }
    }

    /// <summary>
    /// Erzeugt aus einem Wellenbudget eine gueltige Gegnerzusammensetzung.
    ///
    /// Zwei Regeln: das Budget darf nie ueberschritten werden, und ein Gegnertyp
    /// erscheint erst, wenn das Budget deutlich groesser ist als seine Kosten.
    /// Innerhalb dieser Grenzen wuerfelt der Generator frei, damit sich Wellen
    /// unterscheiden.
    /// </summary>
    public sealed class HordeGenerator
    {
        private readonly GameConfig _config;

        public HordeGenerator(GameConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Budget der angegebenen Welle. Waechst geometrisch und wird gerundet,
        /// damit die Zahl im HUD lesbar bleibt.
        /// </summary>
        public int CalculateBudget(int waveNumber)
        {
            WaveSettings settings = _config.Waves;
            if (waveNumber < 1)
            {
                waveNumber = 1;
            }

            double raw = settings.BaseBudget * Math.Pow(settings.BudgetGrowth, waveNumber - 1);

            int rounding = settings.BudgetRounding;
            if (rounding <= 1)
            {
                return (int)Math.Round(raw);
            }

            return (int)(Math.Round(raw / rounding) * rounding);
        }

        /// <summary>
        /// Waehlt Gegner, bis das Restbudget fuer keinen freigeschalteten Typ mehr
        /// reicht. Die Summe der Kosten liegt garantiert bei oder unter
        /// <paramref name="budget"/>.
        /// </summary>
        public List<HordeEntry> Compose(int budget, Random random)
        {
            List<HordeEntry> result = new List<HordeEntry>();
            if (budget <= 0)
            {
                return result;
            }

            List<EnemyDefinition> pool = BuildPool(budget);
            if (pool.Count == 0)
            {
                return result;
            }

            Dictionary<EnemyType, int> counts = new Dictionary<EnemyType, int>();
            List<EnemyDefinition> affordable = new List<EnemyDefinition>();
            int remaining = budget;

            while (true)
            {
                affordable.Clear();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i].BudgetCost > 0 && pool[i].BudgetCost <= remaining)
                    {
                        affordable.Add(pool[i]);
                    }
                }

                if (affordable.Count == 0)
                {
                    break;
                }

                EnemyDefinition picked = affordable[random.Next(affordable.Count)];
                remaining -= picked.BudgetCost;

                int current;
                counts.TryGetValue(picked.Type, out current);
                counts[picked.Type] = current + 1;
            }

            foreach (KeyValuePair<EnemyType, int> pair in counts)
            {
                result.Add(new HordeEntry(pair.Key, pair.Value));
            }

            // Teure Gegner zuerst – so liest sich die Horde im HUD von oben nach unten.
            result.Sort((a, b) =>
                _config.GetEnemy(b.Type).BudgetCost.CompareTo(_config.GetEnemy(a.Type).BudgetCost));

            return result;
        }

        /// <summary>Gegnertypen, die bei diesem Budget ueberhaupt erscheinen duerfen.</summary>
        private List<EnemyDefinition> BuildPool(int budget)
        {
            float factor = _config.Waves.EnemyUnlockBudgetFactor;
            List<EnemyDefinition> pool = new List<EnemyDefinition>();
            EnemyDefinition cheapest = null;

            EnemyDefinition[] all = _config.Enemies;
            for (int i = 0; i < all.Length; i++)
            {
                EnemyDefinition definition = all[i];
                if (definition.BudgetCost <= 0)
                {
                    continue;
                }

                if (cheapest == null || definition.BudgetCost < cheapest.BudgetCost)
                {
                    cheapest = definition;
                }

                if (definition.BudgetCost * factor <= budget)
                {
                    pool.Add(definition);
                }
            }

            // Bei sehr kleinem Budget bleibt wenigstens der guenstigste Gegner uebrig.
            if (pool.Count == 0 && cheapest != null && cheapest.BudgetCost <= budget)
            {
                pool.Add(cheapest);
            }

            return pool;
        }

        public int TotalCost(IReadOnlyList<HordeEntry> entries)
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                total += _config.GetEnemy(entries[i].Type).BudgetCost * entries[i].Count;
            }

            return total;
        }

        public int TotalEnemies(IReadOnlyList<HordeEntry> entries)
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                total += entries[i].Count;
            }

            return total;
        }
    }
}
