using System;
using System.Collections.Generic;
using HordeForge.Core.Data;

namespace HordeForge.Core.Economy
{
    /// <summary>
    /// Das zentrale Lager der Siedlung. Haelt Bestaende und garantiert, dass
    /// Buchungen atomar sind: entweder werden alle Posten einer Kostenliste
    /// abgebucht oder keiner.
    /// </summary>
    public sealed class ResourceLedger
    {
        private readonly Dictionary<ResourceType, int> _amounts =
            new Dictionary<ResourceType, int>();

        private readonly GameConfig _config;

        /// <summary>Feuert nach jeder Bestandsaenderung – vom HUD zum Auffrischen genutzt.</summary>
        public event Action<ResourceType, int> Changed;

        public ResourceLedger(GameConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _config = config;
        }

        public int Get(ResourceType resource)
        {
            int amount;
            return _amounts.TryGetValue(resource, out amount) ? amount : 0;
        }

        public void Add(ResourceType resource, int amount)
        {
            if (resource == ResourceType.None || amount == 0)
            {
                return;
            }

            int current = Get(resource);
            int next = current + amount;
            if (next < 0)
            {
                next = 0;
            }

            _amounts[resource] = next;
            RaiseChanged(resource, next);
        }

        public void Add(IReadOnlyList<ResourceAmount> amounts)
        {
            if (amounts == null)
            {
                return;
            }

            for (int i = 0; i < amounts.Count; i++)
            {
                Add(amounts[i].Resource, amounts[i].Amount);
            }
        }

        public void Set(ResourceType resource, int amount)
        {
            if (resource == ResourceType.None)
            {
                return;
            }

            _amounts[resource] = amount < 0 ? 0 : amount;
            RaiseChanged(resource, _amounts[resource]);
        }

        public bool Has(ResourceType resource, int amount)
        {
            return amount <= 0 || Get(resource) >= amount;
        }

        public bool CanAfford(IReadOnlyList<ResourceAmount> costs)
        {
            ResourceType missing;
            return !TryFindMissing(costs, out missing);
        }

        /// <summary>
        /// Liefert den ersten Posten, der nicht gedeckt ist. Das Gebaeude-UI zeigt
        /// damit "WARTET AUF HOLZ" statt nur "blockiert".
        /// </summary>
        public bool TryFindMissing(IReadOnlyList<ResourceAmount> costs, out ResourceType missing)
        {
            missing = ResourceType.None;
            if (costs == null)
            {
                return false;
            }

            for (int i = 0; i < costs.Count; i++)
            {
                ResourceAmount cost = costs[i];
                if (cost.Resource == ResourceType.None || cost.Amount <= 0)
                {
                    continue;
                }

                if (Get(cost.Resource) < cost.Amount)
                {
                    missing = cost.Resource;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Bucht eine komplette Kostenliste ab. Reicht auch nur ein Posten nicht,
        /// bleibt das Lager unveraendert.
        /// </summary>
        public bool TrySpend(IReadOnlyList<ResourceAmount> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return true;
            }

            if (!CanAfford(costs))
            {
                return false;
            }

            for (int i = 0; i < costs.Count; i++)
            {
                ResourceAmount cost = costs[i];
                if (cost.Resource == ResourceType.None || cost.Amount <= 0)
                {
                    continue;
                }

                Add(cost.Resource, -cost.Amount);
            }

            return true;
        }

        public bool TrySpend(ResourceType resource, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Get(resource) < amount)
            {
                return false;
            }

            Add(resource, -amount);
            return true;
        }

        /// <summary>
        /// Gibt Kosten zurueck ins Lager, aber ohne Munition – siehe Regel zum
        /// Aufloesen von Truppen: Ausruestung kommt zurueck, Munition ist verschossen.
        /// </summary>
        public void RefundWithoutAmmunition(IReadOnlyList<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return;
            }

            for (int i = 0; i < costs.Count; i++)
            {
                ResourceAmount cost = costs[i];
                if (cost.Resource == ResourceType.None || cost.Amount <= 0)
                {
                    continue;
                }

                if (_config.GetResource(cost.Resource).Category == ResourceCategory.Ammunition)
                {
                    continue;
                }

                Add(cost.Resource, cost.Amount);
            }
        }

        // ------------------------------------------------------------------
        // Nahrung
        // ------------------------------------------------------------------

        /// <summary>
        /// Gesamte Nahrung im Lager, gewichtet mit dem Naehrwert der Ressource.
        /// Brot zaehlt mehr als Fisch.
        /// </summary>
        public int FoodStock
        {
            get
            {
                int total = 0;
                ResourceDefinition[] resources = _config.Resources;
                for (int i = 0; i < resources.Length; i++)
                {
                    if (resources[i].FoodValue > 0)
                    {
                        total += Get(resources[i].Type) * resources[i].FoodValue;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Bucht Nahrungspunkte aus dem gemeinsamen Vorrat ab, egal aus welcher
        /// Nahrungsressource sie stammen. Reicht der Vorrat nicht, bleibt er unberuehrt.
        /// </summary>
        public bool TrySpendFood(int foodPoints)
        {
            if (foodPoints <= 0)
            {
                return true;
            }

            if (FoodStock < foodPoints)
            {
                return false;
            }

            int remaining = foodPoints;
            while (remaining > 0)
            {
                int gained = ConsumeOneFoodUnit();
                if (gained <= 0)
                {
                    return false;
                }

                remaining -= gained;
            }

            return true;
        }

        /// <summary>
        /// Verbraucht eine Einheit einer Nahrungsressource und liefert deren Naehrwert.
        /// Ressourcen mit kleinerem Naehrwert kommen zuerst dran, damit ein einzelner
        /// fehlender Nahrungspunkt kein ganzes Brot verschwendet.
        /// </summary>
        public int ConsumeOneFoodUnit()
        {
            ResourceDefinition[] resources = _config.Resources;
            ResourceDefinition best = null;

            for (int i = 0; i < resources.Length; i++)
            {
                ResourceDefinition candidate = resources[i];
                if (candidate.FoodValue <= 0 || Get(candidate.Type) <= 0)
                {
                    continue;
                }

                if (best == null || candidate.FoodValue < best.FoodValue)
                {
                    best = candidate;
                }
            }

            if (best == null)
            {
                return 0;
            }

            Add(best.Type, -1);
            return best.FoodValue;
        }

        // ------------------------------------------------------------------
        // Persistenz
        // ------------------------------------------------------------------

        public Dictionary<ResourceType, int> Snapshot()
        {
            return new Dictionary<ResourceType, int>(_amounts);
        }

        public void Restore(IDictionary<ResourceType, int> amounts)
        {
            _amounts.Clear();
            if (amounts != null)
            {
                foreach (KeyValuePair<ResourceType, int> pair in amounts)
                {
                    _amounts[pair.Key] = pair.Value < 0 ? 0 : pair.Value;
                }
            }

            RaiseChanged(ResourceType.None, 0);
        }

        private void RaiseChanged(ResourceType resource, int amount)
        {
            Action<ResourceType, int> handler = Changed;
            if (handler != null)
            {
                handler(resource, amount);
            }
        }
    }
}
