using System;
using System.Collections.Generic;
using System.Text;

namespace HordeForge.Core.Data
{
    /// <summary>
    /// Ein Posten aus Ressource und Menge – Baukosten, Rezept-Input, Rekrutierungskosten.
    /// </summary>
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Resource;
        public int Amount;

        public ResourceAmount(ResourceType resource, int amount)
        {
            Resource = resource;
            Amount = amount;
        }

        public override string ToString()
        {
            return Amount + "x " + Resource;
        }
    }

    public static class ResourceAmountExtensions
    {
        public static readonly ResourceAmount[] Empty = new ResourceAmount[0];

        /// <summary>
        /// Kurzschreibweise fuer Kostenlisten in <see cref="DefaultContent"/>.
        /// </summary>
        public static ResourceAmount[] Cost(params ResourceAmount[] entries)
        {
            return entries ?? Empty;
        }

        public static string Describe(IReadOnlyList<ResourceAmount> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return "-";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < costs.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(costs[i].Amount).Append("x ").Append(costs[i].Resource);
            }

            return builder.ToString();
        }
    }
}
